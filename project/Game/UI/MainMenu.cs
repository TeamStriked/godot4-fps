using System.Linq;
using System.Threading;
using Godot;
using System;
using FPS.Game.Config;

namespace FPS.Game.UI
{
    public partial class MainMenu : CanvasLayer
    {
        [Export]
        NodePath hostNodePath = null;

        [Export]
        NodePath portNodePath = null;

        private LineEdit hostEdit;
        private LineEdit portEdit;

        [Signal]
        public delegate void onConnect(string host, int port);

        public event onConnect onUpdateConnectEvent;

        public override void _Ready()
        {
            this.hostEdit = GetNode(hostNodePath) as LineEdit;
            this.portEdit = GetNode(portNodePath) as LineEdit;
        }

        public void onConnectButtonPressed()
        {
            onUpdateConnectEvent(this.hostEdit.Text, Int32.Parse(this.portEdit.Text));
        }

        public void Hide()
        {
            (GetNode("AspectRatioContainer") as AspectRatioContainer).Visible = false;
        }

        public void Show()
        {
            Input.SetMouseMode(Input.MouseMode.Visible);
            (GetNode("AspectRatioContainer") as AspectRatioContainer).Visible = true;
        }

    }
}