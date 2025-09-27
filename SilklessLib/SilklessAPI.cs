using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using SilklessLib.Connectors;

namespace SilklessLib;

public abstract class SilklessPacket
{
    public string ID;
}

public static class SilklessAPI
{
    public static bool Initialized;

    public static bool Connected => _connector?.Connected ?? false;
    public static bool Ready => Initialized && Connected;

    public static HashSet<string> PlayerIDs => _lastSeen.Keys.ToHashSet();

    private static Connector _connector;
        
    private static float _time;
    // ReSharper disable once InconsistentNaming
    private static readonly Dictionary<string, float> _lastSeen = new();
    // ReSharper disable once InconsistentNaming
    private static readonly Dictionary<string, Type> _keyToType = new();
    // ReSharper disable once InconsistentNaming
    private static readonly Dictionary<string, Action<SilklessPacket>> _handlers = new();

    public static bool Init(ManualLogSource logger = null)
    {
        try
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
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
            return false;
        }
    }

    public static bool Enable()
    {
        try
        {
            LogUtil.LogInfo($"Enabling {_connector.GetConnectorName()}...", true);

            if (!_connector.Connect()) return false;
            
            PlayerIDs.Clear();

            LogUtil.LogInfo($"Enabled {_connector.GetConnectorName()} successfully.", true);

            try
            {
                OnConnect?.Invoke();
            }
            catch (Exception e)
            {
                LogUtil.LogError($"Error during OnConnect: {e}");
                return false;
            }
                

            return true;
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
            return false;
        }
    }

    public static bool Disable()
    {
        try
        {
            LogUtil.LogInfo($"Disabling {_connector.GetConnectorName()}...", true);

            if (!_connector.Disconnect()) return false;

            foreach (string id in PlayerIDs)
            {
                OnPlayerLeave?.Invoke(id);
                LogUtil.LogInfo($"Player {id} left.", true);
            }
            PlayerIDs.Clear();
            
            LogUtil.LogInfo($"Disabled {_connector.GetConnectorName()} successfully.", true);

            try
            {
                OnDisconnect?.Invoke();
            }
            catch (Exception e)
            {
                LogUtil.LogError($"Error during OnDisconnect: {e}");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
            return false;
        }
    }

    public static bool Toggle()
    {
        if (Connected) return Disable();
        return Enable();
    }
        
    public static void Update(float dt)
    {
        try
        {
            _time += dt;

            _lastSeen["self"] = _time;
            foreach (string id in _lastSeen.ToDictionary(e => e.Key, e => e.Value).Keys)
                if (_lastSeen[id] < _time - SilklessConfig.ConnectionTimeout)
                {
                    OnPlayerLeave?.Invoke(id);
                    LogUtil.LogInfo($"Player {id} left.", true);
                    _lastSeen.Remove(id);
                }
            
            if (!Ready) return;

            try
            {
                _connector.Update(dt);
            }
            catch (Exception e)
            {
                LogUtil.LogError($"Error during connector update: {e}");
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }

    public static bool SendPacket<T>(T packet) where T : SilklessPacket
    {
        try
        {
            if (!Ready) return false;

            LogUtil.LogDebug($"Sending packet with key={packet.GetType().Name} and id={_connector.GetId()}...");

            packet.ID = _connector.GetId();

            byte[] bytes = Serialize(packet);

            if (!_connector.SendBytes(bytes)) return false;

            LogUtil.LogDebug($"Sent packet with key={packet.GetType().Name} successfully.");

            return true;
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
            return false;
        }
    }

    public static bool AddHandler<T>(Action<T> a) where T : SilklessPacket
    {
        try
        {
            LogUtil.LogDebug($"Adding handler with key={typeof(T).Name}...");

            string key = typeof(T).Name;
            
            if (!_keyToType.ContainsKey(key)) _keyToType.Add(key, typeof(T));

            // ReSharper disable once NotAccessedVariable
            // ReSharper disable once RedundantAssignment
            if (_handlers.TryGetValue(key, out Action<SilklessPacket> handler)) handler += packet => a((T)packet);
            else _handlers.Add(key, packet => a((T)packet));

            LogUtil.LogInfo($"Added handler with key={typeof(T).Name} successfully.");

            return true;
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
            return false;
        }
    }

    public static bool RemoveHandler<T>(Action<T> a) where T : SilklessPacket
    {
        try
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
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
            return false;
        }
    }

    private static void OnData(byte[] bytes)
    {
        try
        {
            SilklessPacket packet = Deserialize(bytes);
            if (packet == null)
            {
                LogUtil.LogError("Could not deserialize packet!");
                return;
            }

            string key = packet.GetType().Name;

            if (!_handlers.TryGetValue(key, out Action<SilklessPacket> handler) || handler == null)
            {
                LogUtil.LogDebug($"Could not find handler for packet key={key}");
                return;
            }
            
            LogUtil.LogDebug($"Received packet with key={key}.");

            try
            {
                handler.Invoke(packet);
            }
            catch (Exception e)
            {
                LogUtil.LogError($"Error in handler: {e}");
            }
            

            if (_lastSeen.TryAdd(packet.ID, _time))
            {
                OnPlayerJoin?.Invoke(packet.ID);
                LogUtil.LogInfo($"Player {packet.ID} joined.", true);
            }
            else _lastSeen[packet.ID] = _time;
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }

    private static byte[] Serialize<T>(T packet) where T : SilklessPacket
    {
        try
        {
            string key = typeof(T).Name;

            LogUtil.LogDebug($"Serializing packet with key={key}");

            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            Type type = typeof(T);
                
            SilklessSerialization.Serialize(bw, key);
            SilklessSerialization.Serialize(bw, packet.ID);
                
            foreach (FieldInfo field in type.GetFields())
                SilklessSerialization.Serialize(bw, field.GetValue(packet));

            return ms.ToArray();
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
            return null;
        }
    }

    private static SilklessPacket Deserialize(byte[] bytes)
    {
        try
        {
            using MemoryStream ms = new MemoryStream(bytes);
            using BinaryReader br = new BinaryReader(ms);
            
            int keyLen = br.ReadInt32();
            string key = Encoding.UTF8.GetString(br.ReadBytes(keyLen));

            if (!_keyToType.TryGetValue(key, out Type type) || type == null)
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
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
            return null;
        }
    }

    public static Action OnConnect;
    public static Action<string> OnPlayerJoin;
    public static Action<string> OnPlayerLeave;
    public static Action OnDisconnect;
}