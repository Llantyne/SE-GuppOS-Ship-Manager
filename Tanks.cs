using EmptyKeys.UserInterface.Generated.StoreBlockView_Bindings;
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
using VRage.Game.ModAPI;
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
        //Tank config variables
        static string mainFuelTankGroupName;
        static string mainAirTankGroupName;

        //Tank group objects
        GasTankGroup mainFuelTanks;
        GasTankGroup mainAirTanks;
        class GasTankGroup : BlockGroup<IMyGasTank>
        {
            //Constructor
            public GasTankGroup(Program program, string name) : base(program, name, new string[]{"Ta", "Tanks"}) {}

            //Indicators and trackers
            public bool? stockpileOn = null;
            public float capacity = 0;
            public float stored = 0;
            public float lastStored = 0;
            public float level = 0;
            public int totalCount = 0;
            public int funcCount = 0;
            public float flowRate = 0;

            //Tank methods
             //Read tank blocks
            public override void ReadStep()
            {
                //Save last stored value for flow calculation
                lastStored = stored;
                float secondsSince = (float)program.timeSinceLooped.TotalSeconds;

                //Reset counters
                capacity = 0;
                stored = 0;
                totalCount = 0;
                funcCount = 0;
                
                foreach (IMyGasTank tank in thingList)
                {
                    totalCount ++;
                    if (tank.IsFunctional)
                    {
                        funcCount ++;
                        capacity += tank.Capacity;
                        stored += Convert.ToSingle(tank.Capacity * tank.FilledRatio);
                    }
                }
                level = stored / capacity * 100;
                
                if (secondsSince > 0)
                {
                    flowRate = (lastStored - stored) / secondsSince;
                }
            }
             //Set tank stockpile
            public void SetStockpile(bool stockpile)
            {
                try
                {
                    //Check if tanks already set to stockpile
                    if (stockpile != stockpileOn)
                    {
                        //Set each tank
                        foreach (IMyGasTank tank in thingList)
                        {
                            tank.Stockpile = stockpile;
                        }
                        //Set group indicator
                        stockpileOn = stockpile;
                    }
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E04:"+groupName] = new logItem{category=logID[1], content="Error when setting stockpile mode of "+groupName+" -"+exc, priority=1, maxLife=3};
                }
                
            }
        }
    }
}