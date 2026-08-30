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
using VRageMath;
using VRageRender;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        //Lighting config variables
        static string lightGroupName;
        static string navLightTag;

        //Light setting object
        class LightSetting
        {
            //Light enabled
            public bool enabled;
            //Light colour with default as white
            public Color colour = Color.White;
        }

        //light group object
        LightGroup mainLights;
        class LightGroup : BlockGroup<IMyLightingBlock>
        {
            //Constructor
            public LightGroup(Program program, string name) : base(program, name, new string[]{"LL", "Lighting"}) {}

            //Methods
             //Sets lights on/off and colour
            public void SetLightTag(string lightTag, LightSetting setting)
            {
                try
                {
                    //Goes through each light
                    foreach (IMyLightingBlock light in thingList)
                    {
                        if (light.CustomName.Contains(lightTag))
                        {
                            //Sets on/off
                            light.Enabled = setting.enabled;
                            //Sets colour
                            light.SetValue("Color", setting.colour);
                        }
                    }
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E04:"+groupName] = new logItem{category=logID[1], content="Error when setting light tag status of "+groupName+" -"+exc, priority=1, maxLife=3};
                }
            }
             //Sets nav lights on/off
            public void SetNavLights(bool on)
            {
                try
                {    
                    //Goes through each light
                    foreach (IMyLightingBlock light in thingList)
                    {
                        if (light.CustomName.Contains(navLightTag))
                        {
                            if (light.CustomName.ToLower().Contains("port"))
                            {
                                //Sets port nav lights
                                light.SetValue("Color", Color.Red);
                                light.Enabled = on;
                            }
                            else if (light.CustomName.ToLower().Contains("starboard"))
                            {
                                //Sets starboard nav lights
                                light.SetValue("Color", Color.Green);
                                light.Enabled = on; 
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E05:"+groupName] = new logItem{category=logID[1], content="Error when setting nav light status of "+groupName+" -"+exc, priority=1, maxLife=3};
                }
            }
             //Disable SetGroupEnabled method
            public new void SetGroupEnabled(bool enable)
            {
                throw new NotSupportedException("SetGroupEnabled is disabled in LightGroup.");
            }
        } 
    }
}