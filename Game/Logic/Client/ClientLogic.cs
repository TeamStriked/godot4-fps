using Godot;
using System;
using FPS.Game.Config;
using FPS.Game.UI;

namespace FPS.Game.Logic.Client
{
    public partial class ClientLogic : Game.Logic.Networking.NetworkLogic
    {
        [Export]
        NodePath settingsMenuPath = null;

        GameSettings settingsMenu = null;

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
            this.settingsMenu = GetNode(settingsMenuPath) as GameSettings;

            InitNetwork();

            CustomMultiplayer.Connect("connected_to_server", new Callable(this, "onConnected"));
            CustomMultiplayer.Connect("connection_failed", new Callable(this, "onConnectionFailed"));
            CustomMultiplayer.Connect("server_disconnected", new Callable(this, "onDisconnect"));

            if (autoLogin)
            {
                this.doConnect();
            }

            foreach (var id in DisplayServer.GetWindowList())
            {
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled, id);
            }

        }
        [Authority]
        public override void serverAuthSuccessfull(string levelName)
        {
            loadWorld();

            this.World.loadLevel(levelName);
            this.World.setFreeMode(false);

            RpcId(0, "mapLoadedSuccessfull");
        }

        [AnyPeer]
        public override void serverNotReady()
        {
            GD.Print("Server not read");
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

        public override void _Process(float delta)
        {
            base._Process(delta);

            if (Input.IsActionJustPressed("ui_cancel") && this.IsProcessingInput())
            {
                if (Input.GetMouseMode() == Input.MouseMode.Visible)
                {
                    if (!this.settingsMenu.isOpen)
                    {
                        Input.SetMouseMode(Input.MouseMode.Captured);
                    }
                }
                else
                {
                    Input.SetMouseMode(Input.MouseMode.Visible);
                    this.settingsMenu.Show();
                }
            }
        }


        [Authority]
        public override void spwanPlayer(int id, Vector3 origin)
        {
            GD.Print("player spwaned with id " + id + " on " + this.ownNetworkId + " on location " + origin);

            if (id == this.ownNetworkId)
            {
                this.World.spwanLocalPlayer(id, origin);
            }
            else
            {
                this.World.spwanPuppetPlayer(id, origin);
            }
        }
    }
}
