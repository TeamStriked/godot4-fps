using Godot;
using System;

public partial class ServerWindow : SubViewportContainer
{
    public override void _EnterTree()
    {
        base._EnterTree();
        GetTree().NodeAdded += onNodeUpdate;
    }

    private void onNodeUpdate(Node node)
    {
        if (!node.IsInsideTree())
            return;

        var path = GetPath().ToString();
        var mypath = node.GetPath().ToString();


    }
}
