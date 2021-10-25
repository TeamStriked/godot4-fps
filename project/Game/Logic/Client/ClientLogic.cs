using System.Collections.Generic;
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
        [Export]
        NodePath gameGraphPath = null;

        GameSettings settingsMenu = null;

        GameGraph graphMenu = null;

        MainMenu mainMenu = null;

        public string hostname = "localhost";

        [Export]
        public int port = 27015;

        [Export]
        public bool autoLogin = false;

        private int ownNetworkId = 0;

        public static int serverId = 1;

        private string currentLevelName = "";

        public override void logNetPackage(int id, byte[] packet)
        {
            GD.Print("Client receveid package from " + id + " with" + packet.Length);
        }


        // Called when the node enters the scene tree for the first time.
        public override void _EnterTree()
        {
            this.settingsMenu = GetNode(settingsMenuPath) as GameSettings;
            this.mainMenu = GetNode(mainMenuPath) as MainMenu;
            this.graphMenu = GetNode(gameGraphPath) as GameGraph;

            InitNetwork();
            CustomMultiplayer.ConnectedToServer += onConnected;
            CustomMultiplayer.ConnectionFailed += onConnectionFailed;
            CustomMultiplayer.ServerDisconnected += onDisconnect;
            this.mainMenu.onUpdateConnectEvent += this.doConnect;

            if (autoLogin)
            {
                this.doConnect(hostname, port);
            }

            foreach (var id in DisplayServer.GetWindowList())
            {
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled, id);
            }

            this.settingsMenu.OnDisconnect += () =>
            {
                network.CloseConnection();
                this.handleDisconnect();
            };
        }


        [Authority]
        public override void serverAuthSuccessfull(string levelName)
        {
            serverId = Multiplayer.GetRemoteSenderId();
            this.currentLevelName = levelName;
            this.CallDeferred("loadWorldThreaded");
        }

        protected override void OnGameWorldResourceLoaded()
        {
            this.World.OnGameLevelLoadedSuccessfull += this.OnLevelLoadedSuccesfull;
            this.World.loadLevelThreaded(this.currentLevelName);
        }

        protected void OnLevelLoadedSuccesfull()
        {
            FPS.Game.Utils.Logger.InfoDraw("[Client] Level loaded successfull");

            this.World.setFreeMode(false);
            RpcId(serverId, "mapLoadedSuccessfull");
        }

        [AnyPeer]
        public override void serverNotReady()
        {
            FPS.Game.Utils.Logger.LogError("[Client] Server is not ready");
        }

        bool isConnected = false;

        public void doConnect(string hostname, int port)
        {
            this.mainMenu.Hide();

            var realIP = IP.ResolveHostname(hostname, IP.Type.Ipv4);
            FPS.Game.Utils.Logger.InfoDraw("Try to connect to " + realIP + ":" + port);
            var error = network.CreateClient(realIP, port);
            if (error != Error.Ok)
            {
                isConnected = true;
                FPS.Game.Utils.Logger.InfoDraw("Network error: " + error.ToString());
                this.mainMenu.Show();
            }

            CustomMultiplayer.MultiplayerPeer = network;
        }

        public void onConnectionFailed()
        {
            FPS.Game.Utils.Logger.InfoDraw("Error cant connect.");
            isConnected = false;
        }

        public void onDisconnect()
        {
            FPS.Game.Utils.Logger.InfoDraw("Server disconnected.");
            this.handleDisconnect();
            isConnected = false;
        }

        private void handleDisconnect()
        {
            ownNetworkId = 0;
            FPS.Game.Utils.Logger.InfoDraw("Client is disconnected.");
            this.destroyWorld();
            this.settingsMenu.Hide();
            this.mainMenu.Show();
        }

        public void onConnected()
        {
            ownNetworkId = CustomMultiplayer.GetUniqueId();
            FPS.Game.Utils.Logger.InfoDraw("[Client] Connection established.Your id is " + ownNetworkId.ToString());
            isConnected = true;
        }

        public Timer timer = new Timer();

        public override void _Ready()
        {
            base._Ready();

            this.AddChild(timer);
            timer.Autostart = true;
            timer.Start();
            timer.WaitTime = 1.0f;
            timer.Connect("timeout", new Callable(this, "StatNetworkTraffic"));
        }

        public void StatNetworkTraffic()
        {
            if (isConnected)
            {
                this.graphMenu.inTraffic = network.Host.PopStatistic(ENetConnection.HostStatistic.ReceivedData);
                this.graphMenu.outTraffic = network.Host.PopStatistic(ENetConnection.HostStatistic.SentData);
            }
        }

        public override void _Process(float delta)
        {
            base._Process(delta);

            if (Input.IsActionJustPressed("ui_cancel"))
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
        public override void spawnPlayer(int id, Vector3 origin)
        {
            if (id != this.ownNetworkId)
            {
                this.World.spawnPlayer(id, origin, Player.PlayerType.Puppet);
            }
        }


        [Authority]
        public override void removePlayer(int id, string message)
        {
            if (id != this.ownNetworkId)
            {
                FPS.Game.Utils.Logger.LogError("[Client][" + id + "]" + " Disconnected with reason " + message);
                this.World.removePlayer(id);
            }
        }

        [Authority]
        public override void spawnPlayers(string message)
        {
            var uncompress = FPS.Game.Utils.NetworkCompressor.Decompress<Dictionary<int, Vector3>>(message);

            foreach (var item in uncompress)
            {
                if (item.Key == this.ownNetworkId)
                {
                    this.World.spawnPlayer(item.Key, item.Value, Player.PlayerType.Local);
                }
                else
                {
                    this.World.spawnPlayer(item.Key, item.Value, Player.PlayerType.Puppet);
                }
            }
        }
    }
}
