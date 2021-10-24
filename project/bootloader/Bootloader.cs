using System.Diagnostics;
using System.Linq;
using Godot;
using System;

public partial class Bootloader : Node
{


    private void createServerWindow(string name = "window")
    {
        FPS.Game.Utils.Logger.InfoDraw("Load server..");

        var scene = (PackedScene)ResourceLoader.Load("res://bootloader/ServerWindow.tscn");
        var serverWindow = (ServerWindow)scene.Instantiate();
        serverWindow.Name = name;
        //  serverWindow.Visible = false;
        GetNode("box").AddChild(serverWindow);
    }


    private void createClientWindow(string name = "window")
    {
        FPS.Game.Utils.Logger.InfoDraw("Load client..");

        var scene = (PackedScene)ResourceLoader.Load("res://bootloader/ClientWindow.tscn");
        var clientWindow = scene.Instantiate();
        clientWindow.Name = name;
        GetNode("box").AddChild(clientWindow);
    }

    public override void _Ready()
    {
        FPS.Game.Logic.World.ResourceBackgroundLoader.Start();
        FPS.Game.Utils.Logger.InfoDraw("Bootloading..");

        if (OS.GetCmdlineArgs().Contains("-server"))
        {
            this.createServerWindow();
        }
        else if (OS.GetCmdlineArgs().Contains("-client"))
        {
            this.createClientWindow();
        }
        else
        {
            this.createServerWindow("ServerWindow");
            this.createClientWindow("ClientWindow");
        }

        base._Ready();
    }

}
