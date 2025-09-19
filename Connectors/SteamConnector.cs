using SilklessCoop.Global;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilklessCoop.Connectors
{
    internal class SteamConnector : Connector
    {
        private bool _isHost = true;
        private Dictionary<CSteamID, float> _lastSeen = new Dictionary<CSteamID, float>();

        private Callback<GameRichPresenceJoinRequested_t> _gameRichPresenceJoinRequested;
        private Callback<P2PSessionRequest_t> _p2pSessionRequest;

        public override string GetConnectorName() => "Steam connector";

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
            return base.Init();
        }

        public override void Enable()
        {
            try
            {
                LogUtil.LogInfo($"Enabling {GetConnectorName()}...");

                _gameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);
                _p2pSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);

                _lastSeen = new Dictionary<CSteamID, float>();
                _isHost = true;

                SteamFriends.SetRichPresence("connect", GetId());

                base.Enable();

                LogUtil.LogInfo($"{GetConnectorName()} enabled successfully.", true);
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        public override void Disable()
        {
            try
            {
                LogUtil.LogInfo($"Disabling {GetConnectorName()}...");

                base.Disable();
                _gameRichPresenceJoinRequested.Unregister();
                _p2pSessionRequest.Unregister();

                foreach (CSteamID id in _lastSeen.Keys)
                    SteamNetworking.CloseP2PSessionWithUser(id);
                _lastSeen.Clear();

                SteamFriends.ClearRichPresence();

                LogUtil.LogInfo($"{GetConnectorName()} disabled successfully.", true);

                _sync.Reset();
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        protected override void Update()
        {
            try
            {
                if (Initialized && Enabled) SteamAPI.RunCallbacks();

                base.Update();
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        protected override void Tick()
        {
            try
            {
                // handle timeout
                foreach (CSteamID id in _lastSeen.ToDictionary(e => e.Key, e => e.Value).Keys)
                {
                    if (_lastSeen[id] != 0 && _lastSeen[id] < Time.unscaledTime - ModConfig.ConnectionTimeout)
                    {
                        LogUtil.LogInfo($"Player {id} timed out.", true);

                        SteamNetworking.CloseP2PSessionWithUser(id);
                        _sync.RemovePlayer(id.ToString());
                        _lastSeen.Remove(id);
                    }
                }

                // handle loneliness
                if (_lastSeen.Count == 0 && !_isHost)
                {
                    Connected = false;
                    _isHost = true;

                    SteamFriends.SetRichPresence("connect", GetId());
                }

                // read
                while (SteamNetworking.IsP2PPacketAvailable(out uint msgSize))
                {
                    byte[] bytes = new byte[msgSize];

                    if (SteamNetworking.ReadP2PPacket(bytes, msgSize, out _, out CSteamID sender))
                    {
                        _lastSeen[sender] = Time.unscaledTime;

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

        public override void SendData(byte[] data)
        {
            if (!Initialized || !Enabled) return;

            foreach (CSteamID id in _lastSeen.Keys)
                SteamNetworking.SendP2PPacket(id, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
        }

        private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t request)
        {
            // called on client
            LogUtil.LogInfo($"Connecting to {request.m_steamIDFriend}...");

            Connected = true;
            _isHost = false;
            _lastSeen[request.m_steamIDFriend] = 0;
            SteamFriends.SetRichPresence("connect", request.m_steamIDFriend.ToString());

            LogUtil.LogInfo($"Successfully connected to {request.m_steamIDFriend}.", true);
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t request)
        {
            // called on server
            LogUtil.LogInfo($"Incoming connection from {request.m_steamIDRemote}...");

            SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDRemote);

            Connected = true;
            _lastSeen[request.m_steamIDRemote] = 0;

            LogUtil.LogInfo($"Successfully received connection from {request.m_steamIDRemote}.", true);
        }
    }
}
