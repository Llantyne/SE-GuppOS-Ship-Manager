using KeenSoftwareHouse.Library.Extensions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using SpaceEngineers.Game.SessionComponents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {   
        //Custom data config section lists
        readonly string[] defaultConfigSections = new string[]
        {
            "GuppOS.General",
            "GuppOS.PowerManager",
            "GuppOS.Tanks",
            "GuppOS.Propulsion",
            "GuppOS.LifeSupport",
            "GuppOS.Lighting",
            "GuppOS.Doors",
            "GuppOS.Connectors"
        };
        List<string> loadedSections = new List<string>();
        
        //Instantiate parser
        MyIni _config = new MyIni();
        
        //Load PB block custom data as config
        bool LoadConfig()
        {
            string customData = Me.CustomData;

            MyIniParseResult result;
            if (!_config.TryParse(customData, out result))
            {
                //Failed to parse custom data
                Echo("Could not parse custom data.\n-" + result.ToString());
                logItems["CfE01"] = new logItem{category="Config",content="Could not parse custom data.\n-"+result.ToString(), priority=0, maxLife=-1};
                return false;
            }

            List<string> sections = new List<string>();
            _config.GetSections(sections);
            foreach (string section in sections)
            {
                //Read basic config
                Echo("Reading section: " + section);
                try
                {
                    switch (section)
                    {
                        //General
                        case "GuppOS.General":
                            logAll = _config.Get(section, "LogAll").ToBoolean();
                            gOSLCDTag = _config.Get(section, "LCDTag").ToString().Trim();
                            ignoreTag = _config.Get(section, "IgnoreTag").ToString().Trim();
                            break;
                        //Power manager
                        case "GuppOS.PowerManager":
                            powerManagerEnabled = _config.Get(section, "PowerManagerEnabled").ToBoolean();
                            mainBatteryGroupName = _config.Get(section, "MainBatteryGroup").ToString().Trim();
                            backupBatteryGroupName = _config.Get(section, "BackupBatteryGroup").ToString().Trim();
                            mainReactorGroupName = _config.Get(section, "MainReactorGroup").ToString().Trim();
                            lowPowerPercent = _config.Get(section, "LowPowerLevel").ToSingle();
                            highPowerPercent = _config.Get(section, "HighPowerLevel").ToSingle();
                            break;
                        case "GuppOS.Tanks":
                            mainFuelTankGroupName = _config.Get(section, "MainFuelTankGroup").ToString().Trim();
                            mainAirTankGroupName = _config.Get(section, "MainAirTankGroup").ToString().Trim();
                            break;
                        case "GuppOS.Propulsion":
                            mainDriveGroupName = _config.Get(section, "EpsteinDriveGroup").ToString().Trim();
                            rcsGroupName = _config.Get(section, "RCSGroup").ToString().Trim();
                            gyroGroupName = _config.Get(section, "GyroGroup").ToString().Trim();
                            break;
                        case "GuppOS.LifeSupport":
                            ventGroupName = _config.Get(section, "VentGroup").ToString().Trim();
                            gravGenGroupName = _config.Get(section, "GravGenGroup").ToString().Trim();
                            break;
                        //Lighting
                        case "GuppOS.Lighting":
                            lightGroupName = _config.Get(section, "LightGroup").ToString().Trim();
                            navLightTag = _config.Get(section, "NavLightTag").ToString().Trim();
                            break;
                        //Door manager
                        case "GuppOS.Doors":
                            doorManagerEnabled = _config.Get(section, "DoorManagerEnabled").ToBoolean();
                            doorGroupName = _config.Get(section, "DoorGroup").ToString().Trim();
                            doorOpenLength = _config.Get(section, "DoorOpenLength").ToInt16();
                            break;
                        //Connectors
                        case "GuppOS.Connectors":
                            connectorGroupName = _config.Get(section, "ConnectorGroup").ToString().Trim();
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception exc)
                {
                    //Failed to parse section, outputs section that failed and loads defaults for section
                    Echo(exc + "\nCould not parse config section: " + section + "\n-Applying default values.");
                    LoadDefaultSection(section);
                    logItems["CfE02:"+section] = new logItem{category="Config",content="Config section "+section+" failed to parse; "+exc+"\n-Default values loaded for section.", priority=0, maxLife=-1};
                }
                
                //Read postures
                if (section.Contains("GuppOS.Posture."))
                {
                    string newPostureName = section.Substring(15);
                    try
                    {
                        availablePostures.Add(newPostureName, BuildPosture(_config, section));
                    }
                    catch (Exception exc)
                    {
                        Echo(exc + "\n-Could not build posture " + newPostureName);
                        logItems["CfE03:"+section] = new logItem{category="Config",content="Posture "+newPostureName+" failed to build; "+exc, priority=0, maxLife=-1};
                    }
                }

                //Add section to list of loaded sections
                loadedSections.Add(section);
            }

            //Load sections that were not found
            foreach (string sec in defaultConfigSections)
            {
                if (!loadedSections.Contains(sec))
                {
                    Echo("Could not find config section: " + sec + "\n-Applying default values.");
                    LoadDefaultSection(sec);
                    logItems["CfE04:"+sec] = new logItem{category="Config",content="Could not find config section: " + sec + "\n-Default values loaded for section.", priority=0, maxLife=-1};
                }
            }

            logItems["CfS01"] = new logItem{category="Config",content="Custom config applied.", priority=1, maxLife=-1};

            if (availablePostures.Count <= 0)
            {
                //Apply default postures if none loaded
                Echo("No custom postures found. Loading defaults.");
                availablePostures = BuildDefaultPostures();
                logItems["CfS02"] = new logItem{category="Config",content="Default postures applied", priority=1, maxLife=-1};
            }
            else
            {
                logItems["CfS02"] = new logItem{category="Config",content="Custom postures applied", priority=1, maxLife=-1};
            }
            //Add in boot posture
            BuildBootPosture();

            //Indicate successful load
            return true;
        }

        //Load default config
        void LoadDefaults()
        {
            //Load default config data
            foreach (string sec in defaultConfigSections)
            {
                LoadDefaultSection(sec);
            }

            //Build default postures
            availablePostures = BuildDefaultPostures();

            //Default loading success
            logItems["CfS01"] = new logItem{category="Config",content="Default config applied.", priority=1, maxLife=-1};
            logItems["CfS02"] = new logItem{category="Config",content="Default postures applied", priority=1, maxLife=-1};
        }

        //Default config data
        void LoadDefaultSection(string sect)
        {
            switch (sect)
            {
                //General
                case "GuppOS.General":
                    logAll = true;
                    gOSLCDTag = "<GOSLCD>";
                    ignoreTag = "<I>";
                    break;
                //Power manager
                case "GuppOS.PowerManager":
                    powerManagerEnabled = true;
                    mainBatteryGroupName = "Main Batteries";
                    backupBatteryGroupName = "Backup Batteries";
                    mainReactorGroupName = "ALL";
                    lowPowerPercent = 25;
                    highPowerPercent = 90;
                    break;
                //Tanks
                case "GuppOS.Tanks":
                    mainFuelTankGroupName = "Fuel Tanks";
                    mainAirTankGroupName = "Air Tanks";
                    break;
                //Propulsion
                case "GuppOS.Propulsion":
                    mainDriveGroupName = "Main Drives";
                    rcsGroupName = "RCS Drives";
                    gyroGroupName = "Gyros";
                    break;
                //Life support
                case "GuppOS.LifeSupport":
                    ventGroupName = "ALL";
                    gravGenGroupName = "ALL";
                    break;
                //Lighting
                case "GuppOS.Lighting":
                    lightGroupName = "ALL";
                    navLightTag = "*NAV*";
                    break;
                //Door manager
                case "GuppOS.Doors":
                    doorManagerEnabled = true;
                    doorGroupName = "ALL";
                    doorOpenLength = 6;
                    break;
                case "GuppOS.Connectors":
                    connectorGroupName = "ALL";
                    break;
            }
        }
    }
}