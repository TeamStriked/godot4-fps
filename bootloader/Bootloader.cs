using Godot;
using FPS.Game.Config;
using System;
using FPS.Game.Logic.Player;
using FPS.Game.Logic.Camera;
public partial class Bootloader : Node
{
    int drawMode = 2;
    int currentActiveInstance = 0;

    public override void _EnterTree()
    {
        base._EnterTree();
        GetTree().Connect("node_added", new Callable(this, "onNodeUpdate"));
    }

    public override void _Ready()
    {
        GD.Print("Bootloading.." + Godot.OS.GetName());

        base._Ready();
        handleDrawMode();
    }

    private void onNodeUpdate(Node node)
    {
        if (!node.IsInsideTree())
            return;

        var childsAmount = GetNode("vbox").GetChildCount();
        bool activated = false;

        if (currentActiveInstance < childsAmount)
        {
            var childs = GetNode("vbox").GetChildren();
            var activeChild = childs[currentActiveInstance] as Node;

            var path = node.GetPath().ToString();
            var mypath = activeChild.GetPath().ToString();

            if (path.Contains(mypath))
            {
                activated = true;
            }
        }

        setDisableOrEnableTree(node, activated);
    }

    private void handleDrawMode()
    {
        var childs = GetNode("vbox").GetChildren();
        var amount = 0;

        foreach (var item in childs)
        {
            var childItem = (item as SubViewportContainer);
            bool canHandle = (amount == currentActiveInstance);
            childItem.Visible = (amount <= drawMode);
            setDisableOrEnableTree(childItem, canHandle);
            GD.Print(amount + " handle: " + canHandle);

            amount++;
        }
    }

    public void setDisableOrEnableTree(Node item, bool canHandle, bool withSubs = true)
    {
        if (item is Node)
        {
            (item as Node).SetProcessInput(canHandle);
            (item as Node).SetProcessUnhandledInput(canHandle);
            (item as Node).SetProcessUnhandledKeyInput(canHandle);
        }

        if (item is SubViewport)
        {
            (item as SubViewport).GuiDisableInput = !canHandle;
        }

        if (item is LocalPlayer)
        {
            (item as LocalPlayer).canHandleInput = canHandle;
        }

        if (item is FreeModeCamera)
        {
            (item as FreeModeCamera).canHandleInput = canHandle;
        }

        if (item is MeshInstance3D)
        {
            (item as MeshInstance3D).Visible = canHandle;
        }

        if (withSubs)
        {
            foreach (var subItem in item.GetChildren())
            {
                if (subItem is Node)
                {
                    setDisableOrEnableTree(subItem as Node, canHandle);
                }
            }
        }


    }

    public override void _Process(float delta)
    {
        //fix godot 4 gc bug
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();

        var childs = GetNode("vbox").GetChildren();

        if (Input.IsActionJustPressed("debug_switch_mode"))
        {
            drawMode++;

            if (drawMode >= childs.Count)
            {
                drawMode = 0;
                currentActiveInstance = 0;
            }

            handleDrawMode();
        }

        if (Input.IsActionJustPressed("debug_switch_frame"))
        {
            currentActiveInstance++;

            if (currentActiveInstance > drawMode)
            {
                //back to 0
                currentActiveInstance = 0;
            }

            handleDrawMode();
        }
    }
}
