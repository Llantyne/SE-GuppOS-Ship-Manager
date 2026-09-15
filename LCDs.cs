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
using System.Security.AccessControl;
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
        //LCD config variables
        static string gOSLCDTag;
        readonly static string basicFont = "Monospace";
         //colours
        static Color titleColour = new Color(255,255,255,255); //white
        static Color basicColour = new Color(255,255,255,255); //white
        static Color postureColour = new Color(255,128,192,255); //pinkish
        static Color enabledColour = new Color(0,255,0,255); //green
        static Color disabledColour = new Color(255,0,0,255); //red
        static Color onlineColour =  new Color(128,255,128,255); //light green
        static Color offlineColour = new Color(255,128,128,255); //light red
        static Color goodColour =  new Color(0, 255, 128, 255); //light green
        static Color cautionColour = new Color(255, 128, 64, 255); //orange
        static Color mildCautionColour = new Color(255, 255, 128, 255); //orange
        static Color autoModeColour = new Color(0,255,64,255); //green
        static Color rechargeModeColour = new Color(255,255,0,255); //yellow
        static Color dischargeModeColour = new Color(0,255,255,255); //cyan
        static Color defModeColour = new Color(0,255,64,255); //green
        static Color stockpileModeColour = new Color(128,255,255,255); //light blue




        //LCD list
        List<GOSLCD> allGOSLCDs;
        

        //GuppOS LCD object
        class GOSLCD
        {
            //LCD function
            public string lcdType = "";
            //Screen objects
            public IMyTextSurface lcdScreen;
            public RectangleF drawableSurface;
            //Font size
            public float fontScale = 1.0f;
            //Margins (top, bottom, left, right)
            public float[] margins;

            //Adjusts drawable surface for margins
            public void AdjustForMargins()
            {
                //Top margin
                drawableSurface.Y += margins[0];
                drawableSurface.Height -= margins[0];
                //Bottom margin
                drawableSurface.Height -= margins[1];
                //Left margin
                drawableSurface.X += margins[2];
                drawableSurface.Width -= margins[2];
                //Right margin
                drawableSurface.Width -= margins[3];
            }
        }


        //Load GuppOS LCDs
        MyIni _LCDConfig = new MyIni();
        List<GOSLCD> LoadLCDs()
        {
            List<GOSLCD> gLCDs = new List<GOSLCD>();
            
            //Gets all LCDs with tag and not ignore
            List<IMyTerminalBlock> lcdBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(lcdBlocks, block => block.CustomName.Contains(gOSLCDTag) && !block.CustomName.Contains(ignoreTag) && block.IsSameConstructAs(Me));

            foreach (IMyTerminalBlock block in lcdBlocks)
            {
                //Read custom data for lcd index and type
                string lcdCustomData = block.CustomData;
                IMyTextSurfaceProvider lcdProvider = block as IMyTextSurfaceProvider;
                MyIniParseResult lcdResult;
                if (lcdProvider != null && lcdProvider.SurfaceCount > 0)
                {
                    if (!_LCDConfig.TryParse(lcdCustomData, out lcdResult))
                    {
                        //Failed to parse custom data for lcd
                        logItems["LCDE02"] = new logItem{category="LCDs",content="Could not parse custom data for "+block.CustomName+" -"+lcdResult.ToString(), priority=1, maxLife=3};
                        continue;
                    }
                    if (_LCDConfig.ContainsSection("GuppOS.LCD"))
                    {
                        int screenIndex;
                        List<MyIniKey> sectKeys = new List<MyIniKey>(); 
                        _LCDConfig.GetKeys("GuppOS.LCD", sectKeys);
                        //Go through each key
                        foreach (MyIniKey key in sectKeys)
                        {
                            //Read screen index and create GOSLCD object
                            if (int.TryParse(key.Name.Trim().Substring(4), out screenIndex))
                            {
                                try
                                {
                                    string[] LCDSetting = _LCDConfig.Get(key).ToString().Split(':');
                                    if (LCDSetting.Length < 3)
                                    {
                                        throw new Exception("Invalid config format for " + key.ToString());
                                    }
                                    string[] LCDMargins = LCDSetting[2].Split(',');
                                    if (LCDMargins.Length < 4)
                                    {
                                        throw new Exception("Invalid config format for " + key.ToString());
                                    }

                                    //LCD object creation
                                    GOSLCD newLCD = new GOSLCD
                                    {
                                        lcdType = LCDSetting[0].Trim(),
                                        lcdScreen = lcdProvider.GetSurface(screenIndex),
                                        fontScale = Convert.ToSingle(LCDSetting[1].Trim()),
                                        margins = new float[]
                                        {
                                            Convert.ToSingle(LCDMargins[0].Trim()),
                                            Convert.ToSingle(LCDMargins[1].Trim()),
                                            Convert.ToSingle(LCDMargins[2].Trim()),
                                            Convert.ToSingle(LCDMargins[3].Trim())
                                        }
                                    };
                                    //Set drawable area
                                    newLCD.drawableSurface = new RectangleF
                                    (
                                        (newLCD.lcdScreen.TextureSize - newLCD.lcdScreen.SurfaceSize) / 2f,
                                        newLCD.lcdScreen.SurfaceSize
                                    );
                                    //Adjusts drawable area for margins
                                    newLCD.AdjustForMargins();
                                    //Add to list
                                    gLCDs.Add(newLCD);
                                }
                                catch (Exception exc)
                                {
                                    logItems["LCDE06"] = new logItem{category="LCDs",content="Error parsing custom data for "+block.CustomName+": "+exc, priority=1, maxLife=3};
                                    continue;
                                }
                            }
                            else
                            {
                                logItems["LCDE05"] = new logItem{category="LCDs",content="Error parsing screen index in "+block.CustomName+" config line "+key, priority=1, maxLife=3};
                                continue;
                            }
                        }
                    }
                    else
                    {
                        //Custom data does not contain LCD info
                        logItems["LCDE04"] = new logItem{category="LCDs",content=block.CustomName+" missing LCD config data.", priority=1, maxLife=3};
                        continue;
                    }
                }
                else
                {
                    //Block does not have any LCD screens
                    logItems["LCDE01"] = new logItem{category="LCDs",content=block.CustomName+" does not have LCDs", priority=1, maxLife=3};
                    continue;
                }
            }

            if (logAll) logItems["LCDL01"] = new logItem{category="LCDs",content=gLCDs.Count+" GuppOS LCDs loaded.", priority=5, maxLife=2};
            return gLCDs;
        }


        //Refresh LCDs
        void RefreshLCDs()
        {
            foreach (GOSLCD lcd in allGOSLCDs)
            {
                switch (lcd.lcdType)
                {
                    case "Log":
                        DrawLogLCD(lcd);
                        break;
                    case "Info":
                        DrawInfoLCD(lcd);
                        break;
                    default:
                        break;
                }
            }
        }

        //Draws console log LCD
        void DrawLogLCD(GOSLCD lcd)
        {
            //Prep LCD for script display
            lcd.lcdScreen.ContentType = ContentType.SCRIPT;
            lcd.lcdScreen.Script = "";

            //Calculate max characters in a line and max lines
            int logLineMaxChar = (int)Math.Floor(lcd.drawableSurface.Width / (19.4*0.8*lcd.fontScale));
            //int logMaxLines = (int)Math.Floor(lcd.drawableSurface.Height / (29*lcd.fontScale));

            //Draw sprites
            using (var frame = lcd.lcdScreen.DrawFrame())
            {
                float logLineY = 0;
                //Header
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = "===GuppOS Log===",
                    Position = new Vector2(lcd.drawableSurface.Width / 2, logLineY) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 1.0f * lcd.fontScale,
                    Color = titleColour,
                    FontId=basicFont
                });
                
                //Log line 1 Y-value
                logLineY += 35 * lcd.fontScale;

                //Mirror console log (does not increment lifetime counters)
                 //Cycles through priority levels up to and including 5, priority > 5 or < 0 will not be displayed
                for (int i=0; i<=5; i++)
                {
                    foreach (KeyValuePair<string, logItem> item in logItems)
                    {
                        //Displays log item if matching current priority in for loop
                        if (item.Value.priority == i)
                        {
                            StringBuilder logLine = new StringBuilder("(" + item.Value.life + ") " + item.Value.category + ": " + item.Value.content);
                            float lineHeight = 25 * lcd.fontScale;
                            //Checks if log item exceeds max line length
                            if (logLine.Length > logLineMaxChar)
                            {
                                //Splits log item into multiple lines
                                int lineBreaks = logLine.Length / logLineMaxChar;
                                for (int l=1; l<=lineBreaks; l++)
                                {
                                    logLine.Insert(logLineMaxChar*l, "\n");
                                    lineHeight += 25 * lcd.fontScale;
                                }
                            }
                            //Create sprite
                            frame.Add(new MySprite
                            {
                                Type = SpriteType.TEXT,
                                Data = logLine.ToString(),
                                Position = new Vector2(0, logLineY) + lcd.drawableSurface.Position,
                                Alignment = TextAlignment.LEFT,
                                RotationOrScale = 0.8f * lcd.fontScale,
                                Color = basicColour,
                                FontId=basicFont
                            });
                            logLineY += lineHeight;
                        }
                    }   
                }  
            }
            if (logAll) logItems["LCDL02"] = new logItem{category="LCDs",content="Log LCD drawn.", priority=5, maxLife=2};
        }

        //Draws general info LCD
        void DrawInfoLCD(GOSLCD lcd)
        {
            //Prep LCD for script display
            lcd.lcdScreen.ContentType = ContentType.SCRIPT;
            lcd.lcdScreen.Script = "";

            //Draw sprites
            using (var frame = lcd.lcdScreen.DrawFrame())
            {
                float spriteXPos = 0;
                float spriteYPos = 0;
                Color spriteColour;
                string spriteText;
                

                //Header
                spriteXPos = lcd.drawableSurface.Width / 2;
                spriteYPos = 0 * lcd.fontScale;
                spriteColour = titleColour;
                spriteText = "===GuppOS Ship Status===";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 1.0f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });

                //Posture indicator
                spriteXPos += 0;
                spriteYPos += 30 * lcd.fontScale;
                spriteColour = postureColour;
                spriteText = $"--Posture: {currentPostureName}--";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0.9f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });

                //Power manager section
                 //manager header
                spriteXPos = 0;
                spriteYPos += 25 * lcd.fontScale;
                spriteColour = titleColour;
                spriteText = "Power Manager:";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.8f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //Manager status
                spriteXPos = lcd.drawableSurface.Width / 2;
                spriteYPos += 0 * lcd.fontScale;
                if (powerManagerEnabled)
                {
                    spriteColour = enabledColour;
                    spriteText = "Enabled";
                }
                else
                {
                    spriteColour = disabledColour;
                    spriteText = "Disabled";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0.8f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //Reactors enabled
                spriteXPos = lcd.drawableSurface.Width;
                spriteYPos += 5 * lcd.fontScale;
                if (mainReactors.groupEnabled == true)
                {
                    spriteColour = onlineColour;
                    spriteText = "Reactors Online";
                }
                else
                {
                    spriteColour = offlineColour;
                    spriteText = "Reactors Offline";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.RIGHT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //Battery group header
                spriteXPos = 5;
                spriteYPos += 20 * lcd.fontScale;
                spriteColour = titleColour;
                spriteText = "Batteries  | Total | Functional | Level | Mode";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //Main battery group
                spriteXPos += 0;
                spriteYPos += 15 * lcd.fontScale;
                if (mainBatteries.batteryGroupMode == ChargeMode.Recharge)
                {
                    spriteColour = rechargeModeColour;
                }
                else if (mainBatteries.batteryGroupMode == ChargeMode.Discharge)
                {
                    spriteColour = dischargeModeColour;
                }
                else
                {
                    spriteColour = autoModeColour;
                }
                spriteText = $" Main      | {CentreText(mainBatteries.groupTotBats.ToString(), 5)} | {CentreText(mainBatteries.groupFuncBats.ToString(), 10)} | {CentreText(mainBatteries.groupPowerLevel.ToString("F0")+"%", 5)} | {mainBatteries.batteryGroupMode.ToString()}";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //Backup battery group
                spriteXPos += 0;
                spriteYPos += 15 * lcd.fontScale;
                if (backupBatteries.batteryGroupMode == ChargeMode.Recharge)
                {
                    spriteColour = rechargeModeColour;
                }
                else if (backupBatteries.batteryGroupMode == ChargeMode.Discharge)
                {
                    spriteColour = dischargeModeColour;
                }
                else
                {
                    spriteColour = autoModeColour;
                }
                spriteText = $" Backup    | {CentreText(backupBatteries.groupTotBats.ToString(), 5)} | {CentreText(backupBatteries.groupFuncBats.ToString(), 10)} | {CentreText(backupBatteries.groupPowerLevel.ToString("F0")+"%", 5)} | {backupBatteries.batteryGroupMode.ToString()}";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });

                //Fuel tank section
                 //fuel tank header
                spriteXPos = 0;
                spriteYPos += 20 * lcd.fontScale;
                spriteColour = titleColour;
                spriteText = "Main Fuel Tanks:";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.8f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //Fuel filled
                spriteXPos += 5;
                spriteYPos += 20 * lcd.fontScale;
                if (mainFuelTanks.level > 20)
                {
                    spriteColour = goodColour;
                }
                else
                {
                    spriteColour = cautionColour;
                }
                spriteText = "Filled: " + mainFuelTanks.level.ToString("F0") + "%";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.7f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //fuel stockpile
                spriteXPos += 0;
                spriteYPos += 17.5f * lcd.fontScale;
                if (mainFuelTanks.stockpileOn == true)
                {
                    spriteColour = stockpileModeColour;
                }
                else
                {
                    spriteColour = defModeColour;
                }
                spriteText = "Stockpile: " + mainFuelTanks.stockpileOn;
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.7f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //fuel consumption
                spriteXPos += 0;
                spriteYPos += 17.5f * lcd.fontScale;
                spriteColour = basicColour;
                spriteText = $"Consumption: {mainFuelTanks.flowRate:F1}L/s";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //fuel stored
                spriteXPos += 0;
                spriteYPos += 15 * lcd.fontScale;
                spriteColour = basicColour;
                spriteText = $"Stored: {mainFuelTanks.stored/1000:F1}kL";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //fuel max
                spriteXPos += 0;
                spriteYPos += 15 * lcd.fontScale;
                spriteColour = basicColour;
                spriteText = $"Capacity: {mainFuelTanks.capacity/1000:F1}kL";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });

                //Air tank section
                 //air tank header
                spriteXPos = lcd.drawableSurface.Width / 2;
                spriteYPos += -85 * lcd.fontScale;
                spriteColour = titleColour;
                spriteText = "Main Air Tanks:";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.8f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //air filled
                spriteXPos += 5;
                spriteYPos += 20 * lcd.fontScale;
                if (mainAirTanks.level > 20)
                {
                    spriteColour = goodColour;
                }
                else
                {
                    spriteColour = cautionColour;
                }
                spriteText = "Filled: " + mainAirTanks.level.ToString("F0") + "%";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.7f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //air stockpile
                spriteXPos += 0;
                spriteYPos += 17.5f * lcd.fontScale;
                if (mainAirTanks.stockpileOn == true)
                {
                    spriteColour = stockpileModeColour;
                }
                else
                {
                    spriteColour = defModeColour;
                }
                spriteText = "Stockpile: " + mainAirTanks.stockpileOn;
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.7f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //air consumption
                spriteXPos += 0;
                spriteYPos += 17.5f * lcd.fontScale;
                spriteColour = basicColour;
                spriteText = $"Consumption: {mainAirTanks.flowRate:F1}L/s";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //air stored
                spriteXPos += 0;
                spriteYPos += 15 * lcd.fontScale;
                spriteColour = basicColour;
                spriteText = $"Stored: {mainAirTanks.stored/1000:F1}kL";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //air max
                spriteXPos += 0;
                spriteYPos += 15 * lcd.fontScale;
                spriteColour = basicColour;
                spriteText = $"Capacity: {mainAirTanks.capacity/1000:F1}kL";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });

                //Propulsion
                 //Propulsion header
                spriteXPos = 0;
                spriteYPos += 20 * lcd.fontScale;
                spriteColour = titleColour;
                spriteText = "Propulsion:";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.8f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //main drive status
                spriteXPos += 5;
                spriteYPos += 20 * lcd.fontScale;
                if (mainDrives.groupEnabled == true)
                {
                    spriteColour = onlineColour;
                    spriteText = "Main Drives: Online";
                }
                else
                {
                    spriteColour = offlineColour;
                    spriteText = "Main Drives: Offline";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //RCS status
                spriteXPos += 0;
                spriteYPos += 15 * lcd.fontScale;
                if (rcsThrusters.groupEnabled == true)
                {
                    spriteColour = onlineColour;
                    spriteText = "RCS Thrusters: Online";
                }
                else
                {
                    spriteColour = offlineColour;
                    spriteText = "RCS Thrusters: Offline";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //Gyro status
                spriteXPos += 0;
                spriteYPos += 15 * lcd.fontScale;
                if (mainGyros.groupEnabled == true)
                {
                    spriteColour = onlineColour;
                    spriteText = "Gyroscopes: Online";
                }
                else
                {
                    spriteColour = offlineColour;
                    spriteText = "Gyroscopes: Offline";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });

                //Life support
                 //life support header
                spriteXPos = lcd.drawableSurface.Width / 2;
                spriteYPos += -50 * lcd.fontScale;
                spriteColour = titleColour;
                spriteText = "Life Support:";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.8f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //vent mode
                spriteXPos += 5;
                spriteYPos += 20 * lcd.fontScale;
                if (mainAirVents.ventsPressurized == true)
                {
                    spriteColour = onlineColour;
                    spriteText = "Vent Mode: Pressurize";
                }
                else
                {
                    spriteColour = mildCautionColour;
                    spriteText = "Vent Mode: Depressurize";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //leaks
                spriteXPos += 0;
                spriteYPos += 15 * lcd.fontScale;
                if (mainAirVents.leakyVents.Count > 0)
                {
                    spriteColour = cautionColour;
                    spriteText = "Leaks Detected: " + mainAirVents.leakyVents.Count;
                }
                else
                {
                    spriteColour = goodColour;
                    spriteText = "No Leaks Detected";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //Grav status
                spriteXPos += 0;
                spriteYPos += 15 * lcd.fontScale;
                if (mainGravGens.groupEnabled == true)
                {
                    spriteColour = onlineColour;
                    spriteText = "Gravity Gens: Online";
                }
                else
                {
                    spriteColour = offlineColour;
                    spriteText = "Gravity Gens: Offline";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });

                //Doors
                 //door manager header
                spriteXPos = 0;
                spriteYPos += 20 * lcd.fontScale;
                spriteColour = titleColour;
                spriteText = "Door Manager:";
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.8f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //Door manager status
                spriteXPos = lcd.drawableSurface.Width / 2;
                spriteYPos += 0 * lcd.fontScale;
                if (doorManagerEnabled)
                {
                    spriteColour = enabledColour;
                    spriteText = "Enabled";
                }
                else
                {
                    spriteColour = disabledColour;
                    spriteText = "Disabled";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0.8f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //doors disabled
                spriteXPos = 5;
                spriteYPos += 20 * lcd.fontScale;
                if (mainSDoors.sDoorsFunc < mainSDoors.thingList.Count)
                {
                    spriteColour = cautionColour;
                    spriteText = "Doors Disabled: " + (mainSDoors.thingList.Count - mainSDoors.sDoorsFunc);
                }
                else
                {
                    spriteColour = goodColour;
                    spriteText = "All Doors Operational";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
                 //doors open
                spriteXPos += 0;
                spriteYPos += 20 * lcd.fontScale;
                if (mainSDoors.sDoorsOpen > 0)
                {
                    spriteColour = mildCautionColour;
                    spriteText = "Doors Open: " + mainSDoors.sDoorsOpen;
                }
                else
                {
                    spriteColour = goodColour;
                    spriteText = "All Doors Closed";
                }
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = spriteText,
                    Position = new Vector2(spriteXPos, spriteYPos) + lcd.drawableSurface.Position,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = 0.6f * lcd.fontScale,
                    Color = spriteColour,
                    FontId=basicFont
                });
            }
            if (logAll) logItems["LCDL03"] = new logItem{category="LCDs",content="Info LCD drawn.", priority=5, maxLife=2};
        }
        //Centre text
        string CentreText(string text, int width)
        {
            return $"{text.PadLeft((width + text.Length) / 2).PadRight(width)}";
        }
    }
}