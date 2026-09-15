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
        //life support config variables
        static string ventGroupName;
        static string gravGenGroupName;

        //life support objects
        VentGroup mainAirVents;
        class VentGroup : BlockGroup<IMyAirVent>
        {
            //Constructor
            public VentGroup(Program program, string name) : base(program, name, new string[]{"LS", "LifeSupport"}) {}
            
            //Info variables
            public List<IMyAirVent> leakyVents = new List<IMyAirVent>();
            public bool? ventsPressurized;

            //Methods
             //Read vents
            public override void ReadStep()
            {
                leakyVents.Clear();
                foreach (IMyAirVent vent in thingList)
                {
                    if (!vent.CanPressurize)
                    {
                        leakyVents.Add(vent);
                    }
                }
                if (logAll&&leakyVents.Count>0) logItems["VeL02"] = new logItem{category="Vents",content="Air leak(s) detected.", priority=4, maxLife=1};
            }
             //Set vent pressurization mode
            public void VentsMode(bool pressurize)
            {
                try
                {
                    //Checks if vents already at pressurization status
                    if (pressurize != ventsPressurized)
                    {
                        //Sets mode of each vent
                        foreach (IMyAirVent vent in thingList)
                        {
                            vent.Depressurize = !pressurize;
                        }
                        ventsPressurized = pressurize;
                    }
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E04:"+groupName] = new logItem{category=logID[1], content="Error when setting mode of "+groupName+" -"+exc, priority=1, maxLife=3};
                }
            }
        }
        GravGenGroup mainGravGens;
        class GravGenGroup : BlockGroup<IMyGravityGeneratorBase>
        {
            //Constructor
            public GravGenGroup(Program program, string name) : base(program, name, new string[]{"LS", "LifeSupport"}) {}
        }
    }
}


            