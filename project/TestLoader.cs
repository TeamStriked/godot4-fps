using Godot;
using System;
using FPS.Game.Logic.World;
public partial class TestLoader : Node3D
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ResourceBackgroundLoader.Start();
    }

    public void activate(Node instance)
    {
        this.AddChild(instance);
        // this.ProcessMode = ProcessModeEnum.Always;
    }


    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {

        if (Input.IsActionJustPressed("game_reset_tp"))
        {
            ResourceBackgroundLoader.Add("res://Game/Logic/Player/ServerPlayer.tscn", (Node test) =>
            {
                GD.Print("INSTANCING FINISHED");

                this.CallDeferred("add_child", test);

            });
        }
    }
}
