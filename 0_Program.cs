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
using System.Linq;
using System.Net.Security;
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
using VRage.Scripting.MemorySafeTypes;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        //Version
        static readonly string _Version = "0.2.23";

        //General variables
        static bool logAll;
        TimeSpan timeSinceLooped;
        static string ignoreTag;
        
        //storage variables
        MyIni _store = new MyIni();
        string storedPosture = "";

        //Boot variables        
        static bool isBooting;
        
        //Program method, runs on PB block compilation. Contains boot steps.       
        public Program()
        {
            //Start boot process
            isBooting = true;
            Echo("Booting GuppOS, Version: " + _Version + ". . . ");


            //Load config
            Echo("Loading custom data config. . . ");
            if (!LoadConfig())
            {
                Echo("Config load failed, applying default config.");
                LoadDefaults();
            }
            Echo("Custom data config loaded.");

            //Initial loading of block lists
            Echo("Initializing block lists. . . ");
            InitBlocks();
            LoadBlocks(1);
            LoadBlocks(2);
            LoadBlocks(3);
            Echo("Block lists initialized.");

            //Load storage
            _store.TryParse(Storage);
            storedPosture = _store.Get("Status", "CurrentPosture").ToString("Standby");

            //Set starting posture
            FixPosture(storedPosture);

            //Enable slow loop
            loopVer = 1;
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            //Finish booting
            Echo("Boot complete.");
            isBooting = false;
        }

        public void Save()
        {
        }

        //Main method, runs every time PB block updates.
        public void Main(string argument, UpdateType updateSource)
        {
            //Detects if update is a command from a terminal or other action, and runs input command
            if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                RunCommand(argument);
            }
            
            //Detects if update is from the slow loop
            if ((updateSource & UpdateType.Update100) != 0)
            {
                timeSinceLooped = Runtime.TimeSinceLastRun;
                SlowLoop();
            }
        }

        //Run input command
        void RunCommand(string argument)
        {
            if (isBooting)
            {
                //Command failed due to system booting
                Echo("Command aborted, booting in process.");
                return;
            }
            if (argument == "")
            {
                //Command failed due to blank input
                logItems["CmE01"] = new logItem{category="Commands",content="Command failed due to blank input.", priority=3, maxLife=1};
                return;
            }
            
            string[] inputCommand = argument.Split('=');
            string[] inputValues;
            //Try to run command
            try
            {
                switch (inputCommand[0].Trim())
                {
                    case "StopRunning":
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        return;
                    case "StartRunning":
                        Runtime.UpdateFrequency = UpdateFrequency.Update100;
                        return;
                    case "PowerManagerEnabled":
                        //Enable/disable power manager
                        powerManagerEnabled = Convert.ToBoolean(inputCommand[1].Trim());
                        if (logAll) logItems["PML00"] = new logItem{category="PowerManager",content="PowerManagerEnabled set to "+powerManagerEnabled, priority=4, maxLife=1};
                        return;
                    case "DoorManagerEnabled":
                        //Enable/disable door manager
                        doorManagerEnabled = Convert.ToBoolean(inputCommand[1].Trim());
                        if (logAll) logItems["DML00"] = new logItem{category="DoorManager",content="DoorManagerEnabled set to "+doorManagerEnabled, priority=4, maxLife=1};
                        return;
                    case "SetDoorLock":
                        //Set door tag lock
                        inputValues = inputCommand[1].Split(':');
                        if (inputValues[1].Trim() == "Locked")
                        {
                            mainSDoors.SetDoorTagLock(inputValues[0].Trim(), false);
                        }
                        else if(inputValues[1].Trim() == "Unlocked")
                        {
                            mainSDoors.SetDoorTagLock(inputValues[0].Trim(), true);
                        }
                        else
                        {
                            //Throw error to get caught if input is wrong
                            throw new ArgumentException(inputValues[1].Trim()+" is not a valid lock status.");
                        }
                        return;
                    case "SetLightTag":
                        //Set light tag
                        LightSetting commandSetting = new LightSetting();
                        inputValues = inputCommand[1].Split(':');
                        //Parse light on/off
                        commandSetting.enabled = Convert.ToBoolean(inputValues[1].Trim());
                        if (commandSetting.enabled)
                        {
                            //Parse colour if light is on
                            string[] lightSettingColours = inputValues[2].Trim().Split(',');
                            commandSetting.colour = new Color
                            (
                                Convert.ToInt16(lightSettingColours[0].Trim()), 
                                Convert.ToInt16(lightSettingColours[1].Trim()), 
                                Convert.ToInt16(lightSettingColours[2].Trim()),
                                255
                            );
                        }
                        mainLights.SetLightTag(inputValues[0].Trim(), commandSetting);
                        return;
                    case "SetConnectorTag":
                        //Set connector tag
                        inputValues = inputCommand[1].Split(':');
                        mainConnectors.SetConTag(inputValues[0].Trim(), Convert.ToBoolean(inputValues[1].Trim()));
                        return;
                    case "SetPosture":
                        //Set posture
                        FixPosture(inputCommand[1].Trim());
                        return;
                    default:
                        //Command not recognized
                        logItems["CmE02"] = new logItem{category="Commands",content="Command "+inputCommand+" not recognized.", priority=3, maxLife=1};
                        return;
                }
            }
            catch (Exception exc)
            {
                //Command failed
                logItems["CmE03"+inputCommand] = new logItem{category="Commands",content="Command "+inputCommand+" failed: "+exc, priority=3, maxLife=1};
                return;
            }
        }

        //Main loop
        int loopVer;
        void SlowLoop()
        {
            //Write header and log to console
            EchoHeader(loopVer);
            EchoLog();
            
            //Refresh block lists
            LoadBlocks(loopVer);

            //run power manager
            PowerManager();

            //run door manager
            DoorManager();

            //Refresh LCDs
            RefreshLCDs();

            //Save current posture to storage ini
            _store.Set("Status", "CurrentPosture", currentPostureName);


            //Save storage ini to storage
            Storage = _store.ToString();


            //Iterate loop version counter
            if (loopVer < 3)
            {
                //count up
                loopVer ++;
            }
            else
            {
                //restart count
                loopVer = 1;
            }
        }

        //Log item object and dict
        static Dictionary<string, logItem> logItems = new Dictionary<string, logItem>();
        class logItem
        {
            //Item category
            public string category = "Misc";
            //Actual item content
            public string content = "";
            //Item priority (lower numbers have greater priority)
            public int priority = 3;
            //Item lifetime max (negative values are permanent)
            public int maxLife = 5;
            //Item lifetime counter
            public int life = 0;
        }

        //Creates console log output
        void EchoLog()
        {
            List<string> toRemove = new List<string>();
            //Cycles through priority levels up to and including 5, priority > 5 or < 0 will not be displayed
            for (int i = 0; i <= 5; i++)
            {
                foreach (KeyValuePair<string, logItem> item in logItems)
                {
                    //Displays log item if matching current priority in for loop
                    if (item.Value.priority == i)
                    {
                        //Display item with category and current lifetime
                        Echo("(" + item.Value.life + ") [color=#ffff9600]" + item.Value.category + ":[/color] " + item.Value.content);

                        //increase lifetime counter
                        item.Value.life ++;
                        if (item.Value.life == item.Value.maxLife)
                        {
                            //Adds to delete list if lifetime runs out
                            toRemove.Add(item.Key);
                        }
                    }
                }
                //Remove old items
                foreach (string thing in toRemove)
                {
                    logItems.Remove(thing);

                }   
            }
        }

        //Creates console header
        void EchoHeader(int incr)
        {
            //Animated header
            string headerStart;
            string headerEnd;
            switch (incr)
            {
                case 1:
                    headerStart = "[color=#ff00ff00]-==[/color]";
                    headerEnd = "[color=#ff00ff00]==-[/color]";
                    break;
                case 2:
                    headerStart = "[color=#ff00ff00]=-=[/color]";
                    headerEnd = "[color=#ff00ff00]=-=[/color]";
                    break;
                case 3:
                    headerStart = "[color=#ff00ff00]==-[/color]";
                    headerEnd = "[color=#ff00ff00]-==[/color]";
                    break;
                default:
                    headerStart = "[color=#ff00ff00]===[/color]";
                    headerEnd = "[color=#ff00ff00]===[/color]";
                    break;
            }
            Echo(headerStart + "GuppOS, Version: " + _Version + headerEnd);
        }
    }
}
