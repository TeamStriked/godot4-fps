using Godot;
using System;

public partial class BaseWindow : Window
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.FocusEntered += () =>
        {
            GD.Print("Focus"); 
            Input.SetMouseMode(Input.MouseMode.Captured);
        };
        
        this.FocusExited += () =>
        {
            GD.Print("Unfocus");
            Input.SetMouseMode(Input.MouseMode.Visible);
        };
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        // Receives key input
        if (@event is InputEventKey)
        {
            var ev = @event as InputEventKey;
            switch (ev.Keycode)
            {
                case Key.Escape:
                    Input.SetMouseMode(Input.MouseMode.Visible);
                    break;
            }
        }

        @event.Dispose();
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
