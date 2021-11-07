using System.Linq;
using Godot;
using System;
using FPS.Game.Logic.Camera;
using FPS.Game.Logic.Level;
using FPS.Game.Logic.Player;
using FPS.Game.Logic.Server;
using System.Collections.Generic;

namespace FPS.Game.Logic.World
{
    public partial class GameWorld : Node3D
    {
        [Export]
        NodePath freeModeCameraNodePath = null;

        [Export]
        NodePath playerNodePath = null;

        [Export]
        NodePath decalNodePath = null;

        protected Node3D _decalNode = null;
        protected GameLevel _level = null;
        protected LocalPlayer _localPlayer = null;

        protected Dictionary<int, NetworkPlayer> players = new Dictionary<int, NetworkPlayer>();

        protected string gameLevelName = "";

        public delegate void GameLevelLoadedSuccessfull();
        public event GameLevelLoadedSuccessfull OnGameLevelLoadedSuccessfull;

        public NetworkPlayer getPlayer(int id)
        {
            return this.players[id];
        }

        public void sendDecalsToClients(Vector3 point, Vector3 normal)
        {
            foreach (var item in ServerLogic.clients.Where(df => df.state == ServerClientState.INIT))
            {
                RpcId(item.id, "onNewDecal", point, normal);
            }
        }

        [Authority]
        public void onNewDecal(Vector3 point, Vector3 normal)
        {
            this.AddDecal(point, normal);
        }

        [Authority]
        public void onNetworkTeleport(int id, Vector3 origin)
        {
            FPS.Game.Utils.Logger.InfoDraw("Cliend received teleport on " + origin);
            if (this.players.ContainsKey(id))
            {
                this.players[id].DoTeleport(origin);
            }
        }

        [Authority]
        public void onPuppetUpdate(int id, string message)
        {
            if (this.players.ContainsKey(id) && this.players[id] is PuppetPlayer)
            {
                var puppetFrame = FPS.Game.Utils.NetworkCompressor.Decompress<CalculatedServerFrame>(message);
                (this.players[id] as PuppetPlayer).IncomingServerFrame(puppetFrame);
            }
        }

        public void updateAllPuppets(int puppetId, string message)
        {
            foreach (var currentClient in ServerLogic.clients.Where(df => df.id != puppetId && df.state == ServerClientState.INIT))
            {
                RpcId(currentClient.id, "onPuppetUpdate", puppetId, message);
            }
        }

        public Dictionary<int, Vector3> GetPlayers()
        {
            var dic = new Dictionary<int, Vector3>();
            foreach (KeyValuePair<int, NetworkPlayer> item in this.players)
            {
                dic.Add(item.Key, item.Value.GlobalTransform.origin);
            }
            return dic;
        }

        public GameLevel Level
        {
            get { return this._level; }
        }

        public void setFreeMode(bool value)
        {
            if (freeModeCameraNodePath == null)
                return;

            var freeNode = GetNodeOrNull(freeModeCameraNodePath);

            if (freeNode != null)
            {
                (freeNode as FreeModeCamera).Visible = value;
                (freeNode as FreeModeCamera).activated = value;
                (freeNode as FreeModeCamera).Current = value;
            }
        }

        public override void _EnterTree()
        {
            base._EnterTree();

            RpcConfig("onNetworkTeleport", RPCMode.Auth, false, TransferMode.Reliable);
            RpcConfig("onPuppetUpdate", RPCMode.Auth, false, TransferMode.Unreliable);
            RpcConfig("onNewDecal", RPCMode.Auth, false, TransferMode.Unreliable);

            this._decalNode = GetNode<Node3D>(decalNodePath);

        }


        public void loadLevelThreaded(string levelName)
        {
            FPS.Game.Utils.Logger.InfoDraw("Loading game level.." + this.gameLevelName);

            this.gameLevelName = "res://" + levelName;

            ResourceBackgroundLoader.Add(this.gameLevelName, (Node instancedNode) =>
            {
                FPS.Game.Utils.Logger.InfoDraw("Game level loaded");
                this.CallDeferred("loadLevel", instancedNode);
            });
        }

        public void loadLevel(Node instancedNode)
        {
            this._level = (GameLevel)instancedNode.Duplicate();
            this._level.Name = "Level";
            this._level.Ready += () =>
             {
                 if (OnGameLevelLoadedSuccessfull != null)
                     OnGameLevelLoadedSuccessfull();
             };

            this.AddChild(this._level);
        }

        private void AddDecal(Vector3 point, Vector3 normal)
        {
            var resource = GD.Load("res://Game/Logic/World/WallDecal.tscn") as PackedScene;
            var decal = resource.Instantiate() as Decal;

            this._decalNode.AddChild(decal);

            var gt = decal.GlobalTransform;
            gt.origin = point;
            decal.GlobalTransform = gt;

            decal.LookAt(point + normal, new Vector3(1, 1, 0));
            var rot = decal.Rotation;
            rot.x -= Mathf.Deg2Rad(90);
            decal.Rotation = rot;
        }

        public delegate void PlayerInstanceCreated(int id, Vector3 origin);

        [Signal]
        public event PlayerInstanceCreated OnPlayerInstanceCreated;

        private void AddPlayerInstance(int id, Vector3 origin, PlayerType type)
        {
            if (type == PlayerType.Local)
            {
                ResourceBackgroundLoader.Add("res://Game/Logic/Player/LocalPlayer.tscn", (Node instance) =>
                {
                    this.CallDeferred("addPlayer", instance as LocalPlayer, id, origin);
                });
            }
            else if (type == PlayerType.Server)
            {
                ResourceBackgroundLoader.Add("res://Game/Logic/Player/ServerPlayer.tscn", (Node instance) =>
                {
                    this.CallDeferred("addPlayer", instance as ServerPlayer, id, origin);

                });
            }
            else if (type == PlayerType.Puppet)
            {
                ResourceBackgroundLoader.Add("res://Game/Logic/Player/PuppetPlayer.tscn", (Node instance) =>
                {
                    this.CallDeferred("addPlayer", instance as PuppetPlayer, id, origin);
                });
            }
        }

        public void addPlayer(NetworkPlayer player, int id, Vector3 origin)
        {
            if (this.players.ContainsKey(id))
                return;

            player = (NetworkPlayer)player.Duplicate();
            var path = GetNode(playerNodePath);
            if (path != null)
            {

                this.players.Add(id, player);
                FPS.Game.Utils.Logger.InfoDraw("[Player] Added " + id + player.GetType() + " at " + origin);

                player.world = this;
                player.Name = id.ToString();
                player.networkId = id;
                path.AddChild(player);

                player.DoTeleport(origin);
                player.Activate();

                if (OnPlayerInstanceCreated != null)
                    OnPlayerInstanceCreated(id, origin);

            }
        }

        public void spawnPlayer(int id, Vector3 origin, PlayerType type)
        {
            if (this.players.ContainsKey(id))
                return;

            FPS.Game.Utils.Logger.InfoDraw("[Player] Spwan " + id + " on location " + origin + " type " + type);

            if (this._level == null)
                return;

            if (playerNodePath != null)
            {
                this.AddPlayerInstance(id, origin, type);
            }
        }

        public void removePlayer(int id)
        {
            var path = GetNode(playerNodePath);

            if (this.players.ContainsKey(id))
            {
                var obj = path.GetNodeOrNull(id.ToString());
                if (obj != null)
                    obj.QueueFree();

                this.players.Remove(id);
            }
        }
    }
}
