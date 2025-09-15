using BepInEx.Logging;
using SilklessCoop.Connectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SilklessCoop
{
    public class PacketTypes
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct JoinPacket : NetworkInterface.IPacket
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string id;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string version;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct LeavePacket : NetworkInterface.IPacket
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string id;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct HornetPositionPacket : NetworkInterface.IPacket
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string id;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string scene;
            public float posX;
            public float posY;
            public float scaleX;
            public float vX;
            public float vY;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct HornetAnimationPacket : NetworkInterface.IPacket
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string id;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string collectionGuid;
            public int spriteId;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct CompassPositionPacket : NetworkInterface.IPacket
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string id;
            public bool active;
            public float posX;
            public float posY;
        }
    }

    internal class NetworkInterface : MonoBehaviour
    {
        public interface IPacket { };

        public ManualLogSource Logger;
        public ModConfig Config;

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
                    Logger.LogError($"Could not key by type {typeof(T).Name}");
                    return;
                }

                int size = Marshal.SizeOf(typeof(T));
                byte[] bytes = new byte[1 + size];
                bytes[0] = key;

                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(packet, ptr, false);
                Marshal.Copy(ptr, bytes, 1, size);
                Marshal.FreeHGlobal(ptr);

                _connector.SendData(bytes);

                Logger.LogInfo($"Sent {size} bytes");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while serializing packet {e}!");
            }
        }

        public void ReceivePacket(byte[] bytes)
        {
            try
            {
                byte key = bytes[0];
                byte[] msg = bytes.Skip(1).ToArray();

                if (!_keyToType.TryGetValue(key, out Type type) || type == null) return;
                if (!_handlers.TryGetValue(type, out Action<IPacket> handler) || handler == null) return;

                int size = msg.Length;
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(msg, 0, ptr, size);
                IPacket packet = (IPacket)Marshal.PtrToStructure(ptr, type);

                handler(packet);
            } catch (Exception e)
            {
                Logger.LogError($"Error while deserializing packet {e}!");
            }
        }

        public void AddHandler<T>(Action<T> handler) where T : IPacket, new()
        {
            byte key = ++_lastKey;
            _keyToType.Add(key, typeof(T));
            _typeToKey.Add(typeof(T), key);
            _handlers.Add(typeof(T), (packet) => handler((T) packet));

            Logger.LogInfo($"Mapped key={key} to {typeof(T).Name}");
        }
    }
}
