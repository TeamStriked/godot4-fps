using System.Linq;
using Godot;
using System;
using FPS.Game.Logic.Camera;
using FPS.Game.Logic.Level;
using FPS.Game.Logic.Player;
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
            this._decalNode = GetNode<Node3D>(decalNodePath);

            OnNewDecal += AddDecal;
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


        public delegate void NewDecal(Vector3 point, Vector3 normal, StaticBody3D collider);
        public static event NewDecal OnNewDecal;

        public static void TriggerNewDecal(Vector3 point, Vector3 normal, StaticBody3D collider)
        {
            OnNewDecal(point, normal, collider);
        }

        public void AddDecal(Vector3 point, Vector3 normal, StaticBody3D collider)
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


        public delegate void CallBackFunction();

        private void AddPlayerInstance(int id, Vector3 origin, PlayerType type, CallBackFunction callback = null)
        {
            if (type == PlayerType.Local)
            {
                FPS.Game.Utils.Logger.InfoDraw("Try to load local player");

                ResourceBackgroundLoader.Add("res://Game/Logic/Player/LocalPlayer.tscn", (Node instance) =>
                {
                    this.CallDeferred("addPlayer", instance as LocalPlayer, id, origin);
                    if (callback != null)
                        callback();
                });
            }
            else if (type == PlayerType.Server)
            {
                FPS.Game.Utils.Logger.InfoDraw("Try to load server player");
                ResourceBackgroundLoader.Add("res://Game/Logic/Player/ServerPlayer.tscn", (Node instance) =>
                {
                    this.CallDeferred("addPlayer", instance as ServerPlayer, id, origin);
                    if (callback != null)
                        callback();
                });

            }
            else if (type == PlayerType.Puppet)
            {
                FPS.Game.Utils.Logger.InfoDraw("Try to load puppet player");
                ResourceBackgroundLoader.Add("res://Game/Logic/Player/PuppetPlayer.tscn", (Node instance) =>
                {
                    this.CallDeferred("addPlayer", instance as PuppetPlayer, id, origin);
                    if (callback != null)
                        callback();
                });
            }
        }

        public void addPlayer(NetworkPlayer player, int id, Vector3 origin)
        {
            this.players.Add(id, player);

            var path = GetNode(playerNodePath);
            if (path != null)
            {
                player.Name = id.ToString();
                player.networkId = id;

                path.AddChild(player);
                player.DoTeleport(origin);
                player.Activate();
            }
        }

        public void spwanPlayer(int id, Vector3 origin, PlayerType type, CallBackFunction callback = null)
        {
            FPS.Game.Utils.Logger.InfoDraw("[Player] Spwan " + id + " on location " + origin + " type " + type);

            if (this._level == null)
                return;

            if (playerNodePath != null)
            {
                this.AddPlayerInstance(id, origin, type, callback);
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
