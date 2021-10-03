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
                (freeNode as FreeModeCamera).activated = value;
                (freeNode as FreeModeCamera).Current = value;
            }
        }

        public Error loadLevel(string levelName)
        {
            GD.Print("Loading game level.. " + levelName);

            var scene = (PackedScene)ResourceLoader.Load("res://" + levelName);
            this._level = (GameLevel)scene.Instantiate();
            this._level.Name = "Level";
            AddChild(this._level);

            return Error.Ok;
        }

        public void spwanLocalPlayer(int id, Vector3 origin)
        {
            if (playerNodePath != null)
            {
                var path = GetNode(playerNodePath);
                if (path != null)
                {
                    var scene = (PackedScene)ResourceLoader.Load("res://Game/Logic/Player/LocalPlayer.tscn");
                    this._localPlayer = (LocalPlayer)scene.Instantiate();
                    this._localPlayer.Name = id.ToString();
                    path.AddChild(this._localPlayer);

                    this._localPlayer.DoTeleport(origin);
                    this._localPlayer.Activate();
                    this.players.Add(id, this._localPlayer);
                }
            }
        }

        public void spwanServerPlayer(int id, Vector3 origin)
        {
            if (playerNodePath != null)
            {
                var path = GetNode(playerNodePath);
                if (path != null)
                {
                    var scene = (PackedScene)ResourceLoader.Load("res://Game/Logic/Player/ServerPlayer.tscn");
                    var player = (ServerPlayer)scene.Instantiate();
                    player.Name = id.ToString();
                    player.networkId = id;
                    path.AddChild(player);

                    this.players.Add(id, player);
                    player.DoTeleport(origin);
                }
            }
        }
    }
}
