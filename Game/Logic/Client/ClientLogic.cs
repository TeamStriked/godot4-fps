using Godot;
using System;
using FPS.Game.Config;
using FPS.Game.UI;

namespace FPS.Game.Logic.Client
{
    public partial class ClientLogic : FPS.Game.Logic.Networking.NetworkLogic
    {
        [Export]
        NodePath settingsMenuPath = null;

        [Export]
        NodePath mainMenuPath = null;

        GameSettings settingsMenu = null;

        MainMenu mainMenu = null;

        [Export]
        public string hostname = "localhost";

        [Export]
        public int port = 27015;

        [Export]
        public bool autoLogin = false;

        private int ownNetworkId = 0;

        public static int serverId = 1;

        private string currentLevelName = "";

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            this.settingsMenu = GetNode(settingsMenuPath) as GameSettings;
            this.mainMenu = GetNode(mainMenuPath) as MainMenu;

            InitNetwork();

            CustomMultiplayer.Connect("connected_to_server", new Callable(this, "onConnected"));
            CustomMultiplayer.Connect("connection_failed", new Callable(this, "onConnectionFailed"));
            CustomMultiplayer.Connect("server_disconnected", new Callable(this, "onDisconnect"));

            this.mainMenu.onUpdateConnectEvent += this.doConnect;

            if (autoLogin)
            {
                this.doConnect(hostname, port);
            }

            foreach (var id in DisplayServer.GetWindowList())
            {
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled, id);
            }

        }
        [Authority]
        public override void serverAuthSuccessfull(string levelName)
        {
            serverId = Multiplayer.GetRemoteSenderId();
            this.currentLevelName = levelName;
            loadWorldThreaded();
        }

        protected override void OnGameWorldResourceLoaded()
        {
            this.World.OnGameLevelLoadedSuccessfull += this.OnLevelLoadedSuccesfull;
            this.World.loadLevelThreaded(this.currentLevelName);
        }

        protected void OnLevelLoadedSuccesfull()
        {
            GD.Print("Level loaded successfull");
            this.World.setFreeMode(false);
            RpcId(serverId, "mapLoadedSuccessfull");
        }

        [AnyPeer]
        public override void serverNotReady()
        {
            GD.Print("Server not read");
        }

        public void doConnect(string hostname, int port)
        {
            this.mainMenu.Hide();
            var realIP = IP.ResolveHostname(hostname, IP.Type.Ipv4);
            if (realIP.IsValidIPAddress())
            {

                drawSystemMessage("Try to connect to " + realIP + ":" + port);
                var error = network.CreateClient(realIP, port);
                if (error != Error.Ok)
                {
                    drawSystemMessage("Network error:" + error.ToString());
                    this.mainMenu.ProcessMode = ProcessModeEnum.Disabled;
                    this.mainMenu.Show();
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
            this.destroyWorld();
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

            if (Input.IsActionJustPressed("ui_cancel"))
            {
                GD.Print("PRESSED");
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
            GD.Print("player spwaned with id " + id + " on " + this.ownNetworkId + " on location " + origin + " serverID " + serverId);

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
