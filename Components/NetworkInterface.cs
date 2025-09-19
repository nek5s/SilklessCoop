using SilklessCoop.Connectors;
using SilklessCoop.Global;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SilklessCoop.Components
{
    public class PacketTypes
    {
        public class HornetPositionPacket : NetworkInterface.IPacket
        {
            public string id;
            public string scene;
            public float posX;
            public float posY;
            public float scaleX;
            public float vX;
            public float vY;
        }

        public class HornetAnimationPacket : NetworkInterface.IPacket
        {
            public string id;
            public string collectionGuid;
            public int spriteId;
        }

        public class CompassPositionPacket : NetworkInterface.IPacket
        {
            public string id;
            public bool active;
            public float posX;
            public float posY;
        }
    }

    internal class NetworkInterface : MonoBehaviour
    {
        public interface IPacket { };

        private Connector _connector;

        private byte _lastKey = 0;
        private Dictionary<byte, Type> _keyToType = new Dictionary<byte, Type>();
        private Dictionary<Type, byte> _typeToKey = new Dictionary<Type, byte>();

        private Dictionary<Type, Action<IPacket>> _handlers = new Dictionary<Type, Action<IPacket>>();

        private void Start()
        {
            _connector = gameObject.GetComponent<Connector>();
            _connector.OnData = ReceivePacket;
        }

        public void SendPacket<T>(T packet) where T : IPacket
        {
            try
            {
                if (!_typeToKey.TryGetValue(typeof(T), out byte key) || key == 0)
                {
                    LogUtil.LogError($"Could not key by type {typeof(T).Name}");
                    return;
                }

                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    byte[] content = Serialize(packet);
                    if (content == null) return;

                    bw.Write((int) 5 + content.Length);
                    bw.Write((byte) key);
                    bw.Write(content);

                    _connector.SendData(ms.ToArray());

                    if (ModConfig.PrintDebugOutput) LogUtil.LogInfo($"Sent packet with key={key}.");
                }
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        public void ReceivePacket(byte[] bytes)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    int size = br.ReadInt32();
                    byte key = br.ReadByte();
                    byte[] content = br.ReadBytes(size - 5);

                    if (!_keyToType.TryGetValue(key, out Type type) || type == null) return;
                    if (!_handlers.TryGetValue(type, out Action<IPacket> handler) || handler == null) return;

                    if (ModConfig.PrintDebugOutput) LogUtil.LogInfo($"Received packet with key={key}.");

                    MethodInfo genMethod = typeof(NetworkInterface).GetMethod("Deserialize").MakeGenericMethod(type);

                    IPacket packet = (IPacket) genMethod.Invoke(this, new object[] { content });
                    if (packet == null) return;

                    handler(packet);
                }
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        public void AddHandler<T>(Action<T> handler) where T : IPacket, new()
        {
            byte key = ++_lastKey;
            _keyToType.Add(key, typeof(T));
            _typeToKey.Add(typeof(T), key);
            _handlers.Add(typeof(T), (packet) => handler((T)packet));

            LogUtil.LogInfo($"Mapped key={key} to {typeof(T).Name}");
        }

        public IPacket Deserialize<T>(byte[] bytes) where T : IPacket, new()
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    T packet = new T();

                    Type type = typeof(T);

                    foreach (FieldInfo field in type.GetFields())
                    {
                        if (field.FieldType == typeof(int))
                        {
                            int i = br.ReadInt32();
                            field.SetValue(packet, i);
                        }
                        if (field.FieldType == typeof(float))
                        {
                            float f = br.ReadSingle();
                            field.SetValue(packet, f);
                        }
                        if (field.FieldType == typeof(bool))
                        {
                            bool b = br.ReadByte() != 0;
                            field.SetValue(packet, b);
                        }
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
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return null;
            }
        }

        public byte[] Serialize<T>(T packet) where T : IPacket
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    Type type = typeof(T);

                    foreach (FieldInfo field in type.GetFields())
                    {
                        if (field.FieldType == typeof(int)) bw.Write(BitConverter.GetBytes((int)field.GetValue(packet)));
                        if (field.FieldType == typeof(float)) bw.Write(BitConverter.GetBytes((float)field.GetValue(packet)));
                        if (field.FieldType == typeof(bool)) bw.Write((byte) 1);
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
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return null;
            }
        }
    }
}
