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
        class ThingGroup<thingType> where thingType : class
        {
            //ThingGroup constructor
            public ThingGroup(string name, string[] iD)
            {
                groupName = name;
                logID = iD;
            }


            //Group name
            public string groupName;
            //Log tag and category
            public string[] logID;
            //List of things
            public List<thingType> thingList = new List<thingType>();


            //Methods
             //Load block group
            public virtual void LoadGroup(bool managerStatus = false, bool hardLoad = false) {}
        }
        
        
        //Block group parent class
        class BlockGroup<blockType> : ThingGroup<blockType> where blockType : class, IMyFunctionalBlock 
        {
            protected readonly Program program;

            //BlockGroup constructor
            public BlockGroup(Program program, string name, string[] iD) : base(name, iD)
            {
                this.program = program;
                groupName = name;
                logID = iD;
            }
            

            //Indicators and trackers
            public bool? groupEnabled = null;

            //Methods
             //Load block group
            public override void LoadGroup(bool managerStatus = false, bool hardLoad = false)
            {
                //Get blocks
                List<blockType> blocks = new List<blockType>();
                if (groupName == "ALL")
                {
                    program.GridTerminalSystem.GetBlocksOfType(blocks, b => b.IsSameConstructAs(program.Me) && !b.CustomName.Contains(ignoreTag));
                }
                else
                {
                    IMyBlockGroup myBlockGroup = program.GridTerminalSystem.GetBlockGroupWithName(groupName);
                    if (myBlockGroup == null)
                    {
                        logItems[logID[0]+"E01:"+groupName] = new logItem{category=logID[1], content=groupName+" not found.", priority=1, maxLife=3};
                        return;
                    }
                    myBlockGroup.GetBlocksOfType(blocks, b => b.IsSameConstructAs(program.Me) && !b.CustomName.Contains(ignoreTag));
                }

                //fill group object
                thingList = blocks;
                
                //Fill object with group info
                ReadBlocks();
            
                if (logAll) logItems[logID[0]+"L01:"+groupName] = new logItem{category=logID[1], content=groupName+" loaded.", priority=5, maxLife=1};
            }
             //Read block group (meant to be overridden in children)
              //Read blocks main method
            public void ReadBlocks() 
            {
                try
                {
                    ReadStep();
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E02:"+groupName] = new logItem{category=logID[1], content="Error when reading "+groupName+" -"+exc, priority=1, maxLife=3};
                }
            }
              //Read step hook method
            public virtual void ReadStep() {}
             //Enable blocks in group
            public void SetGroupEnabled(bool enable)
            {
                try
                {
                    //Checks if generators already at status & group list not null or empty
                    if (enable != groupEnabled)
                    {
                        //Sets mode of each vent
                        foreach (blockType drive in thingList)
                        {
                            drive.Enabled = enable;
                        }
                        groupEnabled = enable;
                    }
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E03:"+groupName] = new logItem{category=logID[1], content="Error when enabling/disabling "+groupName+" -"+exc, priority=1, maxLife=3};
                }
            }
        }
        //Initializes block groups/lists
        void InitBlocks()
        {
            //Power groups
            mainBatteries = new BatteryGroup(this, mainBatteryGroupName);
            backupBatteries = new BatteryGroup(this, backupBatteryGroupName);
            mainReactors = new ReactorGroup(this, mainReactorGroupName);
            //LCDs
            allGOSLCDs = LoadLCDs();
            //Propulsion groups
            mainDrives = new DriveGroup(this, mainDriveGroupName);
            rcsThrusters = new DriveGroup(this, rcsGroupName);
            mainGyros = new GyroGroup(this, gyroGroupName);
            //Lights
            mainLights = new LightGroup(this, lightGroupName);
            //Gas tanks
            mainFuelTanks = new GasTankGroup(this, mainFuelTankGroupName);
            mainAirTanks = new GasTankGroup(this, mainAirTankGroupName);
            //Doors
            mainSDoors = new SDoorGroup(this, doorGroupName);
            //Life support groups
            mainAirVents = new VentGroup(this, ventGroupName);
            mainGravGens = new GravGenGroup(this, gravGenGroupName);
            //Connectors
            mainConnectors = new ConnectorGroup(this, connectorGroupName);
        }

        //Loads block groups/lists
        void LoadBlocks(int sec)
        {
            switch (sec)
            {
                case 1:
                    //Power groups
                    mainBatteries.LoadGroup(powerManagerEnabled);
                    backupBatteries.LoadGroup(powerManagerEnabled);
                    mainReactors.LoadGroup(powerManagerEnabled);
                    //LCDs
                    allGOSLCDs = LoadLCDs();
                    //Propulsion groups
                    mainDrives.LoadGroup();
                    rcsThrusters.LoadGroup();
                    mainGyros.LoadGroup();
                    break;
                case 2:
                    //Lights
                    mainLights.LoadGroup();
                    //Gas tanks
                    mainFuelTanks.LoadGroup();
                    mainAirTanks.LoadGroup();
                    break;
                case 3:
                    //Doors
                    mainSDoors.LoadGroup(doorManagerEnabled);
                    //Life support
                    mainAirVents.LoadGroup();
                    mainGravGens.LoadGroup();
                    //Connectors
                    mainConnectors.LoadGroup();
                    break;
                default:
                    break;
            }
        }
    }
}