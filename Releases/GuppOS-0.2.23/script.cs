// R e a d m e
// ----------------------
// GuppOS Ship Operation Manager
// - Ship management script with fully configurable "Postures" to set ship functionality
// - Adjustable LCD displays to work on any screen
// - Automatic battery mode and reactor management
// - Auto door closing and ship-wide door lock control
// - Customizable lighting control
// - More features in the works
// 
// All configuration is done in the custom data of the programmable block, or in the blocks of LCD screens.
// Designed for the Sigma Draconis Expanse 2 server
// 
// DO NOT TOUCH ANYTHING BELOW
// ----------------------
static string _Version="0.2.23",ignoreTag;static bool logAll,isBooting;TimeSpan timeSinceLooped;MyIni _store=new MyIni()
;string storedPosture="";public
 Program
(){isBooting=true;Echo("Booting GuppOS, Version: "+_Version+". . . ");Echo("Loading custom data config. . . ");if(!
LoadConfig()){Echo("Config load failed, applying default config.");LoadDefaults();}Echo("Custom data config loaded.");Echo(
"Initializing block lists. . . ");InitBlocks();LoadBlocks(1);LoadBlocks(2);LoadBlocks(3);Echo("Block lists initialized.");_store.TryParse(Storage);
storedPosture=_store.Get("Status","CurrentPosture").ToString("Standby");FixPosture(storedPosture);loopVer=1;Runtime.UpdateFrequency=
UpdateFrequency.Update100;Echo("Boot complete.");isBooting=false;}public void
 Save
(){}public void
 Main
(string argument,UpdateType updateSource){if((updateSource&(UpdateType.Trigger|UpdateType.Terminal))!=0){RunCommand(
argument);}if((updateSource&UpdateType.Update100)!=0){timeSinceLooped=Runtime.TimeSinceLastRun;SlowLoop();}}void RunCommand(
string argument){if(isBooting){Echo("Command aborted, booting in process.");return;}if(argument==""){logItems["CmE01"]=new
logItem{category="Commands",content="Command failed due to blank input.",priority=3,maxLife=1};return;}string[]inputCommand=
argument.Split('=');string[]inputValues;try{switch(inputCommand[0].Trim()){case"StopRunning":Runtime.UpdateFrequency=
UpdateFrequency.None;return;case"StartRunning":Runtime.UpdateFrequency=UpdateFrequency.Update100;return;case"PowerManagerEnabled":
powerManagerEnabled=Convert.ToBoolean(inputCommand[1].Trim());if(logAll)logItems["PML00"]=new logItem{category="PowerManager",content=
"PowerManagerEnabled set to "+powerManagerEnabled,priority=4,maxLife=1};return;case"DoorManagerEnabled":doorManagerEnabled=Convert.ToBoolean(
inputCommand[1].Trim());if(logAll)logItems["DML00"]=new logItem{category="DoorManager",content="DoorManagerEnabled set to "+
doorManagerEnabled,priority=4,maxLife=1};return;case"SetDoorLock":inputValues=inputCommand[1].Split(':');if(inputValues[1].Trim()==
"Locked"){mainSDoors.SetDoorTagLock(inputValues[0].Trim(),false);}else if(inputValues[1].Trim()=="Unlocked"){mainSDoors.
SetDoorTagLock(inputValues[0].Trim(),true);}else{throw new ArgumentException(inputValues[1].Trim()+" is not a valid lock status.");}
return;case"SetLightTag":LightSetting commandSetting=new LightSetting();inputValues=inputCommand[1].Split(':');commandSetting.
enabled=Convert.ToBoolean(inputValues[1].Trim());if(commandSetting.enabled){string[]lightSettingColours=inputValues[2].Trim().
Split(',');commandSetting.colour=new Color(Convert.ToInt16(lightSettingColours[0].Trim()),Convert.ToInt16(lightSettingColours
[1].Trim()),Convert.ToInt16(lightSettingColours[2].Trim()),255);}mainLights.SetLightTag(inputValues[0].Trim(),
commandSetting);return;case"SetConnectorTag":inputValues=inputCommand[1].Split(':');mainConnectors.SetConTag(inputValues[0].Trim(),
Convert.ToBoolean(inputValues[1].Trim()));return;case"SetPosture":FixPosture(inputCommand[1].Trim());return;default:logItems[
"CmE02"]=new logItem{category="Commands",content="Command "+inputCommand+" not recognized.",priority=3,maxLife=1};return;}}
catch(Exception exc){logItems["CmE03"+inputCommand]=new logItem{category="Commands",content="Command "+inputCommand+
" failed: "+exc,priority=3,maxLife=1};return;}}int loopVer;void SlowLoop(){EchoHeader(loopVer);EchoLog();LoadBlocks(loopVer);
PowerManager();DoorManager();RefreshLCDs();_store.Set("Status","CurrentPosture",currentPostureName);Storage=_store.ToString();if(
loopVer<3){loopVer++;}else{loopVer=1;}}static Dictionary<string,logItem>logItems=new Dictionary<string,logItem>();class logItem
{public string category="Misc",content="";public int priority=3,maxLife=5,life=0;}void EchoLog(){List<string>toRemove=new
List<string>();for(int i=0;i<=5;i++){foreach(KeyValuePair<string,logItem>item in logItems){if(item.Value.priority==i){Echo(
"("+item.Value.life+") [color=#ffff9600]"+item.Value.category+":[/color] "+item.Value.content);item.Value.life++;if(item.
Value.life==item.Value.maxLife){toRemove.Add(item.Key);}}}foreach(string thing in toRemove){logItems.Remove(thing);}}}void
EchoHeader(int incr){string headerStart;string headerEnd;switch(incr){case 1:headerStart="[color=#ff00ff00]-==[/color]";headerEnd=
"[color=#ff00ff00]==-[/color]";break;case 2:headerStart="[color=#ff00ff00]=-=[/color]";headerEnd="[color=#ff00ff00]=-=[/color]";break;case 3:
headerStart="[color=#ff00ff00]==-[/color]";headerEnd="[color=#ff00ff00]-==[/color]";break;default:headerStart=
"[color=#ff00ff00]===[/color]";headerEnd="[color=#ff00ff00]===[/color]";break;}Echo(headerStart+"GuppOS, Version: "+_Version+headerEnd);}string[]
defaultConfigSections=new string[]{"GuppOS.General","GuppOS.PowerManager","GuppOS.Tanks","GuppOS.Propulsion","GuppOS.LifeSupport",
"GuppOS.Lighting","GuppOS.Doors","GuppOS.Connectors"};List<string>loadedSections=new List<string>();MyIni _config=new MyIni();bool
LoadConfig(){string customData=Me.CustomData;MyIniParseResult result;if(!_config.TryParse(customData,out result)){Echo(
"Could not parse custom data.\n-"+result.ToString());logItems["CfE01"]=new logItem{category="Config",content="Could not parse custom data.\n-"+result.
ToString(),priority=0,maxLife=-1};return false;}List<string>sections=new List<string>();_config.GetSections(sections);foreach(
string section in sections){Echo("Reading section: "+section);try{switch(section){case"GuppOS.General":logAll=_config.Get(
section,"LogAll").ToBoolean();gOSLCDTag=_config.Get(section,"LCDTag").ToString().Trim();ignoreTag=_config.Get(section,
"IgnoreTag").ToString().Trim();break;case"GuppOS.PowerManager":powerManagerEnabled=_config.Get(section,"PowerManagerEnabled").
ToBoolean();mainBatteryGroupName=_config.Get(section,"MainBatteryGroup").ToString().Trim();backupBatteryGroupName=_config.Get(
section,"BackupBatteryGroup").ToString().Trim();mainReactorGroupName=_config.Get(section,"MainReactorGroup").ToString().Trim();
lowPowerPercent=_config.Get(section,"LowPowerLevel").ToSingle();highPowerPercent=_config.Get(section,"HighPowerLevel").ToSingle();break
;case"GuppOS.Tanks":mainFuelTankGroupName=_config.Get(section,"MainFuelTankGroup").ToString().Trim();mainAirTankGroupName
=_config.Get(section,"MainAirTankGroup").ToString().Trim();break;case"GuppOS.Propulsion":mainDriveGroupName=_config.Get(
section,"EpsteinDriveGroup").ToString().Trim();rcsGroupName=_config.Get(section,"RCSGroup").ToString().Trim();gyroGroupName=
_config.Get(section,"GyroGroup").ToString().Trim();break;case"GuppOS.LifeSupport":ventGroupName=_config.Get(section,"VentGroup"
).ToString().Trim();gravGenGroupName=_config.Get(section,"GravGenGroup").ToString().Trim();break;case"GuppOS.Lighting":
lightGroupName=_config.Get(section,"LightGroup").ToString().Trim();navLightTag=_config.Get(section,"NavLightTag").ToString().Trim();
break;case"GuppOS.Doors":doorManagerEnabled=_config.Get(section,"DoorManagerEnabled").ToBoolean();doorGroupName=_config.Get(
section,"DoorGroup").ToString().Trim();doorOpenLength=_config.Get(section,"DoorOpenLength").ToInt16();break;case
"GuppOS.Connectors":connectorGroupName=_config.Get(section,"ConnectorGroup").ToString().Trim();break;default:break;}}catch(Exception exc){
Echo(exc+"\nCould not parse config section: "+section+"\n-Applying default values.");LoadDefaultSection(section);logItems[
"CfE02:"+section]=new logItem{category="Config",content="Config section "+section+" failed to parse; "+exc+
"\n-Default values loaded for section.",priority=0,maxLife=-1};}if(section.Contains("GuppOS.Posture.")){string newPostureName=section.Substring(15);try{
availablePostures.Add(newPostureName,BuildPosture(_config,section));}catch(Exception exc){Echo(exc+"\n-Could not build posture "+
newPostureName);logItems["CfE03:"+section]=new logItem{category="Config",content="Posture "+newPostureName+" failed to build; "+exc,
priority=0,maxLife=-1};}}loadedSections.Add(section);}foreach(string sec in defaultConfigSections){if(!loadedSections.Contains(
sec)){Echo("Could not find config section: "+sec+"\n-Applying default values.");LoadDefaultSection(sec);logItems["CfE04:"+
sec]=new logItem{category="Config",content="Could not find config section: "+sec+"\n-Default values loaded for section.",
priority=0,maxLife=-1};}}logItems["CfS01"]=new logItem{category="Config",content="Custom config applied.",priority=1,maxLife=-1}
;if(availablePostures.Count<=0){Echo("No custom postures found. Loading defaults.");availablePostures=
BuildDefaultPostures();logItems["CfS02"]=new logItem{category="Config",content="Default postures applied",priority=1,maxLife=-1};}else{
logItems["CfS02"]=new logItem{category="Config",content="Custom postures applied",priority=1,maxLife=-1};}BuildBootPosture();
return true;}void LoadDefaults(){foreach(string sec in defaultConfigSections){LoadDefaultSection(sec);}availablePostures=
BuildDefaultPostures();logItems["CfS01"]=new logItem{category="Config",content="Default config applied.",priority=1,maxLife=-1};logItems[
"CfS02"]=new logItem{category="Config",content="Default postures applied",priority=1,maxLife=-1};}void LoadDefaultSection(
string sect){switch(sect){case"GuppOS.General":logAll=true;gOSLCDTag="<GOSLCD>";ignoreTag="<I>";break;case
"GuppOS.PowerManager":powerManagerEnabled=true;mainBatteryGroupName="Main Batteries";backupBatteryGroupName="Backup Batteries";
mainReactorGroupName="ALL";lowPowerPercent=25;highPowerPercent=90;break;case"GuppOS.Tanks":mainFuelTankGroupName="Fuel Tanks";
mainAirTankGroupName="Air Tanks";break;case"GuppOS.Propulsion":mainDriveGroupName="Main Drives";rcsGroupName="RCS Drives";gyroGroupName=
"Gyros";break;case"GuppOS.LifeSupport":ventGroupName="ALL";gravGenGroupName="ALL";break;case"GuppOS.Lighting":lightGroupName=
"ALL";navLightTag="*NAV*";break;case"GuppOS.Doors":doorManagerEnabled=true;doorGroupName="ALL";doorOpenLength=6;break;case
"GuppOS.Connectors":connectorGroupName="ALL";break;}}static string connectorGroupName;ConnectorGroup mainConnectors;class ConnectorGroup:
BlockGroup<IMyShipConnector>{public ConnectorGroup(Program program,string name):base(program,name,new string[]{"Co","Connectors"})
{}public void SetConTag(string conTag,bool enabled){try{foreach(IMyShipConnector connector in thingList){if(connector.
CustomName.Contains(conTag)){connector.Enabled=enabled;}}}catch(Exception exc){logItems[logID[0]+"E04:"+groupName]=new logItem{
category=logID[1],content="Error when setting connector tag status of "+groupName+" -"+exc,priority=1,maxLife=3};}}}static bool
doorManagerEnabled;static string doorGroupName;static int doorOpenLength;class SingleDoor{public string doorName;public IMyDoor doorBlock;
public int timeOpen=0;public bool isLocked;public void SetDoorLock(bool unlockDoor){if(unlockDoor&&isLocked){doorBlock.
ApplyAction("AnyoneCanUse");isLocked=!isLocked;}if(!unlockDoor&&!isLocked){doorBlock.ApplyAction("AnyoneCanUse");isLocked=!isLocked
;}}}SDoorGroup mainSDoors;class SDoorGroup:ThingGroup<SingleDoor>{protected Program program;public SDoorGroup(Program
program,string name):base(name,new string[]{"SD","SingleDoors"}){this.program=program;}public int sDoorsOpen=0,sDoorsFunc=0;
public override void LoadGroup(bool managerStatus=false,bool hardLoad=false){List<IMyDoor>doors=new List<IMyDoor>();if(
groupName=="ALL"){program.GridTerminalSystem.GetBlocksOfType(doors,door=>door.IsSameConstructAs(program.Me)&&!door.CustomName.
Contains(ignoreTag));}else{IMyBlockGroup myBlockGroup=program.GridTerminalSystem.GetBlockGroupWithName(groupName);if(
myBlockGroup==null){logItems[logID[0]+"E01:"+groupName]=new logItem{category=logID[1],content=groupName+" not found.",priority=1,
maxLife=3};return;}myBlockGroup.GetBlocksOfType(doors,door=>door.IsSameConstructAs(program.Me)&&!door.CustomName.Contains(
ignoreTag));}List<SingleDoor>singleDoors=new List<SingleDoor>();if(thingList.Count>0&&!hardLoad){singleDoors.AddList(thingList);}
foreach(IMyDoor door in doors){if(singleDoors.Exists(d=>d.doorName==door.CustomName)){continue;}singleDoors.Add(new SingleDoor{
doorName=door.CustomName,doorBlock=door,isLocked=!program.IfUnlocked(door)});}thingList=singleDoors;ReadSDoors();if(logAll)
logItems[logID[0]+"E01:"+groupName]=new logItem{category=logID[1],content="Single doors loaded.",priority=5,maxLife=1};}public
void ReadSDoors(){try{sDoorsOpen=0;sDoorsFunc=0;foreach(SingleDoor sDoor in thingList){if(sDoor.doorBlock.IsWorking){
sDoorsFunc++;}if(sDoor.doorBlock.OpenRatio>0){sDoorsOpen++;}}}catch(Exception exc){logItems[logID[0]+"E02:"+groupName]=new logItem
{category=logID[1],content="Error when reading "+groupName+" -"+exc,priority=1,maxLife=3};}}public void ActionSDoor(
SingleDoor sDoor){try{if(sDoor.doorBlock.OpenRatio>0){if(sDoor.timeOpen>=doorOpenLength){sDoor.doorBlock.CloseDoor();sDoor.
timeOpen=0;}else{sDoor.timeOpen++;}}}catch(Exception exc){logItems[logID[0]+"E03:"+groupName]=new logItem{category=logID[1],
content="Error when actioning door of "+groupName+" -"+exc,priority=1,maxLife=3};}}public void SetDoorTagLock(string doorTag,
bool unlockDoors){try{foreach(var sDoor in thingList){if(sDoor.doorName.Contains(doorTag)){sDoor.SetDoorLock(unlockDoors);}}
if(logAll)logItems[logID[0]+"E01:"+groupName]=new logItem{category=logID[1],content="Doors with tag "+doorTag+
" lock set to "+!unlockDoors,priority=4,maxLife=1};}catch(Exception exc){logItems[logID[0]+"E04:"+groupName]=new logItem{category=logID
[1],content="Error when setting door tag lock in "+groupName+" -"+exc,priority=1,maxLife=3};}}}void DoorManager(){if(!
doorManagerEnabled){return;}if(mainSDoors.thingList==null){logItems["DME01"]=new logItem{category="PowerManager",content=
"Block group(s) invalid, disabling Power Manager.",priority=1,maxLife=3};powerManagerEnabled=false;return;}foreach(SingleDoor sDoor in mainSDoors.thingList){mainSDoors.
ActionSDoor(sDoor);}if(logAll)logItems["DML01"]=new logItem{category="DoorManager",content="Single doors actioned.",priority=5,
maxLife=1};}bool IfUnlocked(IMyDoor door){ITerminalAction action=door.GetActionWithName("AnyoneCanUse");StringBuilder status=
new StringBuilder();action.WriteValue(door,status);return status.ToString()=="On";}class ThingGroup<thingType>where
thingType:class{public ThingGroup(string name,string[]iD){groupName=name;logID=iD;}public string groupName;public string[]logID;
public List<thingType>thingList=new List<thingType>();public virtual void LoadGroup(bool managerStatus=false,bool hardLoad=
false){}}class BlockGroup<blockType>:ThingGroup<blockType>where blockType:class,IMyFunctionalBlock{protected Program program;
public BlockGroup(Program program,string name,string[]iD):base(name,iD){this.program=program;groupName=name;logID=iD;}public
bool?groupEnabled=null;public override void LoadGroup(bool managerStatus=false,bool hardLoad=false){List<blockType>blocks=
new List<blockType>();if(groupName=="ALL"){program.GridTerminalSystem.GetBlocksOfType(blocks,b=>b.IsSameConstructAs(program
.Me)&&!b.CustomName.Contains(ignoreTag));}else{IMyBlockGroup myBlockGroup=program.GridTerminalSystem.
GetBlockGroupWithName(groupName);if(myBlockGroup==null){logItems[logID[0]+"E01:"+groupName]=new logItem{category=logID[1],content=groupName+
" not found.",priority=1,maxLife=3};return;}myBlockGroup.GetBlocksOfType(blocks,b=>b.IsSameConstructAs(program.Me)&&!b.CustomName.
Contains(ignoreTag));}thingList=blocks;ReadBlocks();if(logAll)logItems[logID[0]+"L01:"+groupName]=new logItem{category=logID[1],
content=groupName+" loaded.",priority=5,maxLife=1};}public void ReadBlocks(){try{ReadStep();}catch(Exception exc){logItems[
logID[0]+"E02:"+groupName]=new logItem{category=logID[1],content="Error when reading "+groupName+" -"+exc,priority=1,maxLife=
3};}}public virtual void ReadStep(){}public void SetGroupEnabled(bool enable){try{if(enable!=groupEnabled){foreach(
blockType drive in thingList){drive.Enabled=enable;}groupEnabled=enable;}}catch(Exception exc){logItems[logID[0]+"E03:"+groupName
]=new logItem{category=logID[1],content="Error when enabling/disabling "+groupName+" -"+exc,priority=1,maxLife=3};}}}void
InitBlocks(){mainBatteries=new BatteryGroup(this,mainBatteryGroupName);backupBatteries=new BatteryGroup(this,
backupBatteryGroupName);mainReactors=new ReactorGroup(this,mainReactorGroupName);allGOSLCDs=LoadLCDs();mainDrives=new DriveGroup(this,
mainDriveGroupName);rcsThrusters=new DriveGroup(this,rcsGroupName);mainGyros=new GyroGroup(this,gyroGroupName);mainLights=new LightGroup(
this,lightGroupName);mainFuelTanks=new GasTankGroup(this,mainFuelTankGroupName);mainAirTanks=new GasTankGroup(this,
mainAirTankGroupName);mainSDoors=new SDoorGroup(this,doorGroupName);mainAirVents=new VentGroup(this,ventGroupName);mainGravGens=new
GravGenGroup(this,gravGenGroupName);mainConnectors=new ConnectorGroup(this,connectorGroupName);}void LoadBlocks(int sec){switch(sec)
{case 1:mainBatteries.LoadGroup(powerManagerEnabled);backupBatteries.LoadGroup(powerManagerEnabled);mainReactors.
LoadGroup(powerManagerEnabled);allGOSLCDs=LoadLCDs();mainDrives.LoadGroup();rcsThrusters.LoadGroup();mainGyros.LoadGroup();break;
case 2:mainLights.LoadGroup();mainFuelTanks.LoadGroup();mainAirTanks.LoadGroup();break;case 3:mainSDoors.LoadGroup(
doorManagerEnabled);mainAirVents.LoadGroup();mainGravGens.LoadGroup();mainConnectors.LoadGroup();break;default:break;}}static string
gOSLCDTag,basicFont="Monospace";static Color titleColour=new Color(255,255,255,255),basicColour=new Color(255,255,255,255),
postureColour=new Color(255,128,192,255),enabledColour=new Color(0,255,0,255),disabledColour=new Color(255,0,0,255),onlineColour=new
Color(128,255,128,255),offlineColour=new Color(255,128,128,255),goodColour=new Color(0,255,128,255),cautionColour=new Color(
255,128,64,255),mildCautionColour=new Color(255,255,128,255),autoModeColour=new Color(0,255,64,255),rechargeModeColour=new
Color(255,255,0,255),dischargeModeColour=new Color(0,255,255,255),defModeColour=new Color(0,255,64,255),stockpileModeColour=
new Color(128,255,255,255);List<GOSLCD>allGOSLCDs;class GOSLCD{public string lcdType="";public IMyTextSurface lcdScreen;
public RectangleF drawableSurface;public float fontScale=1.0f;public float[]margins;public void AdjustForMargins(){
drawableSurface.Y+=margins[0];drawableSurface.Height-=margins[0];drawableSurface.Height-=margins[1];drawableSurface.X+=margins[2];
drawableSurface.Width-=margins[2];drawableSurface.Width-=margins[3];}}MyIni _LCDConfig=new MyIni();List<GOSLCD>LoadLCDs(){List<GOSLCD>
gLCDs=new List<GOSLCD>();List<IMyTerminalBlock>lcdBlocks=new List<IMyTerminalBlock>();GridTerminalSystem.GetBlocksOfType(
lcdBlocks,block=>block.CustomName.Contains(gOSLCDTag)&&!block.CustomName.Contains(ignoreTag)&&block.IsSameConstructAs(Me));
foreach(IMyTerminalBlock block in lcdBlocks){string lcdCustomData=block.CustomData;IMyTextSurfaceProvider lcdProvider=block as
IMyTextSurfaceProvider;MyIniParseResult lcdResult;if(lcdProvider!=null&&lcdProvider.SurfaceCount>0){if(!_LCDConfig.TryParse(lcdCustomData,out
lcdResult)){logItems["LCDE02"]=new logItem{category="LCDs",content="Could not parse custom data for "+block.CustomName+" -"+
lcdResult.ToString(),priority=1,maxLife=3};continue;}if(_LCDConfig.ContainsSection("GuppOS.LCD")){int screenIndex;List<MyIniKey>
sectKeys=new List<MyIniKey>();_LCDConfig.GetKeys("GuppOS.LCD",sectKeys);foreach(MyIniKey key in sectKeys){if(int.TryParse(key.
Name.Trim().Substring(4),out screenIndex)){try{string[]LCDSetting=_LCDConfig.Get(key).ToString().Split(':');if(LCDSetting.
Length<3){throw new Exception("Invalid config format for "+key.ToString());}string[]LCDMargins=LCDSetting[2].Split(',');if(
LCDMargins.Length<4){throw new Exception("Invalid config format for "+key.ToString());}GOSLCD newLCD=new GOSLCD{lcdType=LCDSetting
[0].Trim(),lcdScreen=lcdProvider.GetSurface(screenIndex),fontScale=Convert.ToSingle(LCDSetting[1].Trim()),margins=new
float[]{Convert.ToSingle(LCDMargins[0].Trim()),Convert.ToSingle(LCDMargins[1].Trim()),Convert.ToSingle(LCDMargins[2].Trim()),
Convert.ToSingle(LCDMargins[3].Trim())}};newLCD.drawableSurface=new RectangleF((newLCD.lcdScreen.TextureSize-newLCD.lcdScreen.
SurfaceSize)/2f,newLCD.lcdScreen.SurfaceSize);newLCD.AdjustForMargins();gLCDs.Add(newLCD);}catch(Exception exc){logItems["LCDE06"]=
new logItem{category="LCDs",content="Error parsing custom data for "+block.CustomName+": "+exc,priority=1,maxLife=3};
continue;}}else{logItems["LCDE05"]=new logItem{category="LCDs",content="Error parsing screen index in "+block.CustomName+
" config line "+key,priority=1,maxLife=3};continue;}}}else{logItems["LCDE04"]=new logItem{category="LCDs",content=block.CustomName+
" missing LCD config data.",priority=1,maxLife=3};continue;}}else{logItems["LCDE01"]=new logItem{category="LCDs",content=block.CustomName+
" does not have LCDs",priority=1,maxLife=3};continue;}}if(logAll)logItems["LCDL01"]=new logItem{category="LCDs",content=gLCDs.Count+
" GuppOS LCDs loaded.",priority=5,maxLife=2};return gLCDs;}void RefreshLCDs(){foreach(GOSLCD lcd in allGOSLCDs){switch(lcd.lcdType){case"Log":
DrawLogLCD(lcd);break;case"Info":DrawInfoLCD(lcd);break;default:break;}}}void DrawLogLCD(GOSLCD lcd){lcd.lcdScreen.ContentType=
ContentType.SCRIPT;lcd.lcdScreen.Script="";int logLineMaxChar=(int)Math.Floor(lcd.drawableSurface.Width/(19.4*0.8*lcd.fontScale));
using(var frame=lcd.lcdScreen.DrawFrame()){float logLineY=0;frame.Add(new MySprite{Type=SpriteType.TEXT,Data=
"===GuppOS Log===",Position=new Vector2(lcd.drawableSurface.Width/2,logLineY)+lcd.drawableSurface.Position,Alignment=TextAlignment.CENTER,
RotationOrScale=1.0f*lcd.fontScale,Color=titleColour,FontId=basicFont});logLineY+=35*lcd.fontScale;for(int i=0;i<=5;i++){foreach(
KeyValuePair<string,logItem>item in logItems){if(item.Value.priority==i){StringBuilder logLine=new StringBuilder("("+item.Value.life
+") "+item.Value.category+": "+item.Value.content);float lineHeight=25*lcd.fontScale;if(logLine.Length>logLineMaxChar){
int lineBreaks=logLine.Length/logLineMaxChar;for(int l=1;l<=lineBreaks;l++){logLine.Insert(logLineMaxChar*l,"\n");
lineHeight+=25*lcd.fontScale;}}frame.Add(new MySprite{Type=SpriteType.TEXT,Data=logLine.ToString(),Position=new Vector2(0,logLineY
)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.8f*lcd.fontScale,Color=basicColour,FontId=
basicFont});logLineY+=lineHeight;}}}}if(logAll)logItems["LCDL02"]=new logItem{category="LCDs",content="Log LCD drawn.",priority=5
,maxLife=2};}void DrawInfoLCD(GOSLCD lcd){lcd.lcdScreen.ContentType=ContentType.SCRIPT;lcd.lcdScreen.Script="";using(var
frame=lcd.lcdScreen.DrawFrame()){float spriteXPos=0;float spriteYPos=0;Color spriteColour;string spriteText;spriteXPos=lcd.
drawableSurface.Width/2;spriteYPos=0*lcd.fontScale;spriteColour=titleColour;spriteText="===GuppOS Ship Status===";frame.Add(new
MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment
=TextAlignment.CENTER,RotationOrScale=1.0f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=
30*lcd.fontScale;spriteColour=postureColour;spriteText=$"--Posture: {currentPostureName}--";frame.Add(new MySprite{Type=
SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.
CENTER,RotationOrScale=0.9f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos=0;spriteYPos+=25*lcd.fontScale;
spriteColour=titleColour;spriteText="Power Manager:";frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new
Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.8f*lcd.fontScale,
Color=spriteColour,FontId=basicFont});spriteXPos=lcd.drawableSurface.Width/2;spriteYPos+=0*lcd.fontScale;if(
powerManagerEnabled){spriteColour=enabledColour;spriteText="Enabled";}else{spriteColour=disabledColour;spriteText="Disabled";}frame.Add(new
MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment
=TextAlignment.CENTER,RotationOrScale=0.8f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos=lcd.
drawableSurface.Width;spriteYPos+=5*lcd.fontScale;if(mainReactors.groupEnabled==true){spriteColour=onlineColour;spriteText=
"Reactors Online";}else{spriteColour=offlineColour;spriteText="Reactors Offline";}frame.Add(new MySprite{Type=SpriteType.TEXT,Data=
spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.RIGHT,RotationOrScale=
0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos=5;spriteYPos+=20*lcd.fontScale;spriteColour=titleColour;
spriteText="Batteries  | Total | Functional | Level | Mode";frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=
new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.
fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=15*lcd.fontScale;if(mainBatteries.batteryGroupMode==
ChargeMode.Recharge){spriteColour=rechargeModeColour;}else if(mainBatteries.batteryGroupMode==ChargeMode.Discharge){spriteColour=
dischargeModeColour;}else{spriteColour=autoModeColour;}spriteText=$" Main      | {CentreText(mainBatteries.groupTotBats.ToString(),5)} | {CentreText(mainBatteries.groupFuncBats.ToString(),10)} | {CentreText(mainBatteries.groupPowerLevel.ToString("F0")+"%",5)} | {mainBatteries.batteryGroupMode.ToString()}"
;frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.
drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});
spriteXPos+=0;spriteYPos+=15*lcd.fontScale;if(backupBatteries.batteryGroupMode==ChargeMode.Recharge){spriteColour=
rechargeModeColour;}else if(backupBatteries.batteryGroupMode==ChargeMode.Discharge){spriteColour=dischargeModeColour;}else{spriteColour=
autoModeColour;}spriteText=$" Backup    | {CentreText(backupBatteries.groupTotBats.ToString(),5)} | {CentreText(backupBatteries.groupFuncBats.ToString(),10)} | {CentreText(backupBatteries.groupPowerLevel.ToString("F0")+"%",5)} | {backupBatteries.batteryGroupMode.ToString()}"
;frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.
drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});
spriteXPos=0;spriteYPos+=20*lcd.fontScale;spriteColour=titleColour;spriteText="Main Fuel Tanks:";frame.Add(new MySprite{Type=
SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.
LEFT,RotationOrScale=0.8f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=5;spriteYPos+=20*lcd.fontScale;if(
mainFuelTanks.level>20){spriteColour=goodColour;}else{spriteColour=cautionColour;}spriteText="Filled: "+mainFuelTanks.level.ToString(
"F0")+"%";frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.
drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.7f*lcd.fontScale,Color=spriteColour,FontId=basicFont});
spriteXPos+=0;spriteYPos+=17.5f*lcd.fontScale;if(mainFuelTanks.stockpileOn==true){spriteColour=stockpileModeColour;}else{
spriteColour=defModeColour;}spriteText="Stockpile: "+mainFuelTanks.stockpileOn;frame.Add(new MySprite{Type=SpriteType.TEXT,Data=
spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=
0.7f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=17.5f*lcd.fontScale;spriteColour=
basicColour;spriteText=$"Consumption: {mainFuelTanks.flowRate:F1}L/s";frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,
Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.
fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=15*lcd.fontScale;spriteColour=basicColour;spriteText=
$"Stored: {mainFuelTanks.stored/1000:F1}kL";frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.
drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});
spriteXPos+=0;spriteYPos+=15*lcd.fontScale;spriteColour=basicColour;spriteText=$"Capacity: {mainFuelTanks.capacity/1000:F1}kL";
frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.
Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos=lcd.
drawableSurface.Width/2;spriteYPos+=-85*lcd.fontScale;spriteColour=titleColour;spriteText="Main Air Tanks:";frame.Add(new MySprite{Type
=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=
TextAlignment.LEFT,RotationOrScale=0.8f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=5;spriteYPos+=20*lcd.
fontScale;if(mainAirTanks.level>20){spriteColour=goodColour;}else{spriteColour=cautionColour;}spriteText="Filled: "+mainAirTanks.
level.ToString("F0")+"%";frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,
spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.7f*lcd.fontScale,Color=spriteColour,FontId
=basicFont});spriteXPos+=0;spriteYPos+=17.5f*lcd.fontScale;if(mainAirTanks.stockpileOn==true){spriteColour=
stockpileModeColour;}else{spriteColour=defModeColour;}spriteText="Stockpile: "+mainAirTanks.stockpileOn;frame.Add(new MySprite{Type=
SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.
LEFT,RotationOrScale=0.7f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=17.5f*lcd.fontScale;
spriteColour=basicColour;spriteText=$"Consumption: {mainAirTanks.flowRate:F1}L/s";frame.Add(new MySprite{Type=SpriteType.TEXT,Data=
spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=
0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=15*lcd.fontScale;spriteColour=basicColour
;spriteText=$"Stored: {mainAirTanks.stored/1000:F1}kL";frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,
Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.
fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=15*lcd.fontScale;spriteColour=basicColour;spriteText=
$"Capacity: {mainAirTanks.capacity/1000:F1}kL";frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.
drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});
spriteXPos=0;spriteYPos+=20*lcd.fontScale;spriteColour=titleColour;spriteText="Propulsion:";frame.Add(new MySprite{Type=SpriteType
.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.
LEFT,RotationOrScale=0.8f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=5;spriteYPos+=20*lcd.fontScale;if(
mainDrives.groupEnabled==true){spriteColour=onlineColour;spriteText="Main Drives: Online";}else{spriteColour=offlineColour;
spriteText="Main Drives: Offline";}frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,
spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=spriteColour,FontId
=basicFont});spriteXPos+=0;spriteYPos+=15*lcd.fontScale;if(rcsThrusters.groupEnabled==true){spriteColour=onlineColour;
spriteText="RCS Thrusters: Online";}else{spriteColour=offlineColour;spriteText="RCS Thrusters: Offline";}frame.Add(new MySprite{
Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=
TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=15*lcd.
fontScale;if(mainGyros.groupEnabled==true){spriteColour=onlineColour;spriteText="Gyroscopes: Online";}else{spriteColour=
offlineColour;spriteText="Gyroscopes: Offline";}frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(
spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=
spriteColour,FontId=basicFont});spriteXPos=lcd.drawableSurface.Width/2;spriteYPos+=-50*lcd.fontScale;spriteColour=titleColour;
spriteText="Life Support:";frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)
+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.8f*lcd.fontScale,Color=spriteColour,FontId=
basicFont});spriteXPos+=5;spriteYPos+=20*lcd.fontScale;if(mainAirVents.ventsPressurized==true){spriteColour=onlineColour;
spriteText="Vent Mode: Pressurize";}else{spriteColour=mildCautionColour;spriteText="Vent Mode: Depressurize";}frame.Add(new
MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment
=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=15
*lcd.fontScale;if(mainAirVents.leakyVents.Count>0){spriteColour=cautionColour;spriteText="Leaks Detected: "+mainAirVents.
leakyVents.Count;}else{spriteColour=goodColour;spriteText="No Leaks Detected";}frame.Add(new MySprite{Type=SpriteType.TEXT,Data=
spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=
0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=15*lcd.fontScale;if(mainGravGens.
groupEnabled==true){spriteColour=onlineColour;spriteText="Gravity Gens: Online";}else{spriteColour=offlineColour;spriteText=
"Gravity Gens: Offline";}frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.
drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});
spriteXPos=0;spriteYPos+=20*lcd.fontScale;spriteColour=titleColour;spriteText="Door Manager:";frame.Add(new MySprite{Type=
SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.
LEFT,RotationOrScale=0.8f*lcd.fontScale,Color=spriteColour,FontId=basicFont});spriteXPos=lcd.drawableSurface.Width/2;
spriteYPos+=0*lcd.fontScale;if(doorManagerEnabled){spriteColour=enabledColour;spriteText="Enabled";}else{spriteColour=
disabledColour;spriteText="Disabled";}frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,
spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.CENTER,RotationOrScale=0.8f*lcd.fontScale,Color=spriteColour,
FontId=basicFont});spriteXPos=5;spriteYPos+=20*lcd.fontScale;if(mainSDoors.sDoorsFunc<mainSDoors.thingList.Count){spriteColour
=cautionColour;spriteText="Doors Disabled: "+(mainSDoors.thingList.Count-mainSDoors.sDoorsFunc);}else{spriteColour=
goodColour;spriteText="All Doors Operational";}frame.Add(new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(
spriteXPos,spriteYPos)+lcd.drawableSurface.Position,Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=
spriteColour,FontId=basicFont});spriteXPos+=0;spriteYPos+=20*lcd.fontScale;if(mainSDoors.sDoorsOpen>0){spriteColour=
mildCautionColour;spriteText="Doors Open: "+mainSDoors.sDoorsOpen;}else{spriteColour=goodColour;spriteText="All Doors Closed";}frame.Add(
new MySprite{Type=SpriteType.TEXT,Data=spriteText,Position=new Vector2(spriteXPos,spriteYPos)+lcd.drawableSurface.Position,
Alignment=TextAlignment.LEFT,RotationOrScale=0.6f*lcd.fontScale,Color=spriteColour,FontId=basicFont});}if(logAll)logItems[
"LCDL03"]=new logItem{category="LCDs",content="Info LCD drawn.",priority=5,maxLife=2};}string CentreText(string text,int width){
return$"{text.PadLeft((width+text.Length)/2).PadRight(width)}";}static string ventGroupName,gravGenGroupName;VentGroup
mainAirVents;class VentGroup:BlockGroup<IMyAirVent>{public VentGroup(Program program,string name):base(program,name,new string[]{
"LS","LifeSupport"}){}public List<IMyAirVent>leakyVents=new List<IMyAirVent>();public bool?ventsPressurized;public override
void ReadStep(){leakyVents.Clear();foreach(IMyAirVent vent in thingList){if(!vent.CanPressurize){leakyVents.Add(vent);}}if(
logAll&&leakyVents.Count>0)logItems["VeL02"]=new logItem{category="Vents",content="Air leak(s) detected.",priority=4,maxLife=1
};}public void VentsMode(bool pressurize){try{if(pressurize!=ventsPressurized){foreach(IMyAirVent vent in thingList){vent
.Depressurize=!pressurize;}ventsPressurized=pressurize;}}catch(Exception exc){logItems[logID[0]+"E04:"+groupName]=new
logItem{category=logID[1],content="Error when setting mode of "+groupName+" -"+exc,priority=1,maxLife=3};}}}GravGenGroup
mainGravGens;class GravGenGroup:BlockGroup<IMyGravityGeneratorBase>{public GravGenGroup(Program program,string name):base(program,
name,new string[]{"LS","LifeSupport"}){}}static string lightGroupName,navLightTag;class LightSetting{public bool enabled;
public Color colour=Color.White;}LightGroup mainLights;class LightGroup:BlockGroup<IMyLightingBlock>{public LightGroup(Program
program,string name):base(program,name,new string[]{"LL","Lighting"}){}public void SetLightTag(string lightTag,LightSetting
setting){try{foreach(IMyLightingBlock light in thingList){if(light.CustomName.Contains(lightTag)){light.Enabled=setting.enabled
;light.SetValue("Color",setting.colour);}}}catch(Exception exc){logItems[logID[0]+"E04:"+groupName]=new logItem{category=
logID[1],content="Error when setting light tag status of "+groupName+" -"+exc,priority=1,maxLife=3};}}public void
SetNavLights(bool on){try{foreach(IMyLightingBlock light in thingList){if(light.CustomName.Contains(navLightTag)){if(light.
CustomName.ToLower().Contains("port")){light.SetValue("Color",Color.Red);light.Enabled=on;}else if(light.CustomName.ToLower().
Contains("starboard")){light.SetValue("Color",Color.Lime);light.Enabled=on;}}}}catch(Exception exc){logItems[logID[0]+"E05:"+
groupName]=new logItem{category=logID[1],content="Error when setting nav light status of "+groupName+" -"+exc,priority=1,maxLife=
3};}}}string currentPostureName;Posture currentPosture;Dictionary<string,Posture>availablePostures=new Dictionary<string,
Posture>();class Posture{public Posture(){}public Posture(Posture source){inherits=source.inherits;overrideBatteries=source.
overrideBatteries;mainBatteryMode=source.mainBatteryMode;backupBatteryMode=source.backupBatteryMode;overrideReactors=source.
overrideReactors;mainReactorsStatus=source.mainReactorsStatus;stockpileFuel=source.stockpileFuel;stockpileAir=source.stockpileAir;
enableEpsteins=source.enableEpsteins;enableRCS=source.enableRCS;enableGyros=source.enableGyros;pressurizeVents=source.pressurizeVents;
generateGravity=source.generateGravity;navLightsOn=source.navLightsOn;lightTagSetting=new Dictionary<string,LightSetting>(source.
lightTagSetting);conTagSetting=new Dictionary<string,bool>(source.conTagSetting);}public string inherits="";public bool
overrideBatteries=false,overrideReactors=false,mainReactorsStatus=false,stockpileFuel=false,stockpileAir=false,enableEpsteins=true,
enableRCS=true,enableGyros=true,pressurizeVents=true,generateGravity=true,navLightsOn=true;public ChargeMode mainBatteryMode=
ChargeMode.Auto,backupBatteryMode=ChargeMode.Recharge;public Dictionary<string,LightSetting>lightTagSetting=new Dictionary<string,
LightSetting>();public Dictionary<string,bool>conTagSetting=new Dictionary<string,bool>();}Posture BuildPosture(MyIni _ini,string
section){Posture newPosture;List<MyIniKey>newPostureKeys=new List<MyIniKey>();_ini.GetKeys(section,newPostureKeys);if(_ini.
ContainsKey(section,"Inherits")){string inheriteeName=_ini.Get(section,"Inherits").ToString().Trim();if(availablePostures.
ContainsKey(inheriteeName)){newPosture=new Posture(availablePostures[inheriteeName]);}else{throw new Exception(
"Inheritee not found.");}}else{newPosture=new Posture();}if(_ini.ContainsKey(section,"OverrideBatteries")){newPosture.overrideBatteries=_ini.
Get(section,"OverrideBatteries").ToBoolean();if(newPosture.overrideBatteries){if(_ini.ContainsKey(section,"MainBatteryMode"
)){newPosture.mainBatteryMode=(ChargeMode)Enum.Parse(typeof(ChargeMode),_ini.Get(section,"MainBatteryMode").ToString().
Trim());}if(_ini.ContainsKey(section,"BackupBatteryMode")){newPosture.backupBatteryMode=(ChargeMode)Enum.Parse(typeof(
ChargeMode),_ini.Get(section,"BackupBatteryMode").ToString().Trim());}}}if(_ini.ContainsKey(section,"OverrideReactors")){
newPosture.overrideReactors=_ini.Get(section,"OverrideReactors").ToBoolean();if(newPosture.overrideReactors){if(_ini.ContainsKey(
section,"MainReactorStatus")){newPosture.mainReactorsStatus=_ini.Get(section,"MainReactorStatus").ToBoolean();}}}if(_ini.
ContainsKey(section,"StockpileFuel")){newPosture.stockpileFuel=_ini.Get(section,"StockpileFuel").ToBoolean();}if(_ini.ContainsKey(
section,"StockpileAir")){newPosture.stockpileAir=_ini.Get(section,"StockpileAir").ToBoolean();}if(_ini.ContainsKey(section,
"EpsteinsEnabled")){newPosture.enableEpsteins=_ini.Get(section,"EpsteinsEnabled").ToBoolean();}if(_ini.ContainsKey(section,"RCSEnabled"))
{newPosture.enableRCS=_ini.Get(section,"RCSEnabled").ToBoolean();}if(_ini.ContainsKey(section,"GyrosEnabled")){newPosture
.enableGyros=_ini.Get(section,"GyrosEnabled").ToBoolean();}if(_ini.ContainsKey(section,"PressurizeVents")){newPosture.
pressurizeVents=_ini.Get(section,"PressurizeVents").ToBoolean();}if(_ini.ContainsKey(section,"GravEnabled")){newPosture.generateGravity
=_ini.Get(section,"GravEnabled").ToBoolean();}if(_ini.ContainsKey(section,"NavLightsOn")){newPosture.navLightsOn=_ini.Get
(section,"NavLightsOn").ToBoolean();}foreach(var key in newPostureKeys){if(key.Name.Contains("LightSetting")){
LightSetting newSetting=new LightSetting();string[]lightSetting=_ini.Get(key).ToString().Split(':');newSetting.enabled=Convert.
ToBoolean(lightSetting[1].Trim());if(newSetting.enabled){string[]lightSettingColours=lightSetting[2].Trim().Split(',');newSetting
.colour=new Color(Convert.ToInt16(lightSettingColours[0].Trim()),Convert.ToInt16(lightSettingColours[1].Trim()),Convert.
ToInt16(lightSettingColours[2].Trim()),255);}newPosture.lightTagSetting[lightSetting[0].Trim()]=newSetting;}if(key.Name.
Contains("ConSetting")){bool conEnabled;string[]conSetting=_ini.Get(key).ToString().Split(':');conEnabled=Convert.ToBoolean(
conSetting[1].Trim());newPosture.conTagSetting[conSetting[0].Trim()]=conEnabled;}}return newPosture;}void FixPosture(string name){
currentPostureName=name;currentPosture=availablePostures[name];if(logAll)logItems["PsL00"]=new logItem{category="Postures",content=
"Setting posture to "+name,priority=4,maxLife=5};if(currentPosture.overrideBatteries){mainBatteries.SetBatMode(currentPosture.mainBatteryMode
);backupBatteries.SetBatMode(currentPosture.backupBatteryMode);}if(currentPosture.overrideReactors){mainReactors.
SetGroupEnabled(currentPosture.mainReactorsStatus);}mainFuelTanks.SetStockpile(currentPosture.stockpileFuel);mainAirTanks.SetStockpile(
currentPosture.stockpileAir);mainDrives.SetGroupEnabled(currentPosture.enableEpsteins);rcsThrusters.SetGroupEnabled(currentPosture.
enableRCS);mainGyros.SetGroupEnabled(currentPosture.enableGyros);mainAirVents.VentsMode(currentPosture.pressurizeVents);
mainGravGens.SetGroupEnabled(currentPosture.generateGravity);mainLights.SetNavLights(currentPosture.navLightsOn);foreach(var kvp in
currentPosture.lightTagSetting){mainLights.SetLightTag(kvp.Key,kvp.Value);}foreach(var kvp in currentPosture.conTagSetting){
mainConnectors.SetConTag(kvp.Key,kvp.Value);}}void BuildBootPosture(){availablePostures["Standby"]=new Posture{overrideBatteries=false
,overrideReactors=false,stockpileFuel=false,stockpileAir=false,enableEpsteins=false,enableRCS=false,enableGyros=false,
pressurizeVents=true,generateGravity=true,navLightsOn=false,lightTagSetting=new Dictionary<string,LightSetting>{{"*IN*",new
LightSetting{enabled=true,colour=new Color(255,255,255,255)}},{"*OUT*",new LightSetting{enabled=false}},{"*DOCK*",new LightSetting{
enabled=false}}},conTagSetting=new Dictionary<string,bool>{{"|Dockers|",false},{"|Docks|",true}}};}Dictionary<string,Posture>
BuildDefaultPostures(){Dictionary<string,Posture>defaultPostures=new Dictionary<string,Posture>();defaultPostures["Cruise"]=new Posture{
overrideBatteries=false,overrideReactors=false,stockpileFuel=false,stockpileAir=false,enableEpsteins=true,enableRCS=true,enableGyros=true
,pressurizeVents=true,generateGravity=true,navLightsOn=true,lightTagSetting=new Dictionary<string,LightSetting>{{"*IN*",
new LightSetting{enabled=true,colour=new Color(255,255,255,255)}},{"*OUT*",new LightSetting{enabled=true,colour=new Color(
255,255,255,255)}},{"*DOCK*",new LightSetting{enabled=false}}},conTagSetting=new Dictionary<string,bool>{{"|Dockers|",false
},{"|Docks|",true}}};defaultPostures["Maneuver"]=new Posture{overrideBatteries=false,overrideReactors=false,stockpileFuel
=false,stockpileAir=false,enableEpsteins=false,enableRCS=true,enableGyros=true,pressurizeVents=true,generateGravity=true,
navLightsOn=true,lightTagSetting=new Dictionary<string,LightSetting>{{"*IN*",new LightSetting{enabled=true,colour=new Color(255,255
,255,255)}},{"*OUT*",new LightSetting{enabled=true,colour=new Color(255,255,255,255)}},{"*DOCK*",new LightSetting{enabled
=false}}},conTagSetting=new Dictionary<string,bool>{{"|Dockers|",false},{"|Docks|",true}}};defaultPostures[
"StealthCruise"]=new Posture{overrideBatteries=false,overrideReactors=false,stockpileFuel=false,stockpileAir=false,enableEpsteins=true,
enableRCS=true,enableGyros=true,pressurizeVents=true,generateGravity=true,navLightsOn=false,lightTagSetting=new Dictionary<string
,LightSetting>{{"*IN*",new LightSetting{enabled=true,colour=new Color(200,200,255,255)}},{"*OUT*",new LightSetting{
enabled=false}},{"*DOCK*",new LightSetting{enabled=false}}},conTagSetting=new Dictionary<string,bool>{{"|Dockers|",false},{
"|Docks|",true}}};defaultPostures["StealthManeuver"]=new Posture{overrideBatteries=false,overrideReactors=false,stockpileFuel=
false,stockpileAir=false,enableEpsteins=false,enableRCS=true,enableGyros=true,pressurizeVents=true,generateGravity=true,
navLightsOn=false,lightTagSetting=new Dictionary<string,LightSetting>{{"*IN*",new LightSetting{enabled=true,colour=new Color(200,
200,255,255)}},{"*OUT*",new LightSetting{enabled=false}},{"*DOCK*",new LightSetting{enabled=false}}},conTagSetting=new
Dictionary<string,bool>{{"|Dockers|",false},{"|Docks|",true}}};defaultPostures["Combat"]=new Posture{overrideBatteries=true,
mainBatteryMode=ChargeMode.Auto,backupBatteryMode=ChargeMode.Auto,overrideReactors=true,mainReactorsStatus=true,stockpileFuel=false,
stockpileAir=false,enableEpsteins=true,enableRCS=true,enableGyros=true,pressurizeVents=false,generateGravity=false,navLightsOn=false
,lightTagSetting=new Dictionary<string,LightSetting>{{"*IN*",new LightSetting{enabled=true,colour=new Color(255,0,0,255)}
},{"*OUT*",new LightSetting{enabled=false}},{"*DOCK*",new LightSetting{enabled=false}}},conTagSetting=new Dictionary<
string,bool>{{"|Dockers|",false},{"|Docks|",true}}};defaultPostures["Docking"]=new Posture{overrideBatteries=false,
overrideReactors=false,stockpileFuel=false,stockpileAir=false,enableEpsteins=false,enableRCS=true,enableGyros=true,pressurizeVents=true,
generateGravity=true,navLightsOn=true,lightTagSetting=new Dictionary<string,LightSetting>{{"*IN*",new LightSetting{enabled=true,colour=
new Color(255,255,255,255)}},{"*OUT*",new LightSetting{enabled=true,colour=new Color(255,255,255,255)}},{"*DOCK*",new
LightSetting{enabled=true,colour=new Color(255,255,255,255)}}},conTagSetting=new Dictionary<string,bool>{{"|Dockers|",true},{
"|Docks|",true}}};defaultPostures["Docked"]=new Posture{overrideBatteries=true,mainBatteryMode=ChargeMode.Recharge,
backupBatteryMode=ChargeMode.Recharge,overrideReactors=true,mainReactorsStatus=false,stockpileFuel=true,stockpileAir=true,enableEpsteins=
false,enableRCS=false,enableGyros=false,pressurizeVents=true,generateGravity=false,navLightsOn=true,lightTagSetting=new
Dictionary<string,LightSetting>{{"*IN*",new LightSetting{enabled=true,colour=new Color(255,255,255,255)}},{"*OUT*",new
LightSetting{enabled=true,colour=new Color(255,255,255,255)}},{"*DOCK*",new LightSetting{enabled=true,colour=new Color(255,255,255,
255)}}},conTagSetting=new Dictionary<string,bool>{{"|Dockers|",true},{"|Docks|",true}}};return defaultPostures;}static bool
powerManagerEnabled;static string mainBatteryGroupName,backupBatteryGroupName,mainReactorGroupName;static float lowPowerPercent,
highPowerPercent;BatteryGroup mainBatteries;BatteryGroup backupBatteries;class BatteryGroup:BlockGroup<IMyBatteryBlock>{public
BatteryGroup(Program program,string name):base(program,name,new string[]{"Ba","Batteries"}){}public ChargeMode?batteryGroupMode=null
;public float groupPowerCapacity,groupPowerStored,groupPowerLevel;public int groupFuncBats,groupTotBats;public override
void ReadStep(){groupPowerCapacity=0;groupPowerStored=0;groupPowerLevel=0;groupFuncBats=0;groupTotBats=thingList.Count;
foreach(IMyBatteryBlock bat in thingList){if(bat.IsWorking){groupPowerCapacity+=bat.MaxStoredPower;groupPowerStored+=bat.
CurrentStoredPower;groupFuncBats++;}}groupPowerLevel=groupPowerStored/groupPowerCapacity*100;}public void SetBatMode(ChargeMode mode){try{
if(mode!=batteryGroupMode){foreach(IMyBatteryBlock bat in thingList){bat.ChargeMode=mode;}}batteryGroupMode=mode;}catch(
Exception exc){logItems[logID[0]+"E04:"+groupName]=new logItem{category=logID[1],content="Error when setting mode of "+groupName+
" -"+exc,priority=1,maxLife=3};}}}ReactorGroup mainReactors;class ReactorGroup:BlockGroup<IMyReactor>{public ReactorGroup(
Program program,string name):base(program,name,new string[]{"Re","Reactors"}){}}void PowerManager(){if(!powerManagerEnabled){
return;}if(mainBatteries.thingList==null||backupBatteries.thingList==null||mainReactors.thingList==null){logItems["PME01"]=new
logItem{category="PowerManager",content="Block group(s) invalid, disabling Power Manager.",priority=1,maxLife=3};
powerManagerEnabled=false;return;}if(!currentPosture.overrideBatteries){mainBatteries.SetBatMode(ChargeMode.Auto);}if(mainBatteries.
groupPowerLevel>=highPowerPercent){if(!currentPosture.overrideReactors){mainReactors.SetGroupEnabled(false);}if(!currentPosture.
overrideBatteries){backupBatteries.SetBatMode(ChargeMode.Recharge);}}else if(mainBatteries.groupPowerLevel<highPowerPercent&&
mainBatteries.groupPowerLevel>=lowPowerPercent){if(!currentPosture.overrideReactors){mainReactors.SetGroupEnabled(mainReactors.
groupEnabled??false);}if(!currentPosture.overrideBatteries){backupBatteries.SetBatMode(ChargeMode.Recharge);}}else if(mainBatteries.
groupPowerLevel<lowPowerPercent&&mainBatteries.groupPowerLevel>=2){if(!currentPosture.overrideReactors){mainReactors.SetGroupEnabled(
true);}if(!currentPosture.overrideBatteries){backupBatteries.SetBatMode(backupBatteries.batteryGroupMode??ChargeMode.
Recharge);}}else if(mainBatteries.groupPowerLevel<2){if(!currentPosture.overrideReactors){mainReactors.SetGroupEnabled(true);}if
(!currentPosture.overrideBatteries){backupBatteries.SetBatMode(ChargeMode.Auto);}}if(mainBatteries.groupFuncBats<1){if(!
currentPosture.overrideBatteries){backupBatteries.SetBatMode(ChargeMode.Auto);}}}static string mainDriveGroupName,rcsGroupName,
gyroGroupName;DriveGroup mainDrives;DriveGroup rcsThrusters;class DriveGroup:BlockGroup<IMyThrust>{public DriveGroup(Program program,
string name):base(program,name,new string[]{"Pr","Propulsion"}){}public float maxThrust=0,maxEffectiveThrust=0;public override
void ReadStep(){maxThrust=0;maxEffectiveThrust=0;foreach(IMyThrust drive in thingList){if(drive.IsWorking){maxThrust+=drive.
MaxThrust;maxEffectiveThrust+=drive.MaxEffectiveThrust;}}}}GyroGroup mainGyros;class GyroGroup:BlockGroup<IMyGyro>{public
GyroGroup(Program program,string name):base(program,name,new string[]{"Pr","Propulsion"}){}}static string mainFuelTankGroupName,
mainAirTankGroupName;GasTankGroup mainFuelTanks;GasTankGroup mainAirTanks;class GasTankGroup:BlockGroup<IMyGasTank>{public GasTankGroup(
Program program,string name):base(program,name,new string[]{"Ta","Tanks"}){}public bool?stockpileOn=null;public float capacity=
0,stored=0,lastStored=0,level=0,flowRate=0;public int totalCount=0,funcCount=0;public override void ReadStep(){lastStored
=stored;float secondsSince=(float)program.timeSinceLooped.TotalSeconds;capacity=0;stored=0;totalCount=0;funcCount=0;
foreach(IMyGasTank tank in thingList){totalCount++;if(tank.IsFunctional){funcCount++;capacity+=tank.Capacity;stored+=Convert.
ToSingle(tank.Capacity*tank.FilledRatio);}}level=stored/capacity*100;if(secondsSince>0){flowRate=(lastStored-stored)/
secondsSince;}}public void SetStockpile(bool stockpile){try{if(stockpile!=stockpileOn){foreach(IMyGasTank tank in thingList){tank.
Stockpile=stockpile;}stockpileOn=stockpile;}}catch(Exception exc){logItems[logID[0]+"E04:"+groupName]=new logItem{category=logID[
1],content="Error when setting stockpile mode of "+groupName+" -"+exc,priority=1,maxLife=3};}}}
