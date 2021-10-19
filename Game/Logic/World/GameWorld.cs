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

        ResourceBackgroundLoader _gameMapLoader = new ResourceBackgroundLoader();

        public override void _EnterTree()
        {
            base._EnterTree();
            this._decalNode = GetNode<Node3D>(decalNodePath);
            _gameMapLoader.OnLoaderComplete += LoadCompleteGameLevel;

            OnNewDecal += AddDecal;
        }

        public void loadLevelThreaded(string levelName)
        {
            this.gameLevelName = "res://" + levelName;

            GD.Print("Loading game level.. " + this.gameLevelName);
            this._gameMapLoader.Load(this.gameLevelName);
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

        public void LoadCompleteGameLevel(PackedScene scene)
        {
            scene.ResourceLocalToScene = true;

            this._level = (GameLevel)scene.Instantiate();
            this._level.Name = "Level";

            this.CallDeferred("add_child", this._level);
            OnGameLevelLoadedSuccessfull();
        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            _gameMapLoader.Tick();
        }

        private void AddPlayerAfterThreadLoading(int id, NetworkPlayer player, Vector3 origin)
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

        public void addLocalPlayerThreaded(Godot.Collections.Array info)
        {
            int id = int.Parse(info[0].ToString());
            Vector3 origin = (Vector3)info[1];
            PlayerType type = (PlayerType)Enum.ToObject(typeof(PlayerType), info[2]);

            PackedScene packedScene;

            if (type == PlayerType.Local)
            {
                packedScene = ResourceLoader.Load("res://Game/Logic/Player/LocalPlayer.tscn") as PackedScene;
                packedScene.ResourceLocalToScene = true;

                this._localPlayer = (LocalPlayer)packedScene.Instantiate();
                this.CallDeferred("AddPlayerAfterThreadLoading", id, this._localPlayer, origin);
            }
            else if (type == PlayerType.Server)
            {
                packedScene = ResourceLoader.Load("res://Game/Logic/Player/ServerPlayer.tscn") as PackedScene;
                packedScene.ResourceLocalToScene = true;

                var player = (ServerPlayer)packedScene.Instantiate();
                this.CallDeferred("AddPlayerAfterThreadLoading", id, player, origin);

            }
            else if (type == PlayerType.Puppet)
            {
                packedScene = ResourceLoader.Load("res://Game/Logic/Player/PuppetPlayer.tscn") as PackedScene;
                packedScene.ResourceLocalToScene = true;

                var player = (PuppetPlayer)packedScene.Instantiate();
                this.CallDeferred("AddPlayerAfterThreadLoading", id, player, origin);
            }
        }


        public void spwanPlayer(int id, Vector3 origin, PlayerType type)
        {
            if (this._level == null)
                return;

            if (playerNodePath != null)
            {
                var thread = new Godot.Thread();
                var attributes = new object();

                var col = new Godot.Collections.Array(id, origin, type);
                thread.Start(new Callable(this, "addLocalPlayerThreaded"), col);
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
