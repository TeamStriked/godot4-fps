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
        public string ip = "0.0.0.0";

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

        public static List<ServerClient> clients = new List<ServerClient>();

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

        protected override void afterNodeUpdate(Node node)
        {
            if (node is MeshInstance3D)
            {
                (node as MeshInstance3D).Visible = false;
            }
            if (node is Light3D)
            {
                (node as Light3D).ShadowEnabled = false;
            }
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
            this.World.OnPlayerInstanceCreated += this.PlayerInstanceCreated;
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
            this.DisconnectClient(id, "Timeout");
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
                clients.Add(new ServerClient(id));
                RpcId(id, "serverAuthSuccessfull", levelPath);
            }
        }

        [AnyPeer]
        public override void mapLoadedSuccessfull()
        {
            var id = Multiplayer.GetRemoteSenderId();

            var client = clients.FirstOrDefault(df => df.id == id);
            if (client == null)
                return;


            var spwanPoint = this.World.Level.findFreeSpwanPoint();
            if (spwanPoint != null)
            {
                spwanPoint.inUsage = true;
                client.spawnPoint = spwanPoint;
                FPS.Game.Utils.Logger.InfoDraw("[Server] Client " + id.ToString() + " world loaded.");
                this.World.spawnPlayer(id, spwanPoint.GlobalTransform.origin, Player.PlayerType.Server);
            }
            else
            {
                FPS.Game.Utils.Logger.InfoDraw("[Server] Client " + id.ToString() + " has no free spawnpoint.");
                this.DisconnectClient(id, "No free spawnpoint.");
            }
        }

        public void PlayerInstanceCreated(int id, Vector3 origin)
        {
            var client = clients.FirstOrDefault(df => df.id == id);
            if (client == null)
                return;

            FPS.Game.Utils.Logger.InfoDraw("[Server] Client " + id.ToString() + " character added.");
            client.state = ServerClientState.INIT;

            foreach (var currentClient in clients.Where(df => df.state == ServerClientState.INIT))
            {
                if (currentClient.id == id)
                {
                    var list = this.World.GetPlayers();
                    var sendMessage = FPS.Game.Utils.NetworkCompressor.Compress(list);
                    RpcId(currentClient.id, "spawnPlayers", sendMessage);
                }
                else
                {
                    RpcId(currentClient.id, "spawnPlayer", id, origin);
                }
            }
        }

        private void DisconnectClient(int id, string message = "")
        {
            var client = clients.FirstOrDefault(df => df.id == id);
            if (client != null)
            {
                if (client.spawnPoint != null)
                    client.spawnPoint.inUsage = false;
            }

            FPS.Game.Utils.Logger.InfoDraw("[Server][Player][" + id + "] Disconnect Reason:" + message);

            if (network.GetPeer(id) != null && network.GetPeer(id).GetState() == ENetPacketPeer.PeerState.Connected)
            {
                network.GetPeer(id).PeerDisconnect();
            }

            Rpc("removePlayer", id, message);

            this.World.removePlayer(id);
            clients.RemoveAll(df => df.id == id);
        }

    }

}
