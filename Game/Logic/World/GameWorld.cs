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

        protected GameLevel _level = null;
        protected LocalPlayer _localPlayer = null;

        protected Dictionary<int, NetworkPlayer> players = new Dictionary<int, NetworkPlayer>();

        protected string gameLevelName = "";
        protected bool gameLevelOnLoad = false;


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

        public void loadLevelThreaded(string levelName)
        {
            this.gameLevelName = "res://" + levelName;

            GD.Print("Loading game level.. " + this.gameLevelName);

            gameLevelOnLoad = true;
            ResourceLoader.LoadThreadedRequest(this.gameLevelName, "", true);
        }


        private void gameLevelLoadedSuccess(PackedScene scene)
        {
            GD.Print("Loading game level successfull.");

            scene.ResourceLocalToScene = true;

            this._level = (GameLevel)scene.Instantiate();
            this._level.Name = "Level";

            AddChild(this._level);
            OnGameLevelLoadedSuccessfull();
        }

        public override void _Process(float delta)
        {
            if (this.gameLevelOnLoad)
            {
                var status = ResourceLoader.LoadThreadedGetStatus(gameLevelName);
                if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                {
                    this.gameLevelLoadedSuccess(ResourceLoader.LoadThreadedGet(gameLevelName) as PackedScene);
                    this.gameLevelOnLoad = false;
                }
            }
        }

        public void spwanLocalPlayer(int id, Vector3 origin)
        {
            if (this._level == null)
                return;

            if (playerNodePath != null)
            {
                var path = GetNode(playerNodePath);
                if (path != null)
                {

                    PackedScene localPlayerScene;
                    localPlayerScene = ResourceLoader.Load("res://Game/Logic/Player/LocalPlayer.tscn") as PackedScene;
                    localPlayerScene.ResourceLocalToScene = true;

                    this._localPlayer = (LocalPlayer)localPlayerScene.Instantiate();
                    this._localPlayer.Name = id.ToString();
                    path.AddChild(this._localPlayer);

                    this._localPlayer.DoTeleport(origin);
                    this._localPlayer.Activate();
                    this.players.Add(id, this._localPlayer);
                }
            }
        }

        public ServerPlayer spwanServerPlayer(int id, Vector3 origin)
        {

            if (this._level == null)
                return null;

            if (playerNodePath != null)
            {
                var path = GetNode(playerNodePath);
                if (path != null)
                {
                    PackedScene serverPlayerScene;

                    //load cached resource
                    serverPlayerScene = ResourceLoader.Load("res://Game/Logic/Player/ServerPlayer.tscn") as PackedScene;
                    serverPlayerScene.ResourceLocalToScene = true;

                    var player = (ServerPlayer)serverPlayerScene.Instantiate();

                    player.Name = id.ToString();
                    player.networkId = id;
                    path.AddChild(player);

                    this.players.Add(id, player);
                    player.DoTeleport(origin);

                    return player;
                }
            }

            return null;
        }

        public void spwanPuppetPlayer(int id, Vector3 origin)
        {
            if (this._level == null)
                return;

            if (this.players.ContainsKey(id))
                return;

            if (playerNodePath != null)
            {
                var path = GetNode(playerNodePath);
                if (path != null)
                {
                    PackedScene puppetPlayerScene;
                    puppetPlayerScene = ResourceLoader.Load("res://Game/Logic/Player/PuppetPlayer.tscn") as PackedScene;
                    puppetPlayerScene.ResourceLocalToScene = true;

                    var player = (PuppetPlayer)puppetPlayerScene.Instantiate();

                    player.Name = id.ToString();
                    player.networkId = id;
                    path.AddChild(player);

                    this.players.Add(id, player);
                    player.DoTeleport(origin);
                }
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
