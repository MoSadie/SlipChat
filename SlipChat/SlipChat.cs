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
    public class SlipChat : BaseUnityPlugin, IMoPlugin, IMoHttpHandler
    {
        private static ConfigEntry<bool> debugMode;

        internal static ManualLogSource Log;

        public static readonly string HTTP_PREFIX = "slipchat";

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

                debugMode = Config.Bind("Developer Settings", "Debug Mode", false, "Enable debug mode, preventing the game from actually sending the order.");

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            }
            catch (Exception e)
            {
                Log.LogError("An error occurred while starting the plugin.");
                Log.LogError(e.Message);
            }

        }

        public HttpListenerResponse HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            Logger.LogInfo("Handling request");
            try
            {
                string path = request.Url.AbsolutePath.Trim('/');
                string[] parts = path.Split('/');

                if (parts.Length < 2 || parts[0] != HTTP_PREFIX)
                {
                    Logger.LogInfo("Invalid request path.");
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                    return response;
                } else if (parts[1] != "sendchat")
                {
                    Logger.LogInfo("Unknown request path.");
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                    return response;
                }

                bool ableToUse = CanUseAndOnHelm();

                // Check if we are the captain and are seated on the helm
                if (!ableToUse) // This also calls getIsCaptain() internally
                {
                    Logger.LogInfo($"Captain Seat check failed. IsCaptain: {GetIsCaptain()} IsFirstMate: {GetIsFirstMate()} AndOnHelm: {ableToUse}");
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                    string responseString = "You are not the captain/first mate or are not seated on the helm.";
                    byte[] responseBuffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = responseBuffer.Length;
                    System.IO.Stream responseOutput = response.OutputStream;
                    responseOutput.Write(responseBuffer, 0, responseBuffer.Length);
                    return response;
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
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                            response.Headers.Add("Access-Control-Allow-Origin", "*");
                            string responseString = "Message is not usable.";
                            byte[] responseBuffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                            response.ContentLength64 = responseBuffer.Length;
                            System.IO.Stream responseOutput = response.OutputStream;
                            responseOutput.Write(responseBuffer, 0, responseBuffer.Length);
                            return response;
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

                            response.StatusCode = (int)HttpStatusCode.OK;
                            response.Headers.Add("Access-Control-Allow-Origin", "*");
                            string responseString = "Message sent!";
                            byte[] responseBuffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                            response.ContentLength64 = responseBuffer.Length;
                            System.IO.Stream responseOutput = response.OutputStream;
                            responseOutput.Write(responseBuffer, 0, responseBuffer.Length);
                            return response;
                        }
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        response.Headers.Add("Access-Control-Allow-Origin", "*");
                        string responseString = "No message provided.";
                        byte[] responseBuffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = responseBuffer.Length;
                        System.IO.Stream responseOutput = response.OutputStream;
                        responseOutput.Write(responseBuffer, 0, responseBuffer.Length);
                        return response;
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError("An error occurred while handling the request. " + e.Message);
                Log.LogError(e.StackTrace);

                return response;
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

        public IMoHttpHandler GetHttpHandler()
        {
            return this;
        }

        public string GetPrefix()
        {
            return SlipChat.HTTP_PREFIX;
        }
    }
}
