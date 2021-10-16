using System.Diagnostics;
using System.Linq;
using Godot;
using System;

public partial class Bootloader : Node
{


    private void createServerWindow(string name = "window")
    {
        GD.Print("Load server..");

        var scene = (PackedScene)ResourceLoader.Load("res://bootloader/ServerWindow.tscn");
        var serverWindow = (ServerWindow)scene.Instantiate();
        serverWindow.Name = name;
        serverWindow.Visible = false;
        GetNode("vbox").AddChild(serverWindow);
    }

    private void createClientWindow(string name = "window")
    {
        GD.Print("Load client..");

        var scene = (PackedScene)ResourceLoader.Load("res://bootloader/ClientWindow.tscn");
        var clientWindow = scene.Instantiate();
        clientWindow.Name = name;
        GetNode("vbox").AddChild(clientWindow);
    }

    public override void _Ready()
    {
        GD.Print("Bootloading..");

        //load to cache
        ResourceLoader.Load("res://Game/Logic/Player/CharacterInstance.tscn");

        if (OS.GetCmdlineArgs().Contains("-server"))
        {
            this.createServerWindow();
        }
        else if (OS.GetCmdlineArgs().Contains("-client"))
        {
            this.createClientWindow();
        }
        else if (OS.GetCmdlineArgs().Contains("-serverclient") || OS.GetCmdlineArgs().Contains("-clientserver"))
        {
            this.createServerWindow("ServerWindow");
            this.createClientWindow("ClientWindow");
        }
        else
        {
            this.createClientWindow("ClientWindow");
        }

        base._Ready();
    }

}
