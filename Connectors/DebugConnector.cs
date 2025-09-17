namespace SilklessCoop.Connectors
{
    internal class DebugConnector : Connector
    {
        public override string GetConnectorName() { return "Debug connector"; }

        public override string GetId() { return "xxxx-xxxx"; }

        public override bool Init()
        {
            Logger.LogInfo($"Initializing {GetConnectorName()}...");
            bool tmp = base.Init();
            Logger.LogInfo($"{GetConnectorName()} has been initialized successfully.");
            return tmp;
        }

        public override void Enable()
        {
            Logger.LogInfo($"Enabling {GetConnectorName()}...");
            Connected = true;
            base.Enable();
            _interface.SendPacket(new PacketTypes.JoinPacket { id = GetId() });
            Logger.LogInfo($"{GetConnectorName()} has been enabled successfully.");
        }

        public override void Disable()
        {
            Logger.LogInfo($"Disabling {GetConnectorName()}...");
            _interface.SendPacket(new PacketTypes.LeavePacket { id = GetId() });
            Connected = false;
            base.Disable();
            Logger.LogInfo($"{GetConnectorName()} has been disabled successfully.");
        }

        public override void SendData(byte[] data)
        {
            if (!Initialized || !Enabled) return;

            OnData(data);
        }
    }
}
