using Godot;
using System;

public partial class ServerWindow : SubViewportContainer
{
    public override void _EnterTree()
    {
        base._EnterTree();
        GetTree().Connect("node_added", new Callable(this, "onNodeUpdate"));
    }

    private void onNodeUpdate(Node node)
    {
        if (!node.IsInsideTree())
            return;
        if (node is MeshInstance3D)
        {
            var path = GetPath().ToString();
            var mypath = node.GetPath().ToString();

            if (mypath.Contains(path))
            {
                (node as MeshInstance3D).Visible = false;
            }
        }
    }
}
