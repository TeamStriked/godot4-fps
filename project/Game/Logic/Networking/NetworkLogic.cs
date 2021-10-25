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

        public override void _EnterTree()
        {
            base._EnterTree();

            RpcConfig("removePlayer", RPCMode.Auth, false, TransferMode.Reliable);
            RpcConfig("spawnPlayer", RPCMode.Auth, false, TransferMode.Reliable);
            RpcConfig("spawnPlayers", RPCMode.Auth, false, TransferMode.Reliable);
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

            GetTree().NodeAdded += onNodeUpdate;
            attachNodesToNetwork(this);
        }

        public virtual void logNetPackage(int id, byte[] packet)
        {
            GD.Print("Client receveid package from " + id + " with" + packet.Length);
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
            if (!node.IsInsideTree())
                return;
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
                GetTree().NodeAdded -= onNodeUpdate;
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
            FPS.Game.Utils.Logger.InfoDraw("Try to add game world..");

            ResourceBackgroundLoader.Add("res://Game/Logic/World/GameWorld.tscn", (Node instancedNode) =>
            {
                this.CallDeferred("addWorld", instancedNode);
            });
        }

        public void addWorld(Node world)
        {
            FPS.Game.Utils.Logger.InfoDraw("Game world added.");

            this._world = (GameWorld)world.Duplicate();
            this._world.Name = "World";
            this._world.Visible = true;
            this._world.Ready += OnGameWorldResourceLoaded;

            this.AddChild(this._world);
        }
        public void doSomethingThread(object userdata = null)
        {
        }

        protected void destroyWorld()
        {
            if (this._world != null)
            {
                this._world.QueueFree();
            }
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
        public virtual void spawnPlayer(int id, Vector3 origin)
        {

        }

        [Authority]
        public virtual void removePlayer(int id, string message)
        {

        }

        [Authority]
        public virtual void spawnPlayers(string message)
        {

        }
    }
}
