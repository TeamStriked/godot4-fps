using System.Threading;
using Godot;
using System;
using FPS.Game.Config;

namespace FPS.Game.UI
{


    public partial class KeyConfirmationDialog : ConfirmationDialog
    {
        public Key selectedKey;
        public string keyName;
        public void openChanger(string keyName)
        {
            this.keyName = keyName;
            this.DialogText = "Press a key to continue...";
            this.PopupCentered();
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            // Receives key input
            if (@event is InputEventKey)
            {
                var ev = @event as InputEventKey;
                selectedKey = ev.Keycode;
                this.DialogText = "Current selected key is " + ev.Keycode.ToString() + ". Please press apply to confirm.";
            }

            @event.Dispose();
        }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            GetOkButton().FocusMode = Control.FocusModeEnum.None;
            GetCancelButton().FocusMode = Control.FocusModeEnum.None;

        }
    }
}