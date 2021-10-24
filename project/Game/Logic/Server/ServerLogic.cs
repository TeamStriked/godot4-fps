using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using FPS.Game.Logic.World;

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

        public List<ServerClient> clients = new List<ServerClient>();

        public override void _EnterTree()
        {
            InitNetwork();

            network.SetBindIp(ip);
            network.CreateServer(port, maxPlayers);

            CustomMultiplayer.MultiplayerPeer = network;
            Multiplayer.RootNode = this;

            FPS.Game.Utils.Logger.InfoDraw("[Server] started at port " + port);

            CustomMultiplayer.PeerConnected += onPlayerConnect;
            CustomMultiplayer.PeerDisconnected += onPlayerDisconnect;

        }

        public override void _Ready()
        {
            base._Ready();
            this.loadWorldThreaded();
        }

        protected override void OnGameWorldResourceLoaded()
        {
            FPS.Game.Utils.Logger.InfoDraw("Game world loaded successfull");
            this.World.OnGameLevelLoadedSuccessfull += this.OnLevelLoadedSuccesfull;
            this.World.loadLevelThreaded(this.levelPath);
        }

        protected void OnLevelLoadedSuccesfull()
        {
            this.World.setFreeMode(true);
            this.levelState = Error.Ok;
        }

        public void onPlayerDisconnect(int id)
        {
            FPS.Game.Utils.Logger.InfoDraw("[Server] Client " + id.ToString() + " disconnected.");
            this.World.removePlayer(id);
        }

        public void onPlayerConnect(int id)
        {
            FPS.Game.Utils.Logger.InfoDraw("[Server] Client " + id.ToString() + " connected.");

            if (levelState != Error.Ok)
            {
                FPS.Game.Utils.Logger.InfoDraw("Server not ready now");
            }
            else
            {
                this.clients.Add(new ServerClient(id));
                RpcId(id, "serverAuthSuccessfull", levelPath);
            }
        }

        [AnyPeer]
        public override void mapLoadedSuccessfull()
        {
            var id = Multiplayer.GetRemoteSenderId();

            var client = this.clients.FirstOrDefault(df => df.id == id);
            if (client == null)
                return;

            var spwanPoint = this.World.Level.findFreeSpwanPoint();
            if (spwanPoint != null)
            {
                spwanPoint.inUsage = true;
                FPS.Game.Utils.Logger.InfoDraw("[Server] Client " + id.ToString() + " world loaded.");

                var callback = new GameWorld.CallBackFunction(() =>
                {
                    FPS.Game.Utils.Logger.InfoDraw("[Server] Client " + id.ToString() + " character added.");

                    client.state = ServerClientState.INIT;
                    foreach (var client in clients.Where(df => df.state == ServerClientState.INIT))
                    {
                        if (client.id == id)
                        {
                            var list = this.World.GetPlayers();
                            var sendMessage = FPS.Game.Utils.NetworkCompressor.Compress(list);
                            RpcId(id, "spwanPlayers", sendMessage);
                        }
                        else
                        {
                            RpcId(id, "spwanPlayer", id, spwanPoint.GlobalTransform.origin);
                        }
                    }
                });

                this.World.spwanPlayer(id, spwanPoint.GlobalTransform.origin, Player.PlayerType.Server, callback);
            }
        }

        public void CreatePlayer(int id)
        {

        }

        private void DisconnectClient(int id, string message = "")
        {
            FPS.Game.Utils.Logger.InfoDraw("[Server][Player][" + id + "] Disconnect Reason:" + message);
            //RpcId(id, "forceDisconnect", message);
            network.GetPeer(id).PeerDisconnect();

            this.World.removePlayer(id);
            this.clients.RemoveAll(df => df.id == id);
        }

    }

}
