using Godot;
using FPS.Game.Config;
using System;
using FPS.Game.Logic.Player;
using FPS.Game.Logic.Camera;
public partial class Bootloader : Node
{
    int drawMode = 0;
    int currentActiveInstance = 0;

    public override void _Ready()
    {
        GD.Print("Bootloading..");

        base._Ready();
        handleDrawMode();
    }

    private void handleDrawMode()
    {
        var childs = GetNode("vbox").GetChildren();
        var amount = 0;
        GD.Print(currentActiveInstance);
        foreach (var item in childs)
        {
            var childItem = (item as SubViewportContainer);
            bool canHandle = (amount == currentActiveInstance);
            childItem.Visible = (amount <= drawMode);
            setDisableOrEnableTree(childItem, canHandle);

            amount++;
        }
    }

    public void setDisableOrEnableTree(Node item, bool canHandle)
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

        foreach (var subItem in item.GetChildren())
        {
            if (subItem is Node)
            {
                setDisableOrEnableTree(subItem as Node, canHandle);
            }
        }
    }

    public override void _Process(float delta)
    {
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
