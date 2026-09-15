using EmptyKeys.UserInterface.Generated;
using KeenSoftwareHouse.Library.Extensions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using SpaceEngineers.Game.SessionComponents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
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
        //Power manager config variables
        static bool powerManagerEnabled;
        static string mainBatteryGroupName;
        static string backupBatteryGroupName;
        static string mainReactorGroupName;
        static float lowPowerPercent;
        static float highPowerPercent;

        //Power manager objects
        BatteryGroup mainBatteries;
        BatteryGroup backupBatteries;
        class BatteryGroup : BlockGroup<IMyBatteryBlock>
        {
            //Constructor
            public BatteryGroup(Program program, string name) : base(program, name, new string[]{"Ba", "Batteries"}) {}

            //Indicators and trackers
            public ChargeMode? batteryGroupMode = null;
            public float groupPowerCapacity;
            public float groupPowerStored;
            public float groupPowerLevel;
            public int groupFuncBats;
            public int groupTotBats;
            
            //Methods
             //Read battery blocks
            public override void ReadStep()
            {
                groupPowerCapacity = 0;
                groupPowerStored = 0;
                groupPowerLevel = 0;
                groupFuncBats = 0;
                groupTotBats = thingList.Count;

                //Get max power and calculate current power level
                foreach (IMyBatteryBlock bat in thingList)
                {
                    if (bat.IsWorking)
                    {
                        groupPowerCapacity += bat.MaxStoredPower;
                        groupPowerStored += bat.CurrentStoredPower;
                        groupFuncBats ++;
                    }
                }
                groupPowerLevel = groupPowerStored / groupPowerCapacity * 100;
            }
             //Set battery mode
            public void SetBatMode(ChargeMode mode)
            {
                try
                {
                    //Checks if batteries already at requested mode
                    if (mode != batteryGroupMode)
                    {
                        //Sets each battery
                        foreach (IMyBatteryBlock bat in thingList)
                        {
                            bat.ChargeMode = mode;
                        }
                    }
                    batteryGroupMode = mode;
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E04:"+groupName] = new logItem{category=logID[1], content="Error when setting mode of "+groupName+" -"+exc, priority=1, maxLife=3};
                }
            }
        }
        ReactorGroup mainReactors;
        class ReactorGroup : BlockGroup<IMyReactor>
        {
            //Constructor
            public ReactorGroup(Program program, string name) : base(program, name, new string[]{"Re", "Reactors"}) {}
        }


        //Main power manager method
        void PowerManager()
        {
            //Check if power manager is enabled
            if (!powerManagerEnabled)
            {
                return;
            }

            //Disables power manager if any of the required block groups are invalid
            if (mainBatteries.thingList == null || backupBatteries.thingList == null || mainReactors.thingList == null)
            {
                logItems["PME01"] = new logItem{category="PowerManager", content="Block group(s) invalid, disabling Power Manager.", priority=1, maxLife=3};
                powerManagerEnabled = false;
                return;
            }

            //Set main battery mode
            if (!currentPosture.overrideBatteries)
            {
                //Sets main battery mode to auto if not overridden by current posture
                mainBatteries.SetBatMode(ChargeMode.Auto);
            }
            //Check levels for reactor status
            if (mainBatteries.groupPowerLevel < lowPowerPercent)
            {
                if (!currentPosture.overrideReactors)
                {
                    //Enable main reactors if low power and reactors not overridden by current posture
                    mainReactors.SetGroupEnabled(true);
                }
            }
            else if (mainBatteries.groupPowerLevel > highPowerPercent)
            {
                if (!currentPosture.overrideReactors)
                {
                    //Disable main reactors if high power and reactors not overridden by current posture
                    mainReactors.SetGroupEnabled(false);
                }
            }
            //Check levels for backup status
            if (mainBatteries.groupPowerLevel < 1 || mainBatteries.groupFuncBats < 1)
            {
                if (!currentPosture.overrideBatteries)
                {
                    //Enables backups if power critical or no functional main batteries and batteries not overridden by current posture
                    backupBatteries.SetBatMode(ChargeMode.Auto);
                }
            }
            else if (mainBatteries.groupPowerLevel >= lowPowerPercent)
            {
                if (!currentPosture.overrideBatteries)
                {
                    //Disables backups once power passes low threshold and batteries not overridden by current posture
                    backupBatteries.SetBatMode(ChargeMode.Recharge);
                }
            }
        }
    }
}