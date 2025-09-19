using SilklessCoop.Global;

namespace SilklessCoop.Connectors
{
    internal class DebugConnector : Connector
    {
        public override string GetConnectorName() { return "Debug connector"; }

        public override string GetId() { return "xxxx-xxxx"; }

        public override bool Init()
        {
            LogUtil.LogInfo($"Initializing {GetConnectorName()}...");
            bool tmp = base.Init();
            LogUtil.LogInfo($"{GetConnectorName()} has been initialized successfully.", true);
            return tmp;
        }

        public override void Enable()
        {
            LogUtil.LogInfo($"Enabling {GetConnectorName()}...");
            Connected = true;
            base.Enable();
            LogUtil.LogInfo($"{GetConnectorName()} has been enabled successfully.", true);
        }

        public override void Disable()
        {
            LogUtil.LogInfo($"Disabling {GetConnectorName()}...");
            Connected = false;
            base.Disable();
            LogUtil.LogInfo($"{GetConnectorName()} has been disabled successfully.", true);

            _sync.Reset();
        }

        public override void SendData(byte[] data)
        {
            if (!Initialized || !Enabled) return;

            OnData(data);
        }
    }
}
