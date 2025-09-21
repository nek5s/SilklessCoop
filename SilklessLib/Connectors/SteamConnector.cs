using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;

namespace SilklessLib.Connectors
{
    internal class SteamConnector : Connector
    {
        private bool _isHost = true;
        private Dictionary<CSteamID, float> _lastSeen = new();

        private Callback<GameRichPresenceJoinRequested_t> _gameRichPresenceJoinRequested;
        private Callback<P2PSessionRequest_t> _p2PSessionRequest;

        private float _time;

        public override string GetConnectorName() => "Steam Connector";

        public override string GetId() => SteamUser.GetSteamID().ToString();

        public override bool Init()
        {
            LogUtil.LogInfo($"Initializing {GetConnectorName()} ...");

            if (!SteamAPI.Init())
            {
                LogUtil.LogError("Steam API failed to initialize!");
                return false;
            }

            LogUtil.LogInfo($"{GetConnectorName()} initialized successfully.", true);
            return true;
        }

        public override bool Connect()
        {
            try
            {
                LogUtil.LogInfo($"Enabling {GetConnectorName()}...", true);

                _gameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);
                _p2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);

                _lastSeen = new Dictionary<CSteamID, float>();
                _isHost = true;

                SteamFriends.SetRichPresence("connect", GetId());

                Connected = true;

                LogUtil.LogInfo($"{GetConnectorName()} enabled successfully.", true);
                return true;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return false;
            }
        }

        public override bool Disconnect()
        {
            try
            {
                LogUtil.LogInfo($"Disabling {GetConnectorName()}...");

                Connected = false;

                _gameRichPresenceJoinRequested.Unregister();
                _p2PSessionRequest.Unregister();

                foreach (CSteamID id in _lastSeen.Keys)
                    SteamNetworking.CloseP2PSessionWithUser(id);
                _lastSeen.Clear();

                SteamFriends.ClearRichPresence();

                LogUtil.LogInfo($"{GetConnectorName()} disabled successfully.", true);
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
            _time += dt;

            try
            {
                if (!Connected) return;

                SteamAPI.RunCallbacks();

                // handle timeout
                foreach (CSteamID id in _lastSeen.ToDictionary(e => e.Key, e => e.Value).Keys)
                {
                    if (_lastSeen[id] != 0 && _lastSeen[id] < _time - SilklessConfig.ConnectionTimeout)
                    {
                        LogUtil.LogInfo($"Player {id} timed out.", true);

                        SteamNetworking.CloseP2PSessionWithUser(id);
                        _lastSeen.Remove(id);
                    }
                }

                // handle loneliness
                if (_lastSeen.Count == 0 && !_isHost)
                {
                    Active = false;
                    _isHost = true;

                    SteamFriends.SetRichPresence("connect", GetId());
                }

                // read
                while (SteamNetworking.IsP2PPacketAvailable(out uint msgSize))
                {
                    byte[] bytes = new byte[msgSize];

                    if (SteamNetworking.ReadP2PPacket(bytes, msgSize, out _, out CSteamID sender))
                    {
                        _lastSeen[sender] = _time;

                        OnData(bytes);

                        if (_isHost)
                        {
                            foreach (CSteamID id in _lastSeen.Keys)
                                if (id != sender) SteamNetworking.SendP2PPacket(id, bytes, msgSize, EP2PSend.k_EP2PSendReliable);
                        }
                    }
                }
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
                if (!Connected) return false;

                foreach (CSteamID id in _lastSeen.Keys)
                    SteamNetworking.SendP2PPacket(id, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);

                return true;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
                return false;
            }
        }

        private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t request)
        {
            try
            {
                // called on client
                LogUtil.LogInfo($"Connecting to {request.m_steamIDFriend}...");

                Active = true;
                _isHost = false;
                _lastSeen[request.m_steamIDFriend] = 0;
                SteamFriends.SetRichPresence("connect", request.m_steamIDFriend.ToString());

                LogUtil.LogInfo($"Successfully connected to {request.m_steamIDFriend}.", true);
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t request)
        {
            try
            {
                // called on server
                LogUtil.LogInfo($"Incoming connection from {request.m_steamIDRemote}...");

                SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDRemote);

                Active = true;
                _lastSeen[request.m_steamIDRemote] = 0;

                LogUtil.LogInfo($"Successfully received connection from {request.m_steamIDRemote}.", true);
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }
    }
}
