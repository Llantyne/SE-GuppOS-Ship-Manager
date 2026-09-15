using KeenSoftwareHouse.Library.Extensions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
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
        //Propulsion config variables
        static string mainDriveGroupName;
        static string rcsGroupName;
        static string gyroGroupName;

        //Group class objects
        DriveGroup mainDrives;
        DriveGroup rcsThrusters;
        class DriveGroup : BlockGroup<IMyThrust>
        {
            //Constructor
            public DriveGroup(Program program, string name) : base(program, name, new string[]{"Pr", "Propulsion"}) {}
            
            //Info variables
            public float maxThrust = 0;
            public float maxEffectiveThrust = 0;

            //Methods
             //Read drives for info
            public override void ReadStep()
            {
                //Reset info vars
                maxThrust = 0;
                maxEffectiveThrust = 0;
                
                foreach (IMyThrust drive in thingList)
                {
                    if (drive.IsWorking)
                    {
                        maxThrust += drive.MaxThrust;
                        maxEffectiveThrust += drive.MaxEffectiveThrust;
                    }
                }
            }
        }
        GyroGroup mainGyros;
        class GyroGroup : BlockGroup<IMyGyro>
        {
            public GyroGroup(Program program, string name) : base(program, name, new string[]{"Pr", "Propulsion"}) {}
        }
    }
}