using System;

namespace SilklessLib.Connectors
{
    internal class DebugConnector : Connector
    {
        public override bool Connect()
        {
            try
            {
                LogUtil.LogInfo($"Enabling {GetConnectorName()}...", true);
            
                Connected = true;
                Active = true;

                LogUtil.LogInfo($"{GetConnectorName()} has been enabled successfully.", true);
                return true;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return false;
            }
        }

        public override bool Disconnect()
        {
            try
            {
                Connected = false;
                Active = false;
                return true;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return false;
            }
        }

        public override string GetConnectorName() { return "Debug Connector"; }

        public override string GetId() => "xxxx-xxxx";

        public override string GetUsername() => "debug-user";

        public override bool Init() { return true; }

        public override bool SendBytes(byte[] data)
        {
            try
            {
                OnData(data);
                return true;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return false;
            }
        }
    }
}
