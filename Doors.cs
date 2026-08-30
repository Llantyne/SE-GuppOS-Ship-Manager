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
using System.Net.Configuration;
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
        //Door manager config variables
        static bool doorManagerEnabled;
        static string doorGroupName;
        static int doorOpenLength;

        //Single door object
        class SingleDoor
        {
            //Door name
            public string doorName;
            //IMyDoor object
            public IMyDoor doorBlock;
            //Time door has been open (times 100 ticks)
            public int timeOpen = 0;
            //Lock status
            public bool isLocked;

            //Methods
             //Sets door lock
            public void SetDoorLock(bool unlockDoor)
            {
                //Lock/Unlock doors
                if (unlockDoor && isLocked)
                {
                    //Toggles lock if door requested to unlock when locked
                    doorBlock.ApplyAction("AnyoneCanUse");
                    isLocked = !isLocked;
                }
                if (!unlockDoor && !isLocked)
                {
                    //Toggles lock if door requested to lock when unlocked
                    doorBlock.ApplyAction("AnyoneCanUse");
                    isLocked = !isLocked;
                }
            }
        }

        //Door group object
        SDoorGroup mainSDoors;
        class SDoorGroup : ThingGroup<SingleDoor>
        {
            protected readonly Program program;
            
            //Constructor
            public SDoorGroup(Program program, string name) : base(name, new string[]{"SD", "SingleDoors"})
            {
                this.program = program;
            }

            //Tracking variables
            public int sDoorsOpen = 0;
            public int sDoorsFunc = 0;

            //DoorGroup methods
            //Load group
            public override void LoadGroup(bool managerStatus = false, bool hardLoad = false)
            {
                //Get doors
                List<IMyDoor> doors = new List<IMyDoor>();
                if (groupName == "ALL")
                {
                    program.GridTerminalSystem.GetBlocksOfType(doors, door => door.IsSameConstructAs(program.Me) && !door.CustomName.Contains(ignoreTag));
                }
                else
                {
                    IMyBlockGroup myBlockGroup = program.GridTerminalSystem.GetBlockGroupWithName(groupName);
                    if (myBlockGroup == null)
                    {
                        logItems[logID[0]+"E01:"+groupName] = new logItem{category=logID[1], content=groupName+" not found.", priority=1, maxLife=3};
                        return;
                    }
                    myBlockGroup.GetBlocksOfType(doors, door => door.IsSameConstructAs(program.Me) && !door.CustomName.Contains(ignoreTag));
                }
                
                //Creates single door object for each door and adds to list if is new door
                List<SingleDoor> singleDoors = new List<SingleDoor>();
                 //Check if thingList already has doors in it and not doing a hardLoad
                if (thingList.Count > 0 && !hardLoad)
                {
                    //Incorporates existing thingList
                    singleDoors.AddList(thingList);
                }
                foreach (IMyDoor door in doors)
                {
                    if (singleDoors.Exists(d => d.doorName == door.CustomName))
                    {
                        //Door with name already in thingList, skip adding
                        continue;
                    }
                    singleDoors.Add(new SingleDoor
                    {
                        doorName = door.CustomName,
                        doorBlock = door,
                        isLocked = !program.IfUnlocked(door)
                    });
                }

                //fill group object
                thingList = singleDoors;
                
                //Fill object with group info
                ReadSDoors();


                if (logAll) logItems[logID[0]+"E01:"+groupName] = new logItem{category=logID[1], content="Single doors loaded.", priority=5, maxLife=1};
            }
             //Read SingleDoors
            public void ReadSDoors()
            {
                try
                {
                    sDoorsOpen = 0;
                    sDoorsFunc = 0;
                    foreach (SingleDoor sDoor in thingList)
                    {
                        if (sDoor.doorBlock.IsWorking)
                        {
                            sDoorsFunc ++;
                        }
                        if (sDoor.doorBlock.OpenRatio > 0)
                        {
                            sDoorsOpen ++;
                        }
                    }
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E02:"+groupName] = new logItem{category=logID[1], content="Error when reading "+groupName+" -"+exc, priority=1, maxLife=3};
                }
            }
             //Sets individual door status
            public void ActionSDoor(SingleDoor sDoor)
            {
                try
                {
                    //Checks if door is any amount opened
                    if (sDoor.doorBlock.OpenRatio > 0)
                    {
                        //Checks if door has been open longer than allowed time
                        if (sDoor.timeOpen >= doorOpenLength)
                        {
                            //Closes door if required and sets time open to 0
                            sDoor.doorBlock.CloseDoor();
                            sDoor.timeOpen = 0;
                        }
                        else
                        {
                            //Increases open time counter
                            sDoor.timeOpen ++;
                        }
                    }
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E03:"+groupName] = new logItem{category=logID[1], content="Error when actioning door of "+groupName+" -"+exc, priority=1, maxLife=3};
                }
            }
             //Sets lock for doors with tag
            public void SetDoorTagLock(string doorTag, bool unlockDoors)
            {
                try
                {
                    //Iterates through all doors
                    foreach (var sDoor in thingList)
                    {
                        //Sets door lock if door has correct tag
                        if (sDoor.doorName.Contains(doorTag))
                        {
                            sDoor.SetDoorLock(unlockDoors);
                        }
                    }
                    if (logAll) logItems[logID[0]+"E01:"+groupName] = new logItem{category=logID[1],content="Doors with tag "+doorTag+" lock set to "+!unlockDoors, priority=4, maxLife=1};
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E04:"+groupName] = new logItem{category=logID[1], content="Error when setting door tag lock in "+groupName+" -"+exc, priority=1, maxLife=3};
                }
            }
        }
        

        //Main door manager function
        void DoorManager()
        {
            //Checks if door manager enabled
            if (!doorManagerEnabled)
            {
                return;
            }

            //Disables door manager if any of the required block groups are invalid
            if (mainSDoors.thingList == null)
            {
                logItems["DME01"] = new logItem{category="PowerManager", content="Block group(s) invalid, disabling Power Manager.", priority=1, maxLife=3};
                powerManagerEnabled = false;
                return;
            }

            //Cycles through all single doors
            foreach (SingleDoor sDoor in mainSDoors.thingList)
            {
                mainSDoors.ActionSDoor(sDoor);
            }
            if (logAll) logItems["DML01"] = new logItem{category="DoorManager",content="Single doors actioned.", priority=5, maxLife=1};
            
        }

        //Checks if a door is unlocked (thank you RSM)
        bool IfUnlocked(IMyDoor door)
        {
            ITerminalAction action = door.GetActionWithName("AnyoneCanUse");
            StringBuilder status = new StringBuilder();
            action.WriteValue(door, status);
            return status.ToString() == "On";
        }
    }
}