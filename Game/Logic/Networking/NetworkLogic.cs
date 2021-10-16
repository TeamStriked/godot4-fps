using Godot;
using System;
using FPS.Game.Logic.World;

namespace FPS.Game.Logic.Networking
{
    public abstract partial class NetworkLogic : Node
    {
        protected abstract void OnGameWorldResourceLoaded();

        protected ENetMultiplayerPeer network = null;
        protected GameWorld _world = null;

        public GameWorld World
        {
            get { return this._world; }
        }

        public override void _Ready()
        {
            base._Ready();

            RpcConfig("spwanPlayer", RPCMode.Auth, false, TransferMode.Reliable);
            RpcConfig("serverAuthSuccessfull", RPCMode.Auth, false, TransferMode.Reliable);
            RpcConfig("serverNotReady", RPCMode.Auth, false, TransferMode.Reliable);
            RpcConfig("mapLoadedSuccessfull", RPCMode.AnyPeer, false, TransferMode.Reliable);
        }


        public void InitNetwork()
        {
            GetTree().MultiplayerPoll = false;

            CustomMultiplayer = new MultiplayerAPI();
            CustomMultiplayer.RootNode = this;

            network = new ENetMultiplayerPeer();

            GetTree().Connect("node_added", new Callable(this, "onNodeUpdate"));
            attachNodesToNetwork(this);
        }


        public override void _Process(float delta)
        {
            if (CustomMultiplayer != null && CustomMultiplayer.HasMultiplayerPeer())
            {
                CustomMultiplayer.Poll();
            }


        }

        private void onNodeUpdate(Node node)
        {
            var path = node.GetPath().ToString();
            var mypath = GetPath().ToString();

            if (!path.Contains(mypath))
            {
                return;
            }
            var rel = path.Replace(mypath, "");

            if (rel.Length() > 0 && !rel.StartsWith("/"))
                return;


            node.CustomMultiplayer = CustomMultiplayer;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationExitTree)
            {
                GetTree().Disconnect("node_added", new Callable(this, "onNodeUpdate"));
            }
        }

        private void attachNodesToNetwork(Node _node)
        {
            foreach (Node ar in _node.GetChildren())
            {
                ar.CustomMultiplayer = CustomMultiplayer;

                if (ar.GetChildCount() > 0)
                    attachNodesToNetwork(ar);
            }
        }

        protected void loadWorldThreaded()
        {
            var scene = ResourceLoader.Load("res://Game/Logic/World/GameWorld.tscn") as PackedScene;

            GD.Print("Add game world..");
            scene.ResourceLocalToScene = true;
            this._world = (GameWorld)scene.Instantiate();
            this._world.Name = "World";

            AddChild(this._world);
            OnGameWorldResourceLoaded();
        }

        protected void destroyWorld()
        {
            RemoveChild(this._world);
            this._world = null;
        }

        [Authority]
        public virtual void serverAuthSuccessfull(string levelName)
        {
        }

        [AnyPeer]
        public virtual void serverNotReady()
        {
        }

        [AnyPeer]
        public virtual void mapLoadedSuccessfull()
        {
        }

        [Authority]
        public virtual void spwanPlayer(int id, Vector3 origin)
        {

        }




    }
}
