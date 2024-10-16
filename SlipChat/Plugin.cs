using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using System;
using System.Net;
using System.Collections.Generic;

namespace SlipChat
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Slipstream_Win.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<int> port;

        private static ConfigEntry<bool> debugMode;

        private static HttpListener listener = null;

        internal static ManualLogSource Log;

        public static readonly string COMPATIBLE_GAME_VERSION = "4.1579";

        private void Awake()
        {
            try
            {
                Log = base.Logger;

                Log.LogInfo($"Game version: {Application.version}");
                if (Application.version != COMPATIBLE_GAME_VERSION)
                {
                    Log.LogError($"This version of SlipChat is not compatible with the current game version. Please check for an updated version of the plugin.");
                    return;
                }

                port = Config.Bind("Server Settings", "Port", 8002, "Port to listen on.");

                debugMode = Config.Bind("Developer Settings", "Debug Mode", false, "Enable debug mode, preventing the game from actually sending the order.");


                if (!HttpListener.IsSupported)
                {
                    Log.LogError("HttpListener is not supported on this platform.");
                    listener = null;
                    return;
                }

                // Start the http server
                listener = new HttpListener();

                listener.Prefixes.Add($"http://127.0.0.1:{port.Value}/sendchat/");
                listener.Prefixes.Add($"http://localhost:{port.Value}/sendchat/");

                listener.Start();

                listener.BeginGetContext(new AsyncCallback(HandleRequest), listener);

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");


                Application.quitting += ApplicationQuitting;
            }
            catch (PlatformNotSupportedException e)
            {
                Log.LogError("HttpListener is not supported on this platform.");
                Log.LogError(e.Message);
            }
            catch (Exception e)
            {
                Log.LogError("An error occurred while starting the plugin.");
                Log.LogError(e.Message);
            }

        }

        private void HandleRequest(IAsyncResult result)
        {
            Logger.LogInfo("Handling request");
            try
            {
                HttpListener listener = (HttpListener)result.AsyncState;

                HttpListenerContext context = listener.EndGetContext(result);

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                HttpStatusCode status;
                string responseString;

                string pathUrl = request.RawUrl.Split('?', 2)[0];

                // Check if we are the captain and are seated on the helm
                if (!canUseAndOnHelm()) // This also calls getIsCaptain() internally
                {
                    Logger.LogInfo($"Captain Seat check failed. IsCaptain: {getIsCaptain()} IsFirstMate: {getIsFirstMate()} AndOnHelm: {canUseAndOnHelm()}");
                    status = HttpStatusCode.Forbidden;
                    responseString = "You are not the captain/first mate or are not seated on the helm.";
                }
                else
                {

                    // Parse query string into a potential message to send

                    string message = request.QueryString["message"];

                    Logger.LogInfo($"Pre-parsed Message: {message}");

                    if (message != null)
                    {
                        // Parse the message for variables starting with $
                        message = VariableHandler.ParseVariables(message);

                        // Validate the message using EditableText
                        if (!EditableText.IsTextUsable(message))
                        {
                            Logger.LogInfo($"Message is not usable: Null/Whitespace: {string.IsNullOrWhiteSpace(message)}. Null/Empty: {string.IsNullOrEmpty(message)}");
                            status = HttpStatusCode.BadRequest;
                            responseString = "Message is not usable.";
                        }
                        else
                        {
                            // Actually send the message :)
                            if (!debugMode.Value)
                            {
                                RequestCatalog.CaptainIssueOrderAll(OrderType.CustomMessage, message);
                                Logger.LogInfo($"Message sent: {message}");
                            }
                            else
                                Logger.LogInfo($"Debug mode enabled, message not sent: {message}");

                            status = HttpStatusCode.OK;
                            responseString = "Message sent!";
                        }
                    }
                    else
                    {
                        status = HttpStatusCode.BadRequest;
                        responseString = "No message provided.";
                    }
                }

                response.StatusCode = (int)status;

                response.Headers.Add("Access-Control-Allow-Origin", "*");

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();

                VariableHandler.Reset();


                // Start listening for the next request
                listener.BeginGetContext(new AsyncCallback(HandleRequest), listener);
            }
            catch (Exception e)
            {
                Log.LogError("An error occurred while handling the request.");
                Log.LogError(e.Message);
            }
        }

        private static bool getIsCaptain()
        {
            try
            {
                MpSvc mpSvc = Svc.Get<MpSvc>();

                if (mpSvc == null)
                {
                    Log.LogError("An error occurred handling self crew. null MpSvc.");
                    return false;
                }


                MpCaptainController captains = Svc.Get<MpSvc>().Captains;



                if (captains == null || captains.CaptainClient == null)
                {
                    return false;
                }
                else
                {
                    return captains.CaptainClient.IsLocal;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"An error occurred while checking if the crewmate is the captain: {e.Message}");
                return false;
            }
        }

        private static bool getIsFirstMate()
        {
            try
            {
                MpSvc mpSvc = Svc.Get<MpSvc>();

                if (mpSvc == null)
                {
                    Log.LogError("An error occurred handling self crew. null MpSvc.");
                    return false;
                }

                MpClientController clients = Svc.Get<MpSvc>().Clients;

                if (clients == null || clients.LocalClient == null)
                {
                    return false;
                }
                else
                {
                    return clients.LocalClient.Roles.Has(Roles.FirstMate);
                }
            } catch (Exception e)
            {
                Plugin.Log.LogError($"An error occurred while checking if the crewmate is the first mate: {e.Message}");
                return false;
            }
        }

        private static bool canUseAndOnHelm()
        {
            if (!(getIsCaptain() || getIsFirstMate()))
            {
                Log.LogInfo("Not captain or first mate.");
                return false;
            }

            try
            {
                MpSvc mpSvc = Svc.Get<MpSvc>();

                if (mpSvc == null)
                {
                    Log.LogError("An error occurred handling helm check. null MpSvc.");
                    return false;
                }

                MpClientController clients = mpSvc.Clients;

                if (clients == null)
                {
                    Log.LogWarning("An error occurred handling helm check. null Clients.");
                    return false;
                }

                LocalSlipClient self = clients.LocalClient;

                if (clients.LocalClient == null)
                {
                    Log.LogWarning("An error occurred handling helm check. null LocalClient.");
                    return false;
                }

                List<Crewmate> crew = self.Crew;
                
                if (crew == null)
                {
                    Log.LogWarning("An error occurred handling helm check. null Crew list.");
                    return false;
                }

                for (int i = 0; i < crew.Count; i++)
                {
                    Log.LogInfo($"Checking crewmate {i}: {crew[i].Client.Player.DisplayName} {crew[i].CurrentStation.StationType}");
                    if (crew[i] != null && crew[1].CurrentStation != null && crew[i].CurrentStation.StationType.Equals(StationType.Helm))
                    {
                        Log.LogInfo("Found valid crew on helm.");
                        return true;
                    }
                }

                Log.LogInfo("No valid crew on helm.");

                return false;


            }
            catch (Exception e)
            {
                Log.LogError($"An error occurred while checking if the crewmate is the captain/first mate and seated on the helm: {e.Message}");
                Log.LogError(e.StackTrace);
                return false;
            }
        }

        private void ApplicationQuitting()
        {
            Logger.LogInfo("Stopping server");
            // Stop server
            if (listener != null)
                listener.Close();
        }
    }
}
