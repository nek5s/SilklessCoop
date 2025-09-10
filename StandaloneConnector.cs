using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SilklessCoop
{
    internal class StandaloneConnector : Connector
    {
        public string IPAddress;
        public int Port;

        private TcpClient _socket;
        private NetworkStream _stream;
        private bool _rxRunning;
        private Thread _rxThread;
        private Queue<string> _rxQueue;

        public override bool Init()
        {
            Logger.LogInfo("Initializing standalone connector...");
            Logger.LogInfo("Standalone connector has been initialized successfully.");

            return base.Init();
        }

        public override void Enable()
        {
            Logger.LogInfo("Enabling standalone connector...");
            try
            {
                Task.Run(() =>
                {
                    _socket = new TcpClient(IPAddress, Port);
                    _stream = _socket.GetStream();
                    _stream.ReadTimeout = 500;

                    _rxThread = new Thread(RxThreadFunction);
                    _rxQueue = new Queue<string>();
                    _rxRunning = true;
                    _rxThread.Start();

                    base.Enable();

                    Logger.LogInfo("Standalone connector has been enabled successfully.");
                });
            } catch (Exception e)
            {
                Logger.LogError($"Error while enabling standalone connector: {e}");

                Disable();
            }
        }

        public override void Disable()
        {
            if (!Active) return;

            Logger.LogInfo("Disabling standalone connector...");
            try
            {
                Task.Run(() =>
                {
                    _rxRunning = false;
                    _rxThread.Join();
                    if (_stream != null) _stream.Close();
                    if (_socket != null) _socket.Close();

                    base.Disable();

                    Logger.LogInfo("Standalone connector has been disabled successfully.");
                });
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while disabling standalone connector: {e}");
            }
        }

        protected override void Tick()
        {
            try
            {
                // send
                string data = _sync.GetUpdateContent() + "\n";
                if (data != null)
                {
                    byte[] msg = Encoding.UTF8.GetBytes(data);
                    _stream.Write(msg, 0, msg.Length);
                }

                // read
                while (_rxQueue.Count > 0) _sync.ApplyUpdate(_rxQueue.Dequeue());
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
                StringBuilder incomingData = new StringBuilder();

                while (_rxRunning)
                {
                    try
                    {
                        if (_stream == null) { break; }
                        if (_socket == null) { break; }
                        if (_socket.Client.Poll(0, SelectMode.SelectRead) && _socket.Available == 0) { Logger.LogInfo("no data"); break; }

                        if (!_stream.CanRead)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead <= 0) continue;

                        string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        incomingData.Append(chunk);

                        while (true)
                        {
                            int newlineIndex = incomingData.ToString().IndexOf('\n');
                            if (newlineIndex < 0) break;
                            string line = incomingData.ToString(0, newlineIndex).Trim();
                            incomingData.Remove(0, newlineIndex + 1);

                            _rxQueue.Enqueue(line);
                        }
                    }
                    catch (IOException e)
                    {
                        if (e.InnerException is SocketException sockEx && sockEx.SocketErrorCode == SocketError.TimedOut)
                        {
                            continue;
                        }
                        else
                        {
                            Logger.LogError("Error while reading incoming data!");
                            Logger.LogError(e.ToString());

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
                Logger.LogError("Error in receive thread!");
                Logger.LogError(e.ToString());
                Disable();
            }
        }
    }
}
