using System;
using System.Collections.Generic;

namespace SlipChat
{
    internal class VariableHandler
    {
        internal static string ParseVariables(string message)
        {
            // Parse variables, which is any word starting with a $
            string[] words = message.Split(' ');
            string parsedMessage = "";

            foreach (string word in words)
            {
                if (word.StartsWith("$"))
                {
                    string variable = word.Substring(1);
                    string value = GetVariableValue(variable);

                    // If the variable is not found, just use the original word
                    if (value == null)
                    {
                        parsedMessage += word + " ";
                    }
                    else
                    {
                        parsedMessage += value + " ";
                    }
                } else
                {
                    parsedMessage += word + " ";
                }
            }

            // Trim the last space
            return parsedMessage.Trim();
        }

        internal static string GetVariableValue(string variable)
        {
            // Variables: $captain, $randomCrew[id], $crew[id], $enemyName, $enemyIntel, $enemyInvaders, $enemyThreat, $enemySpeed, $enemyCargo, $campaignName, $sectorName, $version
            // $randomCrew is special, it takes an id as a parameter and returns a random crew member but is consistant for the same id.
            // $crew is similar to $randomCrew but returns the a crew member using the numerical id of the crew member.

            // Remove any non-alphanumeric characters from the end of the variable name, saving them for later to reattach
            string nonAlphaNumeric = "";
            while (variable.Length > 0 && !char.IsLetterOrDigit(variable[variable.Length - 1]))
            {
                nonAlphaNumeric = variable[variable.Length - 1] + nonAlphaNumeric;
                variable = variable.Substring(0, variable.Length - 1);
            }

            string response = "";


            if (variable.StartsWith("randomCrew"))
            {
                string id = variable.Substring(10, variable.Length - 11);
                response = GetRandomCrewMember(id);
            }
            else if (variable.StartsWith("crew"))
            {
                string id = variable.Substring(5, variable.Length - 6);
                response = GetCrewMember(id);
            }
            else
            {

                switch (variable)
                {
                    case "version":
                        response = PluginInfo.PLUGIN_VERSION;
                        break;
                    case "captain":
                        response = Svc.Get<MpSvc>().Captains.CaptainClient.Player.DisplayName;
                        break;
                    case "enemyName":
                        MpScenarioController scenarioController = Svc.Get<MpSvc>().Scenarios;
                        if (scenarioController == null || scenarioController.CurrentScenario == null || scenarioController.CurrentScenario.Battle == null || scenarioController.CurrentScenario.Battle.Metadata.EnemyName == null)
                            response = "";
                        else
                            response = scenarioController.CurrentScenario.Battle.Metadata.EnemyName;
                        break;
                    case "enemyIntel":
                        MpScenarioController scenarioController2 = Svc.Get<MpSvc>().Scenarios;
                        if (scenarioController2 == null || scenarioController2.CurrentScenario == null || scenarioController2.CurrentScenario.Battle == null || scenarioController2.CurrentScenario.Battle.Metadata.IntelDescription == null)
                            response = "";
                        else
                            response = scenarioController2.CurrentScenario.Battle.Metadata.IntelDescription;
                        break;
                    case "enemyInvader": // Fallthrough to enemyInvaders
                    case "enemyInvaders":
                        MpScenarioController scenarioController3 = Svc.Get<MpSvc>().Scenarios;
                        if (scenarioController3 == null || scenarioController3.CurrentScenario == null || scenarioController3.CurrentScenario.Battle == null || scenarioController3.CurrentScenario.Battle.Metadata.InvaderDescription == null)
                            response = "";
                        else
                            response = scenarioController3.CurrentScenario.Battle.Metadata.InvaderDescription;
                        break;
                    case "enemyThreat":
                        MpScenarioController scenarioController4 = Svc.Get<MpSvc>().Scenarios;
                        if (scenarioController4 == null || scenarioController4.CurrentScenario == null || scenarioController4.CurrentScenario.Battle == null)
                            response = "";
                        else
                            response = scenarioController4.CurrentScenario.Battle.Metadata.ThreatLevel.ToString();
                        break;
                    case "enemySpeed":
                        MpScenarioController scenarioController5 = Svc.Get<MpSvc>().Scenarios;
                        if (scenarioController5 == null || scenarioController5.CurrentScenario == null || scenarioController5.CurrentScenario.Battle == null)
                            response = "";
                        else
                            response = scenarioController5.CurrentScenario.Battle.Metadata.SpeedLevel.ToString();
                        break;
                    case "enemyCargo":
                        MpScenarioController scenarioController6 = Svc.Get<MpSvc>().Scenarios;
                        if (scenarioController6 == null || scenarioController6.CurrentScenario == null || scenarioController6.CurrentScenario.Battle == null)
                            response = "";
                        else
                            response = scenarioController6.CurrentScenario.Battle.Metadata.CargoLevel.ToString();
                        break;
                    case "campaignName":
                        MpCampaignController campaignController = Svc.Get<MpSvc>().Campaigns;
                        if (campaignController == null || campaignController.CurrentCampaign == null || campaignController.CurrentCampaign.CaptainCampaign == null || campaignController.CurrentCampaign.CaptainCampaign.CampaignVo == null || campaignController.CurrentCampaign.CaptainCampaign.CampaignVo.RegionVo.Metadata.Name == null)
                            response = "";
                        else
                            response = campaignController.CurrentCampaign.CaptainCampaign.CampaignVo.RegionVo.Metadata.Name;
                        break;
                    case "sectorName":
                        MpCampaignController campaignController2 = Svc.Get<MpSvc>().Campaigns;
                        if (campaignController2 == null || campaignController2.CurrentCampaign == null || campaignController2.CurrentCampaign.CaptainCampaign == null || campaignController2.CurrentCampaign.CaptainCampaign.CampaignVo == null || campaignController2.CurrentCampaign.CaptainCampaign.CampaignVo.CurrentSectorVo == null || campaignController2.CurrentCampaign.CaptainCampaign.CampaignVo.CurrentSectorVo.Definition.Name == null)
                            response = "";
                        else
                            response = campaignController2.CurrentCampaign.CaptainCampaign.CampaignVo.CurrentSectorVo.Definition.Name;
                        break;
                    default:
                        response = "";
                        break;
                }
            }

            return response + nonAlphaNumeric;
        }

        static Dictionary<string, string> crewMap = new Dictionary<string, string>();

        internal static string GetRandomCrewMember(string id)
        {
            if (crewMap.ContainsKey(id))
            {
                return crewMap[id];
            }

            Dictionary<int, Crewmate> crew = Svc.Get<MpSvc>().Crew.CrewMap;
            if (crew == null || crew.Count == 0)
            {
                return "";
            }

            Random random = new Random();
            int randomIndex = random.Next(0, crew.Count);
            int i = 0;
            Crewmate randomCrew = null;
            foreach (KeyValuePair<int, Crewmate> kvp in crew)
            {
                randomCrew = kvp.Value;
                if (i == randomIndex)
                {
                    break;
                }
                i++;
            }

            string crewName = randomCrew.Client.Player.DisplayName;

            crewMap.Add(id, crewName);
            return crewName;
        }

        internal static string GetCrewMember(string id)
        {
            Dictionary<int, Crewmate> crew = Svc.Get<MpSvc>().Crew.CrewMap;
            if (crew.Count == 0)
            {
                return "";
            }

            int crewId = 0;
            if (!int.TryParse(id, out crewId))
            {
                return "";
            }

            if (!crew.ContainsKey(crewId))
            {
                return "";
            }

            return crew[crewId].Client.Player.DisplayName;
        }

        internal static void Reset()
        {
            crewMap.Clear();
        }
    }
}