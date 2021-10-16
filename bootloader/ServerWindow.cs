using Godot;
using System;

public partial class ServerWindow : SubViewportContainer
{
    public override void _EnterTree()
    {
        base._EnterTree();
        GetTree().Connect("node_added", new Callable(this, "onNodeUpdate"));
        (GetNode("viewport") as SubViewport).RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
    }

    private void onNodeUpdate(Node node)
    {
        if (!node.IsInsideTree())
            return;
        var path = GetPath().ToString();
        var mypath = node.GetPath().ToString();
        if (mypath.Contains(path))
        {
            if (node is MeshInstance3D)
            {
                (node as MeshInstance3D).Visible = false;
            }
            if (node is WorldEnvironment)
            {
                (node as WorldEnvironment).Environment = new Godot.Environment();
            }
        }

    }
}
