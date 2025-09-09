using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilklessCoop
{
    internal class SteamConnector : Connector
    {
        private enum ELobbyRole { DEFAULT, SERVER, CLIENT }

        // config
        public int TickRate;

        // callbacks
        private Callback<GameRichPresenceJoinRequested_t> _gameRichPresenceJoinRequested;
        private Callback<P2PSessionRequest_t> _p2pSessionRequest;
        private Callback<P2PSessionConnectFail_t> _p2pSessionConnectFail;

        // state
        private ELobbyRole _role;
        private CSteamID _ownId;
        private CSteamID _hostId;
        private HashSet<CSteamID> _connected;

        public override bool Init()
        {
            Logger.LogInfo("Initializing steam connector...");

            if (!SteamAPI.Init())
            {
                Logger.LogError("SteamAPI failed to intialize!");
                return false;
            }

            Logger.LogInfo("Steam connector has been initialized successfully.");

            return base.Init();
        }

        public override void Enable()
        {
            Logger.LogInfo("Enabling steam connector...");
            try
            {
                _gameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);
                _p2pSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
                _p2pSessionConnectFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionConnectFail);

                _role = ELobbyRole.DEFAULT;
                _ownId = SteamUser.GetSteamID();
                _hostId = CSteamID.Nil;
                _connected = new HashSet<CSteamID>();

                SteamFriends.SetRichPresence("connect", _ownId.ToString());

                InvokeRepeating("Tick", 0, 1.0f / TickRate);

                base.Enable();

                Logger.LogInfo("Steam connector has been enabled successfully.");
            } catch (Exception e)
            {
                Logger.LogError($"Error while enabling steam connector: {e}");

                Disable();
            }
        }

        public override void Disable()
        {
            if (!Active) return;

            Logger.LogInfo("Disabling steam connector...");
            try
            {
                _gameRichPresenceJoinRequested.Unregister();
                _gameRichPresenceJoinRequested = null;
                _p2pSessionRequest.Unregister();
                _p2pSessionRequest = null;
                _p2pSessionConnectFail.Unregister();
                _p2pSessionConnectFail = null;

                foreach (CSteamID id in _connected)
                    SteamNetworking.CloseP2PSessionWithUser(id);

                CancelInvoke("Tick");

                SteamFriends.ClearRichPresence();

                base.Disable();

                Logger.LogInfo("Steam connector has been disabled successfully.");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while disabling steam connector: {e}");
            }
        }

        private void Update()
        {
            if (Initialized && Active) SteamAPI.RunCallbacks();
        }

        private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t request)
        {
            // called on the client
            Logger.LogInfo($"Connecting to {request.m_steamIDFriend}...");

            if (_role == ELobbyRole.SERVER)
            {
                Logger.LogError("Cannot join as server.");
                return;
            }

            if (!SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDFriend))
            {
                Logger.LogError($"Failed to accept connection to {request.m_steamIDFriend}!");
                return;
            }

            _hostId = request.m_steamIDFriend;
            _connected.Add(request.m_steamIDFriend);

            SteamFriends.SetRichPresence("connect", request.m_steamIDFriend.ToString());

            if (_role != ELobbyRole.CLIENT)
            {
                Logger.LogInfo("LobbyRole set to CLIENT.");
                _role = ELobbyRole.CLIENT;
            }

            Logger.LogInfo($"Successfully connected to {request.m_steamIDFriend}.");
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t request)
        {
            // called on the server

            if (_role == ELobbyRole.CLIENT)
            {
                Logger.LogError("Cannot be joined as client!");
                return;
            }

            if (!SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDRemote))
            {
                Logger.LogError($"Failed to accept connection from {request.m_steamIDRemote}!");
                return;
            }

            _hostId = CSteamID.Nil;
            _connected.Add(request.m_steamIDRemote);

            if (_role != ELobbyRole.SERVER)
            {
                Logger.LogInfo("LobbyRole set to SERVER.");
                _role = ELobbyRole.SERVER;
            }

            Logger.LogInfo($"Successfully received connection from {request.m_steamIDRemote}.");
        }

        private void OnP2PSessionConnectFail(P2PSessionConnectFail_t fail)
        {
            Logger.LogInfo($"Disconnecting from {fail.m_steamIDRemote} ({fail.m_eP2PSessionError})...");

            if (!SteamNetworking.CloseP2PSessionWithUser(fail.m_steamIDRemote))
            {
                Logger.LogError($"Failed to disconnect from {fail.m_steamIDRemote}");
                return;
            }

            _hostId = CSteamID.Nil;
            _connected.Remove(fail.m_steamIDRemote);

            if (_connected.Count == 0)
            {
                Logger.LogInfo("LobbyRole set to DEFAULT.");
                _role = ELobbyRole.DEFAULT;
                SteamFriends.SetRichPresence("connect", _ownId.ToString());
            }

            Logger.LogInfo($"Disconnected from {fail.m_steamIDRemote} successfully.");
        }

        private void Tick()
        {
            try
            {
                // send
                string updateData = $"{_ownId}::1::{_sync.GetUpdateContent()}";
                if (updateData != null)
                {
                    byte[] updateMsg = Encoding.UTF8.GetBytes(updateData);
                    foreach (CSteamID id in _connected) SteamNetworking.SendP2PPacket(id, updateMsg, (uint) updateMsg.Length, EP2PSend.k_EP2PSendReliable);
                }

                // receive
                while (SteamNetworking.IsP2PPacketAvailable(out uint msgSize))
                {
                    byte[] buffer = new byte[msgSize];

                    if (SteamNetworking.ReadP2PPacket(buffer, msgSize, out _, out CSteamID sender))
                    {
                        string data = Encoding.UTF8.GetString(buffer);
                        _sync.ApplyUpdate(data);

                        if (_role == ELobbyRole.SERVER)
                        {
                            foreach (CSteamID id in _connected)
                                if (id != sender)
                                    SteamNetworking.SendP2PPacket(id, buffer, (uint)buffer.Length, EP2PSend.k_EP2PSendReliable);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error during tick: {e}");
                Disable();
            }
        }
    }
}
