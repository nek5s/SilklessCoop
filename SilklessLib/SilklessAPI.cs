using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using SilklessLib.Connectors;

namespace SilklessLib
{
    public abstract class SilklessPacket
    {
        public string ID;
    }

    public static class SilklessAPI
    {
        public static bool Initialized;

        public static bool Connected => _connector?.Connected ?? false;
        public static bool Ready => Initialized && Connected;

        private static Connector _connector;

        // ReSharper disable once InconsistentNaming
        private static readonly Dictionary<string, Type> _keyToType = new();
        // ReSharper disable once InconsistentNaming
        private static readonly Dictionary<string, Action<SilklessPacket>> _handlers = new();

        public static bool Init(ManualLogSource logger = null)
        {
            if (logger != null) LogUtil.ConsoleLogger = logger;

            if (Initialized) return true;

            LogUtil.LogInfo("Initializing...", true);

            if (SilklessConfig.ConnectionType == SilklessConfig.EConnectionType.Debug) _connector = new DebugConnector();
            if (SilklessConfig.ConnectionType == SilklessConfig.EConnectionType.Standalone) _connector = new StandaloneConnector();
            if (SilklessConfig.ConnectionType == SilklessConfig.EConnectionType.SteamP2P) _connector = new SteamConnector();

            if (_connector == null)
            {
                LogUtil.LogError("Connector type not found!");
                return false;
            }

            if (!_connector.Init())
            {
                LogUtil.LogError($"Failed to initialize {_connector.GetConnectorName()}!");
                return false;
            }

            _connector.OnData += OnData;

            Initialized = true;

            LogUtil.LogInfo("Initialized successfully.", true);

            return true;
        }

        public static bool Enable()
        {
            LogUtil.LogInfo($"Enabling {_connector.GetConnectorName()}...", true);

            if (!_connector.Connect()) return false;

            LogUtil.LogInfo($"Enabled {_connector.GetConnectorName()} successfully.", true);

            OnConnect?.Invoke();

            return true;
        }

        public static bool Disable()
        {
            LogUtil.LogInfo($"Disabling {_connector.GetConnectorName()}...", true);

            if (!_connector.Disconnect()) return false;

            LogUtil.LogInfo($"Disabled {_connector.GetConnectorName()} successfully.", true);

            OnDisconnect?.Invoke();

            return true;
        }

        public static bool Toggle()
        {
            if (Connected) return Disable();
            return Enable();
        }
        
        public static void Update(float dt)
        {
            if (!Ready) return;

            _connector.Update(dt);
        }

        public static bool SendPacket<T>(T packet) where T : SilklessPacket
        {
            if (!Ready) return false;

            LogUtil.LogDebug($"Sending packet with key={packet.GetType().Name} and id={_connector.GetId()}...");

            packet.ID = _connector.GetId();

            byte[] bytes = Serialize(packet);

            if (!_connector.SendBytes(bytes)) return false;

            LogUtil.LogDebug($"Sent packet with key={packet.GetType().Name} successfully.");

            return true;
        }

        public static bool AddHandler<T>(Action<T> a) where T : SilklessPacket
        {
            LogUtil.LogDebug($"Adding handler with key={typeof(T).Name}...");

            string key = typeof(T).Name;
            
            _keyToType.Add(key, typeof(T));

            // ReSharper disable once NotAccessedVariable
            // ReSharper disable once RedundantAssignment
            if (_handlers.TryGetValue(key, out Action<SilklessPacket> handler)) handler += packet => a((T)packet);
            else _handlers.Add(key, packet => a((T)packet));

            LogUtil.LogDebug($"Added handler with key={typeof(T).Name} successfully.");

            return true;
        }

        public static bool RemoveHandler<T>(Action<T> a) where T : SilklessPacket
        {
            LogUtil.LogDebug($"Removing handler with key={typeof(T).Name}...");

            string key = typeof(T).Name;

            _keyToType.Remove(key);

            // ReSharper disable once NotAccessedVariable
            // ReSharper disable once RedundantAssignment
            if (_handlers.TryGetValue(key, out Action<SilklessPacket> handler)) handler -= packet => a((T)packet);

            LogUtil.LogDebug($"Removed handler with key={typeof(T).Name} successfully.");

            return true;
        }

        private static void OnData(byte[] bytes)
        {
            SilklessPacket packet = Deserialize(bytes);
            if (packet == null)
            {
                LogUtil.LogError("Could not deserialize packet!");
                return;
            }

            string key = packet.GetType().Name;

            if (!_handlers.TryGetValue(key, out Action<SilklessPacket> handler))
            {
                LogUtil.LogError($"Could not find handler for packet key={key}");
                return;
            }

            LogUtil.LogDebug($"Received packet with key={key}.");

            handler(packet);
        }

        private static byte[] Serialize<T>(T packet) where T : SilklessPacket
        {
            string key = typeof(T).Name;

            LogUtil.LogInfo($"Serializing packet with key={key}");

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(key.Length);
                bw.Write(Encoding.UTF8.GetBytes(key));

                Type type = typeof(T);

                FieldInfo[] baseFields = typeof(SilklessPacket).GetFields();
                FieldInfo[] specificFields = type.GetFields();
                FieldInfo[] fields = baseFields.Concat(specificFields).ToArray();

                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType == typeof(int)) bw.Write(BitConverter.GetBytes((int)field.GetValue(packet)));
                    if (field.FieldType == typeof(float)) bw.Write(BitConverter.GetBytes((float)field.GetValue(packet)));
                    if (field.FieldType == typeof(bool)) bw.Write((byte)((bool)field.GetValue(packet) ? 1 : 0));
                    if (field.FieldType == typeof(string))
                    {
                        string s = (string)field.GetValue(packet);
                        bw.Write(BitConverter.GetBytes(s.Length));
                        bw.Write(Encoding.UTF8.GetBytes(s));
                    }
                }

                return ms.ToArray();
            }
        }

        private static SilklessPacket Deserialize(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader br = new BinaryReader(ms))
            {
                int keyLen = br.ReadInt32();
                string key = Encoding.UTF8.GetString(br.ReadBytes(keyLen));

                if (!_keyToType.TryGetValue(key, out Type type))
                {
                    LogUtil.LogDebug($"Unrecognized packet key={key}.");
                    return null;
                }

                SilklessPacket packet = (SilklessPacket)Activator.CreateInstance(type);

                int idLen = br.ReadInt32();
                string id = Encoding.UTF8.GetString(br.ReadBytes(idLen));
                packet.ID = id;

                FieldInfo[] specificFields = type.GetFields();
                foreach (FieldInfo field in specificFields)
                {
                    if (field.FieldType == typeof(int)) field.SetValue(packet, br.ReadInt32());
                    if (field.FieldType == typeof(float)) field.SetValue(packet, br.ReadSingle());
                    if (field.FieldType == typeof(bool)) field.SetValue(packet, br.ReadByte() != 0);
                    if (field.FieldType == typeof(string))
                    {
                        int len = br.ReadInt32();
                        string s = Encoding.UTF8.GetString(br.ReadBytes(len));
                        field.SetValue(packet, s);
                    }
                }

                return packet;
            }
        }

        public static Action OnConnect;
        public static Action OnDisconnect;
    }
}
