using Godot;
using System;

[Tool]
public partial class MotionBlur : MeshInstance3D
{

    Vector3 cam_pos_prev = Vector3.Zero;
    Quaternion cam_rot_prev = Quaternion.Identity;

    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }

    public override void _Process(float delta)
    {
        var mat = GetSurfaceOverrideMaterial(0) as ShaderMaterial;
        var cam = GetParent<Camera3D>();
        var body = cam.GetParent().GetParent<CharacterBody3D>();
        var cam_rot = new Quaternion(cam.GlobalTransform.basis);
        var cam_rot_diff = cam_rot - cam_rot_prev;
        if (cam_rot.Dot(cam_rot_prev) < 0.0)
        {
            cam_rot_diff = -cam_rot_diff;
        }

        var cam_rot_conj = this.conjugate(cam_rot);
        var ang_vel = (cam_rot_diff * 2.0f) * cam_rot_conj;

        mat.SetShaderParam("linear_velocity", body.MotionVelocity * 0.01f);
        mat.SetShaderParam("angular_velocity", new Vector3(ang_vel.x, ang_vel.y, ang_vel.z));

        cam_pos_prev = cam.GlobalTransform.origin;
        cam_rot_prev = new Quaternion(cam.GlobalTransform.basis);
    }

    private Quaternion conjugate(Quaternion quat)
    {
        return new Quaternion(-quat.x, -quat.y, -quat.z, quat.w);
    }
}
