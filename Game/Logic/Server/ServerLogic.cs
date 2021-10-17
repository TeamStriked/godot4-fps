using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FPS.Game.Logic.Server
{
    public partial class ServerLogic : Game.Logic.Networking.NetworkLogic
    {
        [Signal]
        public delegate void onServerIsReady();

        [Export]
        public int test = 45;

        [Export]
        public float MAX_DISTANCE = 45.0f;

        [Export]
        public string ip = "0:0:0:0:0:0:0:0";

        [Export]
        public int maxPlayers = 10;

        [Export]
        public int port = 27015;

        [Export]
        public int maxPreAuthWaitTime = 10;

        [Export]
        public int positionSyncTime = 100;

        [Export]
        public string levelPath = "Levels/ExampleLevel.tscn";

        public Error levelState = Error.Failed;

        public override void _EnterTree()
        {
            InitNetwork();

            network.SetBindIp(ip);
            network.CreateServer(port, maxPlayers);

            CustomMultiplayer.MultiplayerPeer = network;
            Multiplayer.RootNode = this;

            GD.Print("[Server] started at port " + port);

            CustomMultiplayer.PeerConnected += onPlayerConnect;
            CustomMultiplayer.PeerDisconnected += onPlayerDisconnect;

            this.loadWorldThreaded();
        }

        protected override void OnGameWorldResourceLoaded()
        {
            GD.Print("Game world loaded successfull");
            this.World.OnGameLevelLoadedSuccessfull += this.OnLevelLoadedSuccesfull;
            this.World.loadLevelThreaded(this.levelPath);
        }

        protected void OnLevelLoadedSuccesfull()
        {
            this.World.setFreeMode(false);
            this.levelState = Error.Ok;
        }

        public void onPlayerDisconnect(int id)
        {
            GD.Print("[Server] Client " + id.ToString() + " disconnected.");
            this.World.removePlayer(id);
        }

        public void onPlayerConnect(int id)
        {
            GD.Print("[Server] Client " + id.ToString() + " connected.");

            if (levelState != Error.Ok)
            {
                GD.Print("Server not ready now");
            }
            else
            {
                RpcId(id, "serverAuthSuccessfull", levelPath);
            }
        }

        [AnyPeer]
        public override void mapLoadedSuccessfull()
        {
            var id = Multiplayer.GetRemoteSenderId();

            var spwanPoint = this.World.Level.findFreeSpwanPoint();
            if (spwanPoint != null)
            {
                spwanPoint.inUsage = true;
                GD.Print("[Server] Client " + id.ToString() + " world loaded.");

                this.World.spwanPlayer(id, spwanPoint.GlobalTransform.origin, Player.PlayerType.Server);
                Rpc("spwanPlayer", id, spwanPoint.GlobalTransform.origin);
            }
        }

        public void CreatePlayer(int id)
        {

        }

        private void DisconnectClient(int id, string message = "")
        {
            GD.Print("[Server][Player][" + id + "] Disconnect Reason:" + message);
            //RpcId(id, "forceDisconnect", message);
            network.GetPeer(id).PeerDisconnect();

            this.World.removePlayer(id);
        }
    }

}
