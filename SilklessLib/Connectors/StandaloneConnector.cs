using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace SilklessLib.Connectors
{
    internal class StandaloneConnector : Connector
    {
        private string _id;

        private TcpClient _socket;
        private NetworkStream _stream;
        private bool _rxRunning;
        private Thread _rxThread;
        private Queue<byte[]> _rxQueue;

        public override string GetConnectorName() => "Standalone Connector";

        public override string GetId()
        {
            if (_id != null) return _id;

            char[] hexChars = "0123456789abcdef".ToCharArray();
            char[] chars = new char[9];

            Random random = new Random();
            for (int i = 0; i < 9; i++)
                chars[i] = hexChars[random.Next(hexChars.Length)];
            chars[4] = '-';

            return _id = new string(chars);
        }

        public override string GetUsername() { return SilklessConfig.StandaloneUsername; }

        public override bool Init()
        {
            try
            {
                LogUtil.LogInfo($"Initializing {GetConnectorName()}...", true);
                LogUtil.LogInfo($"{GetConnectorName()} has been initialized successfully.", true);

                return true;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return false;
            }
        }

        public override bool Connect()
        {
            try
            {
                LogUtil.LogInfo($"Enabling {GetConnectorName()}...", true);

                _socket = new TcpClient(SilklessConfig.StandaloneIP, SilklessConfig.StandalonePort);

                _stream = _socket.GetStream();
                _stream.ReadTimeout = 500;

                _rxThread = new Thread(RxThreadFunction);
                _rxQueue = new Queue<byte[]>();
                _rxRunning = true;
                _rxThread.Start();

                Connected = true;
                Active = true;

                LogUtil.LogInfo($"{GetConnectorName()} has been enabled successfully.", true);

                return true;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());

                Disconnect();

                return false;
            }
        }

        public override bool Disconnect()
        {
            try
            {
                if (!Connected) return false;

                _rxRunning = false;
                _rxThread.Join();
                if (_stream != null) _stream.Close();
                if (_socket != null) _socket.Close();

                Active = false;
                Connected = false;

                LogUtil.LogInfo($"{GetConnectorName()} has been disabled successfully.", true);

                return true;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return false;
            }
        }

        public override void Update(float dt)
        {
            try
            {
                if (!Connected) return;

                while (_rxQueue.Count > 0) OnData(_rxQueue.Dequeue());
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }


        public override bool SendBytes(byte[] data)
        {
            try
            {
                if (!Connected) { LogUtil.LogError("Cannot send while disabled!"); return false; }
                if (_socket == null || _stream == null) { LogUtil.LogError("Cannot send with missing socket!"); return false; }

                using MemoryStream ms = new MemoryStream();
                using BinaryWriter bw = new BinaryWriter(ms);
                
                bw.Write(4 + data.Length);
                bw.Write(data);

                _stream.Write(ms.ToArray());

                return true;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return false;
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
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                Disconnect();
            }
        }

        private byte[] ReadBytes(int length)
        {
            try
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
                        // ReSharper disable once MergeIntoNegatedPattern
                        if (e.InnerException is not SocketException sockEx || sockEx.SocketErrorCode != SocketError.TimedOut)
                        {
                            LogUtil.LogError(e.ToString());
                            return null;
                        }
                    }
                }

                return buffer;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return null;
            }
        }
    }
}
