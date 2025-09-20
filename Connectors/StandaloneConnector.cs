using SilklessCoop.Global;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SilklessCoop.Connectors
{
    internal class StandaloneConnector : Connector
    {
        private TcpClient _socket;
        private NetworkStream _stream;
        private bool _rxRunning;
        private Thread _rxThread;
        private Queue<byte[]> _rxQueue;

        private string _id = null;

        public override string GetConnectorName() { return "Standalone connector"; }

        public override string GetId()
        {
            if (_id != null) return _id;

            char[] hexChars = "0123456789abcdef".ToCharArray();
            char[] chars = new char[9];

            Random random = new Random();
            for (int i = 0; i < 9; i ++)
                chars[i] = hexChars[random.Next(hexChars.Length)];
            chars[4] = '-';

            return _id = new string(chars);
        }

        public override bool Init()
        {
            LogUtil.LogInfo($"Initializing {GetConnectorName()}...");
            LogUtil.LogInfo($"{GetConnectorName()} has been initialized successfully.", true);

            return base.Init();
        }

        public override void Enable()
        {
            try
            {
                LogUtil.LogInfo($"Enabling {GetConnectorName()}...");

                Task.Run(() =>
                {
                    _socket = new TcpClient(ModConfig.EchoServerIP, ModConfig.EchoServerPort);

                    _stream = _socket.GetStream();
                    _stream.ReadTimeout = 500;

                    _rxThread = new Thread(RxThreadFunction);
                    _rxQueue = new Queue<byte[]>();
                    _rxRunning = true;
                    _rxThread.Start();

                    Connected = true;

                    base.Enable();

                    LogUtil.LogInfo($"{GetConnectorName()} has been enabled successfully.", true);
                });
            } catch (Exception e)
            {
                LogUtil.LogError(e.ToString());

                Disable();
            }
        }

        public override void Disable()
        {
            try
            {
                if (!Enabled) return;

                LogUtil.LogInfo($"Disabling {GetConnectorName()}...");

                Task.Run(() =>
                {
                    Connected = false;
                    base.Disable();

                    _rxRunning = false;
                    _rxThread.Join();
                    if (_stream != null) _stream.Close();
                    if (_socket != null) _socket.Close();

                    Connected = false;

                    LogUtil.LogInfo($"{GetConnectorName()} has been disabled successfully.", true);

                    _sync.Reset();
                });
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        private void Update()
        {
            try
            {
                if (!Initialized || !Enabled || !Connected) return;

                while (_rxQueue.Count > 0) OnData(_rxQueue.Dequeue());
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        private void RxThreadFunction()
        {
            try
            {
                while (_rxRunning)
                {
                    // read size
                    byte[] sizeBytes = ReadBytes(4);
                    if (sizeBytes == null) break;
                    int size = BitConverter.ToInt32(sizeBytes);

                    LogUtil.LogDebug($"Read size={size}");
                    if (size == 0) continue;

                    // read content
                    byte[] contentBytes = ReadBytes(size - 4);
                    if (contentBytes == null) break;

                    // success
                    _rxQueue.Enqueue(contentBytes);
                }

                LogUtil.LogInfo("Receive thread ended.");

                if (_rxRunning) Disable();
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                Disable();
            }
        }

        private byte[] ReadBytes(int length)
        {
            byte[] buffer = new byte[length];
            int read = 0;

            while (read < length)
            {
                if (!_rxRunning) return null;
                if (_stream == null) { LogUtil.LogError("Stream not found!"); return null; }
                if (_socket == null) { LogUtil.LogError("Socket not found!"); return null; }
                if (_socket.Client.Poll(0, SelectMode.SelectRead) && _socket.Available == 0) { LogUtil.LogError("Data not found!"); return null; }

                if (!_stream.DataAvailable)
                {
                    Thread.Sleep(100);
                    continue;
                }

                try
                {
                    read += _stream.Read(buffer, read, length - read);
                }
                catch (IOException e)
                {
                    if (e.InnerException is SocketException sockEx && sockEx.SocketErrorCode == SocketError.TimedOut)
                    {
                        continue;
                    }
                    else
                    {
                        LogUtil.LogError(e.ToString()); return null;
                    }
                }
            }

            return buffer;
        }

        public override void SendData(byte[] data)
        {
            try
            {
                if (!Initialized || !Enabled) { LogUtil.LogError($"Cannot send while disabled!");  return; }
                if (_socket == null || _stream == null) { LogUtil.LogError($"Cannot send with missing socket!");  return; }

                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(4 + data.Length);
                    bw.Write(data);

                    _stream.Write(ms.ToArray());
                }
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }
    }
}
