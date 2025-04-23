using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using System;
using System.Net;
using System.Collections.Generic;
using MoCore;
using System.Threading;

namespace SlipChat
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.mosadie.mocore", BepInDependency.DependencyFlags.HardDependency)]
    [BepInProcess("Slipstream_Win.exe")]
    public class SlipChat : BaseUnityPlugin, MoPlugin
    {
        private static ConfigEntry<int> port;

        private static ConfigEntry<bool> debugMode;

        private static HttpListener listener = null;

        internal static ManualLogSource Log;

        private Thread serverThread;

        public static readonly string COMPATIBLE_GAME_VERSION = "4.1595";
        public static readonly string GAME_VERSION_URL = "https://raw.githubusercontent.com/MoSadie/SlipChat/refs/heads/main/versions.json";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Is actually used.")]
        private void Awake()
        {
            try
            {
                Log = base.Logger;

                if (!MoCore.MoCore.RegisterPlugin(this))
                {
                    Log.LogError("Failed to register plugin with MoCore. Please check the logs for more information.");
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

                serverThread = new Thread(() => ServerThread(listener));
                serverThread.Start();

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

        private void ServerThread(HttpListener listener)
        {
            try
            {
                listener.Start();

                while (listener.IsListening)
                {
                    HttpListenerContext context = listener.GetContext();
                    HandleRequest(context);
                }
            }
            catch (Exception e)
            {
                Log.LogError("An exception occurred in the http server thread.");
                Log.LogError(e.Message);
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            Logger.LogInfo("Handling request");
            try
            {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                HttpStatusCode status;
                string responseString;

                string pathUrl = request.RawUrl.Split('?', 2)[0];

                bool ableToUse = CanUseAndOnHelm();

                // Check if we are the captain and are seated on the helm
                if (!ableToUse) // This also calls getIsCaptain() internally
                {
                    Logger.LogInfo($"Captain Seat check failed. IsCaptain: {GetIsCaptain()} IsFirstMate: {GetIsFirstMate()} AndOnHelm: {ableToUse}");
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
            }
            catch (Exception e)
            {
                Log.LogError("An error occurred while handling the request. " + e.Message);
                Log.LogError(e.StackTrace);
            }
        }

        private static bool GetIsCaptain()
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
                SlipChat.Log.LogError($"An error occurred while checking if the crewmate is the captain: {e.Message}");
                return false;
            }
        }

        private static bool GetIsFirstMate()
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
                SlipChat.Log.LogError($"An error occurred while checking if the crewmate is the first mate: {e.Message}");
                return false;
            }
        }

        private static bool CanUseAndOnHelm()
        {
            if (!(GetIsCaptain() || GetIsFirstMate()))
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

                if (self == null)
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
                    try
                    {
                        Log.LogInfo($"Checking crewmate {i}: {crew[i].Client.Player.DisplayName} {(crew[i].CurrentStation != null ? crew[i].CurrentStation.StationType : "No Station")}");
                        if (crew[i] != null && crew[i].CurrentStation != null && crew[i].CurrentStation.StationType.Equals(StationType.Helm))
                        {
                            Log.LogInfo("Found valid crew on helm.");
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.LogError($"An error occurred while checking crew member {i}: {e.Message}");
                        Log.LogError(e.StackTrace);

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
            // Stop server, the thread is looking for the listener to stop listening
            if (listener != null)
                listener.Close();
        }

        public string GetCompatibleGameVersion()
        {
            return COMPATIBLE_GAME_VERSION;
        }

        public string GetVersionCheckUrl()
        {
            return GAME_VERSION_URL;
        }

        public BaseUnityPlugin GetPluginObject()
        {
            return this;
        }
    }
}
