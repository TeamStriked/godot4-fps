using Godot;
using System;

[Tool]
public partial class GunCamera : Camera3D
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    Camera3D cam;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        cam = GetParent().GetParent().GetParent<Camera3D>();
        //  this.Visible = cam.Visible;
        this.Visible = false;
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        this.GlobalTransform = cam.GlobalTransform;
        this.Fov = cam.Fov;
        this.Near = cam.Near;
        this.Far = cam.Far;
        //  this.Visible = cam.Visible;


    }
}
