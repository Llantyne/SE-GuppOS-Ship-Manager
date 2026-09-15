namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        //Connector config variables
        static string connectorGroupName;

        //Connector group object
        ConnectorGroup mainConnectors;
        class ConnectorGroup : BlockGroup<IMyShipConnector>
        {
            //Constructor
            public ConnectorGroup(Program program, string name) : base(program, name, new string[]{"Co", "Connectors"}) {}

            //Methods
             //Sets connectors on/off
            public void SetConTag(string conTag, bool enabled)
            {
                try
                {
                    //Goes through each light
                    foreach (IMyShipConnector connector in thingList)
                    {
                        if (connector.CustomName.Contains(conTag))
                        {
                            //Sets on/off
                            connector.Enabled = enabled;
                        }
                    }
                }
                catch (Exception exc)
                {
                    logItems[logID[0]+"E04:"+groupName] = new logItem{category=logID[1], content="Error when setting connector tag status of "+groupName+" -"+exc, priority=1, maxLife=3};
                }
            }
             //Disable SetGroupEnabled method
            public new void SetGroupEnabled(bool enable)
            {
                throw new NotSupportedException("SetGroupEnabled is disabled in ConnectorGroup.");
            }
        }
    }
}