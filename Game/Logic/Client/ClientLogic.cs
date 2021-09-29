using Godot;
using System;

namespace FPS.Game.Logic.Client
{
    public partial class ClientLogic : Game.Logic.Networking.NetworkLogic
    {
        [Export]
        public string hostname = "localhost";

        [Export]
        public int port = 27015;

        [Export]
        public bool autoLogin = true;

        private int ownNetworkId = 0;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            InitNetwork();

            CustomMultiplayer.Connect("connected_to_server", new Callable(this, "onConnected"));
            CustomMultiplayer.Connect("connection_failed", new Callable(this, "onConnectionFailed"));
            CustomMultiplayer.Connect("server_disconnected", new Callable(this, "onDisconnect"));

            if (autoLogin)
            {
                this.doConnect();
            }
        }

        [Puppet]
        public override void serverAuthSuccessfull(string levelName)
        {
            loadWorld();

            this.World.loadLevel(levelName);
            this.World.setFreeMode(false);
            this.World.spwanLocalPlayer(this.ownNetworkId, Vector3.Zero);

            RpcId(0, "mapLoadedSuccessfull");
        }

        public void doConnect()
        {
            var realIP = IP.ResolveHostname(hostname, IP.Type.Ipv4);
            if (realIP.IsValidIPAddress())
            {

                drawSystemMessage("Try to connect to " + realIP + ":" + port);
                var error = network.CreateClient(realIP, port);
                if (error != Error.Ok)
                {
                    drawSystemMessage("Network error:" + error.ToString());
                }

                CustomMultiplayer.MultiplayerPeer = network;
            }
            else
            {
                drawSystemMessage("Hostname not arreachable.");
            }
        }

        public void onConnectionFailed()
        {
            drawSystemMessage("Error cant connect.");
        }

        public void onDisconnect()
        {
            drawSystemMessage("Server disconnected.");
        }

        public void onConnected()
        {
            ownNetworkId = CustomMultiplayer.GetUniqueId();
            drawSystemMessage("Connection established. Your id is " + ownNetworkId.ToString());
        }
        private void drawSystemMessage(string message)
        {
            GD.Print("[Client] " + message);
        }
    }

}
