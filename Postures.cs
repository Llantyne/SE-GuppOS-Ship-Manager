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
using System.Drawing.Drawing2D;
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
using VRage.Scripting.MemorySafeTypes;
using VRageMath;
using VRageRender;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        //Posture variables
        string currentPostureName;
        Posture currentPosture;
        Dictionary<string, Posture> availablePostures = new Dictionary<string, Posture>();

        //Posture object
        class Posture
        {
            //Fresh constructor
            public Posture() {}
            
            //Inheritee constructor
            public Posture(Posture source)
            {
                inherits = source.inherits;
                overrideBatteries = source.overrideBatteries;
                mainBatteryMode = source.mainBatteryMode;
                backupBatteryMode = source.backupBatteryMode;
                overrideReactors = source.overrideReactors;
                mainReactorsStatus = source.mainReactorsStatus;
                stockpileFuel = source.stockpileFuel;
                stockpileAir = source.stockpileAir;
                enableEpsteins = source.enableEpsteins;
                enableRCS = source.enableRCS;
                enableGyros = source.enableGyros;
                pressurizeVents = source.pressurizeVents;
                generateGravity = source.generateGravity;
                navLightsOn = source.navLightsOn;
                lightTagSetting = new Dictionary<string, LightSetting>(source.lightTagSetting);
                conTagSetting = new Dictionary<string, bool>(source.conTagSetting);
            }
            
            
            public string inherits = "";
            //Power settings
            public bool overrideBatteries = false;
            public ChargeMode mainBatteryMode = ChargeMode.Auto;
            public ChargeMode backupBatteryMode = ChargeMode.Recharge;
            public bool overrideReactors = false;
            public bool mainReactorsStatus = false;
            //Tank settings
            public bool stockpileFuel = false;
            public bool stockpileAir = false;
            //Propulsion settings
            public bool enableEpsteins = true;
            public bool enableRCS = true;
            public bool enableGyros = true;
            //Life support settings
            public bool pressurizeVents = true;
            public bool generateGravity = true;
            //Lighting settings
            public bool navLightsOn = true;
            public Dictionary<string, LightSetting> lightTagSetting = new Dictionary<string, LightSetting>();
            //Connector settings
            public Dictionary<string, bool> conTagSetting = new Dictionary<string, bool>();
        }

        
        //Build posture
        Posture BuildPosture(MyIni _ini, string section)
        {
            //Init posture object
            Posture newPosture;
            List<MyIniKey> newPostureKeys = new List<MyIniKey>();
            _ini.GetKeys(section, newPostureKeys);

            //Checks if inheriting posture
            if (_ini.ContainsKey(section, "Inherits"))
            {
                string inheriteeName = _ini.Get(section, "Inherits").ToString().Trim();
                if (availablePostures.ContainsKey(inheriteeName))
                {
                    //Inherits posture if inheritee exists
                    newPosture = new Posture(availablePostures[inheriteeName]);
                }
                else
                {
                    //Does not build posture if inheritee does not exist
                    throw new Exception("Inheritee not found.");
                }
            }
            else
            {
                //Fresh posture object if not inhertiting
                newPosture = new Posture();
            }
            
            //Power manager overrides
            if (_ini.ContainsKey(section, "OverrideBatteries"))
            {
                //Set override battery
                newPosture.overrideBatteries = _ini.Get(section, "OverrideBatteries").ToBoolean();
                //Checks if overriding batteries
                if(newPosture.overrideBatteries)
                {
                    if (_ini.ContainsKey(section, "MainBatteryMode"))
                    {
                        //Sets main battery mode
                        newPosture.mainBatteryMode = (ChargeMode)Enum.Parse
                        (
                            typeof(ChargeMode), 
                            _ini.Get(section, "MainBatteryMode").ToString().Trim()
                        );
                    }
                    if (_ini.ContainsKey(section, "BackupBatteryMode"))
                    {
                        //Sets backup battery mode
                        newPosture.backupBatteryMode = (ChargeMode)Enum.Parse
                        (
                            typeof(ChargeMode), 
                            _ini.Get(section, "BackupBatteryMode").ToString().Trim()
                        );
                    }
                }
            }
            if (_ini.ContainsKey(section, "OverrideReactors"))
            {
                //Set override reactor
                newPosture.overrideReactors = _ini.Get(section, "OverrideReactors").ToBoolean();
                //Checks if overriding reactors
                if(newPosture.overrideReactors)
                {
                    if (_ini.ContainsKey(section, "MainReactorStatus"))
                    {
                        //Sets main reactor mode
                        newPosture.mainReactorsStatus = _ini.Get(section, "MainReactorStatus").ToBoolean();
                    }
                }
            }
            //Tank modes
            if (_ini.ContainsKey(section, "StockpileFuel"))
            {
                //Stockpile fuel
                newPosture.stockpileFuel = _ini.Get(section, "StockpileFuel").ToBoolean();
            }
            if (_ini.ContainsKey(section, "StockpileAir"))
            {
                //Stockpile air
                newPosture.stockpileAir = _ini.Get(section, "StockpileAir").ToBoolean();
            }
            //Propulsion modes
            if (_ini.ContainsKey(section, "EpsteinsEnabled"))
            {
                //Set main drives
                newPosture.enableEpsteins = _ini.Get(section, "EpsteinsEnabled").ToBoolean();
            }
            if (_ini.ContainsKey(section, "RCSEnabled"))
            {
                //Set RCS
                newPosture.enableRCS = _ini.Get(section, "RCSEnabled").ToBoolean();
            }
            if (_ini.ContainsKey(section, "GyrosEnabled"))
            {
                //Set gyros
                newPosture.enableGyros = _ini.Get(section, "GyrosEnabled").ToBoolean();
            }
            //Life support modes
            if (_ini.ContainsKey(section, "PressurizeVents"))
            {
                //Set vent pressurization
                newPosture.pressurizeVents = _ini.Get(section, "PressurizeVents").ToBoolean();
            }
            if (_ini.ContainsKey(section, "GravEnabled"))
            {
                //Set grav gen status
                newPosture.generateGravity = _ini.Get(section, "GravEnabled").ToBoolean();
            }
            //Lighting settings
            if (_ini.ContainsKey(section, "NavLightsOn"))
            {
                //Sets nav lights
                newPosture.navLightsOn = _ini.Get(section, "NavLightsOn").ToBoolean();
            }
             //Get block tag settings
            foreach (var key in newPostureKeys)
            {
                //Checks if key is light setting
                if (key.Name.Contains("LightSetting"))
                {
                    //Parse light setting
                    LightSetting newSetting = new LightSetting();
                    string[] lightSetting = _ini.Get(key).ToString().Split(':');
                    //Parse light on/off
                    newSetting.enabled = Convert.ToBoolean(lightSetting[1].Trim());
                    if (newSetting.enabled)
                    {
                        //Parse colour if light is on
                        string[] lightSettingColours = lightSetting[2].Trim().Split(',');
                        newSetting.colour = new Color
                        (
                            Convert.ToInt16(lightSettingColours[0].Trim()), 
                            Convert.ToInt16(lightSettingColours[1].Trim()), 
                            Convert.ToInt16(lightSettingColours[2].Trim()),
                            255
                        );
                    }
                    //Add or replace light tag setting
                    newPosture.lightTagSetting[lightSetting[0].Trim()] = newSetting;
                }
                //Checks if key is connector setting
                if (key.Name.Contains("ConSetting"))
                {
                    //Parse light setting
                    bool conEnabled;
                    string[] conSetting = _ini.Get(key).ToString().Split(':');
                    //Parse light on/off
                    conEnabled = Convert.ToBoolean(conSetting[1].Trim());
                    //Add or replace light tag setting
                    newPosture.conTagSetting[conSetting[0].Trim()] = conEnabled;
                }
            }

            //Return built posture
            return newPosture;
        }

        //Set posture
        void FixPosture(string name)
        {
            //Set global posture variables
            currentPostureName = name;
            currentPosture = availablePostures[name];

            if (logAll) logItems["PsL00"] = new logItem{category="Postures",content="Setting posture to "+name, priority=4, maxLife=5};

            //Power settings
             //Set batteries
            if (currentPosture.overrideBatteries)
            {
                //Set main battery mode
                mainBatteries.SetBatMode(currentPosture.mainBatteryMode);
                //Set backup battery mode
                backupBatteries.SetBatMode(currentPosture.backupBatteryMode);
            }
             //Set reactors
            if (currentPosture.overrideReactors)
            {
                //Set main reactors
                mainReactors.SetGroupEnabled(currentPosture.mainReactorsStatus);
            }
            //Tank settings
            mainFuelTanks.SetStockpile(currentPosture.stockpileFuel);
            mainAirTanks.SetStockpile(currentPosture.stockpileAir);
            //Propulsion settings
            mainDrives.SetGroupEnabled(currentPosture.enableEpsteins);
            rcsThrusters.SetGroupEnabled(currentPosture.enableRCS);
            mainGyros.SetGroupEnabled(currentPosture.enableGyros);
            //Life support settings
             //Set vents pressurization
            mainAirVents.VentsMode(currentPosture.pressurizeVents);
             //Gravity gen online
            mainGravGens.SetGroupEnabled(currentPosture.generateGravity);
            //Light settings
             //Set nav lights
            mainLights.SetNavLights(currentPosture.navLightsOn);
             //Set light tags
            foreach (var kvp in currentPosture.lightTagSetting)
            {
                mainLights.SetLightTag(kvp.Key, kvp.Value);
            }
            //Connector tag settings
            foreach (var kvp in currentPosture.conTagSetting)
            {
                mainConnectors.SetConTag(kvp.Key, kvp.Value);
            }
        }

        //Build boot posture
        void BuildBootPosture()
        {
            availablePostures["Standby"] = new Posture
            {
                overrideBatteries = false,
                overrideReactors = false,
                stockpileFuel = false,
                stockpileAir = false,
                enableEpsteins = false,
                enableRCS = false,
                enableGyros = false,
                pressurizeVents = true,
                generateGravity = true,
                navLightsOn = false,
                lightTagSetting = new Dictionary<string, LightSetting>
                {
                    {
                        "*IN*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255, 255, 255, 255)
                        }
                    },
                    {
                        "*OUT*", 
                        new LightSetting
                        {
                            enabled = false
                        }
                    },
                    {
                        "*DOCK*",
                        new LightSetting
                        {
                            enabled = false
                        }
                    }
                },
                conTagSetting = new Dictionary<string, bool>
                {
                    {
                        "|Dockers|",
                        false
                    },
                    {
                        "|Docks|",
                        true
                    }
                }
            };
        }

        //Build dictionary of default postures for when custom postures failed to load
        Dictionary<string,Posture> BuildDefaultPostures()
        {
            Dictionary<string, Posture> defaultPostures = new Dictionary<string, Posture>();
            
            //Add flight posture
            defaultPostures["Cruise"] = new Posture
            {
                overrideBatteries = false,
                overrideReactors = false,
                stockpileFuel = false,
                stockpileAir = false,
                enableEpsteins = true,
                enableRCS = true,
                enableGyros = true,
                pressurizeVents = true,
                generateGravity = true,
                navLightsOn = true,
                lightTagSetting = new Dictionary<string, LightSetting>
                {
                    {
                        "*IN*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255, 255, 255, 255)
                        }
                    },
                    {
                        "*OUT*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255, 255, 255, 255)
                        }
                    },
                    {
                        "*DOCK*",
                        new LightSetting
                        {
                            enabled = false
                        }
                    }
                },
                conTagSetting = new Dictionary<string, bool>
                {
                    {
                        "|Dockers|",
                        false
                    },
                    {
                        "|Docks|",
                        true
                    }
                }
            };
            //Add maneuver posture
            defaultPostures["Maneuver"] = new Posture
            {
                overrideBatteries = false,
                overrideReactors = false,
                stockpileFuel = false,
                stockpileAir = false,
                enableEpsteins = false,
                enableRCS = true,
                enableGyros = true,
                pressurizeVents = true,
                generateGravity = true,
                navLightsOn = true,
                lightTagSetting = new Dictionary<string, LightSetting>
                {
                    {
                        "*IN*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255, 255, 255, 255)
                        }
                    },
                    {
                        "*OUT*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255, 255, 255, 255)
                        }
                    },
                    {
                        "*DOCK*",
                        new LightSetting
                        {
                            enabled = false
                        }
                    }
                },
                conTagSetting = new Dictionary<string, bool>
                {
                    {
                        "|Dockers|",
                        false
                    },
                    {
                        "|Docks|",
                        true
                    }
                }
            };
            //Add stealth cruise posture
            defaultPostures["StealthCruise"] = new Posture
            {
                overrideBatteries = false,
                overrideReactors = false,
                stockpileFuel = false,
                stockpileAir = false,
                enableEpsteins = true,
                enableRCS = true,
                enableGyros = true,
                pressurizeVents = true,
                generateGravity = true,
                navLightsOn = false,
                lightTagSetting = new Dictionary<string, LightSetting>
                {
                    {
                        "*IN*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(200, 200, 255, 255)
                        }
                    },
                    {
                        "*OUT*", 
                        new LightSetting
                        {
                            enabled = false
                        }
                    },
                    {
                        "*DOCK*",
                        new LightSetting
                        {
                            enabled = false
                        }
                    }
                },
                conTagSetting = new Dictionary<string, bool>
                {
                    {
                        "|Dockers|",
                        false
                    },
                    {
                        "|Docks|",
                        true
                    }
                }
            };
            //Add stealth maneuver posture
            defaultPostures["StealthManeuver"] = new Posture
            {
                overrideBatteries = false,
                overrideReactors = false,
                stockpileFuel = false,
                stockpileAir = false,
                enableEpsteins = false,
                enableRCS = true,
                enableGyros = true,
                pressurizeVents = true,
                generateGravity = true,
                navLightsOn = false,
                lightTagSetting = new Dictionary<string, LightSetting>
                {
                    {
                        "*IN*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(200, 200, 255, 255)
                        }
                    },
                    {
                        "*OUT*", 
                        new LightSetting
                        {
                            enabled = false
                        }
                    },
                    {
                        "*DOCK*",
                        new LightSetting
                        {
                            enabled = false
                        }
                    }
                },
                conTagSetting = new Dictionary<string, bool>
                {
                    {
                        "|Dockers|",
                        false
                    },
                    {
                        "|Docks|",
                        true
                    }
                }
            };
            //Add combat posture
            defaultPostures["Combat"] = new Posture
            {
                overrideBatteries = true,
                mainBatteryMode = ChargeMode.Auto,
                backupBatteryMode = ChargeMode.Auto,
                overrideReactors = true,
                mainReactorsStatus = true,
                stockpileFuel = false,
                stockpileAir = false,
                enableEpsteins = true,
                enableRCS = true,
                enableGyros = true,
                pressurizeVents = false,
                generateGravity = false,
                navLightsOn = false,
                lightTagSetting = new Dictionary<string, LightSetting>
                {
                    {
                        "*IN*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255, 0, 0, 255)
                        }
                    },
                    {
                        "*OUT*", 
                        new LightSetting
                        {
                            enabled = false
                        }
                    },
                    {
                        "*DOCK*",
                        new LightSetting
                        {
                            enabled = false
                        }
                    }
                },
                conTagSetting = new Dictionary<string, bool>
                {
                    {
                        "|Dockers|",
                        false
                    },
                    {
                        "|Docks|",
                        true
                    }
                }
            };
            //Add docking posture
            defaultPostures["Docking"] = new Posture
            {
                overrideBatteries = false,
                overrideReactors = false,
                stockpileFuel = false,
                stockpileAir = false,
                enableEpsteins = false,
                enableRCS = true,
                enableGyros = true,
                pressurizeVents = true,
                generateGravity = true,
                navLightsOn = true,
                lightTagSetting = new Dictionary<string, LightSetting>
                {
                    {
                        "*IN*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255, 255, 255, 255)
                        }
                    },
                    {
                        "*OUT*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255, 255, 255, 255)
                        }
                    },
                    {
                        "*DOCK*",
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255,255,255,255)
                        }
                    }
                },
                conTagSetting = new Dictionary<string, bool>
                {
                    {
                        "|Dockers|",
                        true
                    },
                    {
                        "|Docks|",
                        true
                    }
                }
            };
            //Add docked posture
            defaultPostures["Docked"] = new Posture
            {
                overrideBatteries = true,
                mainBatteryMode = ChargeMode.Recharge,
                backupBatteryMode = ChargeMode.Recharge,
                overrideReactors = true,
                mainReactorsStatus = false,
                stockpileFuel = true,
                stockpileAir = true,
                enableEpsteins = false,
                enableRCS = false,
                enableGyros = false,
                pressurizeVents = true,
                generateGravity = false,
                navLightsOn = true,
                lightTagSetting = new Dictionary<string, LightSetting>
                {
                    {
                        "*IN*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255, 255, 255, 255)
                        }
                    },
                    {
                        "*OUT*", 
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255, 255, 255, 255)
                        }
                    },
                    {
                        "*DOCK*",
                        new LightSetting
                        {
                            enabled = true,
                            colour = new Color(255,255,255,255)
                        }
                    }
                },
                conTagSetting = new Dictionary<string, bool>
                {
                    {
                        "|Dockers|",
                        true
                    },
                    {
                        "|Docks|",
                        true
                    }
                }
            };
            return defaultPostures;
        }
    }
}