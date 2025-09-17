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
            Logger.LogInfo($"Initializing {GetConnectorName()}...");
            Logger.LogInfo($"{GetConnectorName()} has been initialized successfully.");

            return base.Init();
        }

        public override void Enable()
        {
            Logger.LogInfo($"Enabling {GetConnectorName()}...");
            try
            {
                Task.Run(() =>
                {
                    _socket = new TcpClient(Config.EchoServerIP, Config.EchoServerPort);

                    _stream = _socket.GetStream();
                    _stream.ReadTimeout = 500;

                    _rxThread = new Thread(RxThreadFunction);
                    _rxQueue = new Queue<byte[]>();
                    _rxRunning = true;
                    _rxThread.Start();

                    Connected = true;

                    base.Enable();
                    _interface.SendPacket(new PacketTypes.JoinPacket { id = GetId() });

                    Logger.LogInfo($"{GetConnectorName()} has been enabled successfully.");
                });
            } catch (Exception e)
            {
                Logger.LogError($"Error while enabling {GetConnectorName()}: {e}");

                Disable();
            }
        }

        public override void Disable()
        {
            if (!Enabled) return;

            Logger.LogInfo($"Disabling {GetConnectorName()}...");
            try
            {
                Task.Run(() =>
                {
                    base.Disable();

                    _rxRunning = false;
                    _rxThread.Join();
                    if (_stream != null) _stream.Close();
                    if (_socket != null) _socket.Close();

                    Connected = false;

                    Logger.LogInfo($"{GetConnectorName()} has been disabled successfully.");
                });
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while disabling {GetConnectorName()}: {e}");
            }
        }

        protected override void Tick()
        {
            if (!Initialized || !Enabled || !Connected) return;

            try
            {
                while (_rxQueue.Count > 0) OnData(_rxQueue.Dequeue());
            }
            catch (Exception e)
            {
                Logger.LogError($"!Error during tick: {e}");
            }
        }

        private void RxThreadFunction()
        {
            try
            {
                byte[] buffer = new byte[1024];

                while (_rxRunning)
                {
                    try
                    {
                        if (_stream == null) { Logger.LogError("no stream"); break; }
                        if (_socket == null) { Logger.LogError("no socket"); break; }
                        if (_socket.Client.Poll(0, SelectMode.SelectRead) && _socket.Available == 0) { Logger.LogError("no data"); break; }

                        if (!_stream.CanRead)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead <= 0) continue;

                        int size = BitConverter.ToInt32(buffer, 0);

                        byte[] bytes = new byte[size];
                        Array.Copy(buffer, 4, bytes, 0, size);
                        _rxQueue.Enqueue(bytes);
                    }
                    catch (IOException e)
                    {
                        if (e.InnerException is SocketException sockEx && sockEx.SocketErrorCode == SocketError.TimedOut)
                        {
                            continue;
                        }
                        else
                        {
                            Logger.LogError($"Error while reading incoming data {e}!");
                            
                            Disable();
                            break;
                        }
                    }
                }

                Logger.LogInfo("Receive thread ended.");

                if (_rxRunning) Disable();
            }
            catch (Exception e)
            {
                Logger.LogError($"Error in receive thread {e}!");
                Disable();
            }
        }

        public override void SendData(byte[] data)
        {
            if (!Initialized || !Enabled) { Logger.LogError($"{GetConnectorName()} not ready to send yet!");  return; }
            if (_socket == null || _stream == null) { Logger.LogError($"{GetConnectorName()} cannot send with missing socket!");  return; }

            int size = 4 + data.Length;
            byte[] bytes = new byte[size];
            bytes[0] = (byte)((size      ) & 0xff);
            bytes[1] = (byte)((size >>  8) & 0xff);
            bytes[2] = (byte)((size >> 16) & 0xff);
            bytes[3] = (byte)((size >> 24) & 0xff);
            Array.Copy(data, 0, bytes, 4, data.Length);

            _stream.Write(bytes, 0, bytes.Length);
        }
    }
}
