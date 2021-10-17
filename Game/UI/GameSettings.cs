using System.Linq;
using System.Threading;
using Godot;
using System;
using FPS.Game.Config;

namespace FPS.Game.UI
{
    public partial class GameSettings : CanvasLayer
    {
        [Export]
        NodePath sensPathX = null;

        [Export]
        NodePath sensPathY = null;

        SpinBox sensX = null;
        SpinBox sensY = null;


        [Export]
        NodePath containerPath = null;

        [Export]
        NodePath keyContainerPath = null;

        [Export]
        NodePath rootContainerPath = null;

        [Export]
        NodePath resChangerPath = null;
        [Export]
        NodePath windowModeChangerPath = null;

        [Export]
        NodePath keyChangeDialogPath = null;

        [Export]
        NodePath closeButtonPath = null;

        TabContainer container = null;
        Control rootContainer = null;

        Button closeButton = null;
        VBoxContainer keyListContainer = null;
        KeyConfirmationDialog keyChangeDialog = null;

        OptionButton resChanger = null;
        OptionButton windowModeChanger = null;
        public bool isOpen = false;

        public void Hide()
        {
            Input.SetMouseMode(Input.MouseMode.Captured);
            this.rootContainer.Visible = false;
            this.SetProcess(false);
            isOpen = false;
        }

        public void Show()
        {
            Input.SetMouseMode(Input.MouseMode.Visible);
            this.SetProcess(true);
            this.rootContainer.Visible = true;
            isOpen = true;
        }

        public override void _Ready()
        {

            this.sensX = GetNode(sensPathX) as SpinBox;
            this.sensY = GetNode(sensPathY) as SpinBox;

            this.sensX.Value = ConfigValues.sensitivityX;
            this.sensY.Value = ConfigValues.sensitivityY;

            this.sensX.ValueChanged += (float val) =>
            {
                ConfigValues.setSensitivityX(val);
            };

            this.sensY.ValueChanged += (float val) =>
            {
                ConfigValues.setSensitivityY(val);
            };

            this.container = GetNode(containerPath) as TabContainer;
            this.closeButton = GetNode(closeButtonPath) as Button;
            this.rootContainer = GetNode(rootContainerPath) as Control;
            this.resChanger = GetNode(resChangerPath) as OptionButton;
            this.windowModeChanger = GetNode(windowModeChangerPath) as OptionButton;

            this.resChanger.ItemSelected += onResChanged;
            this.windowModeChanger.ItemSelected += onFullScreenChanged;

            this.keyListContainer = GetNode(keyContainerPath) as VBoxContainer;
            this.keyChangeDialog = GetNode(keyChangeDialogPath) as KeyConfirmationDialog;
            this.getCurentList();

            this.rootContainer.Visible = false;

            this.closeButton.PressedSignal += () =>
            {
                this.Hide();
            };

            this.keyChangeDialog.Confirmed += () =>
            {
                ConfigValues.storeKey(this.keyChangeDialog.keyName, this.keyChangeDialog.selectedKey);
                var node = this.keyListContainer.GetNode(this.keyChangeDialog.keyName) as GameKeyRecord;
                node.currentKey = this.keyChangeDialog.selectedKey;
            };

            this.SetProcess(false);
        }

        public void onResChanged(int index)
        {
            var values = this.resChanger.Text.Split("x");
            var res = new Vector2i(int.Parse(values[0]), int.Parse(values[1]));
            GetTree().Paused = true;
            GetViewport().Disable3d = true;

            DisplayServer.WindowSetSize(res);
            DisplayServer.WindowSetPosition(Vector2i.Zero);

            GetTree().Paused = false;
            GetViewport().Disable3d = false;
        }
        public void onFullScreenChanged(int index)
        {
            GetTree().Paused = true;
            GetViewport().Disable3d = true;
            GetTree().Root.Mode = index == 0 ? Window.ModeEnum.Windowed : Window.ModeEnum.Fullscreen;

            GetTree().Paused = false;
            GetViewport().Disable3d = false;

        }



        private void getCurentList()
        {
            foreach (var n in ConfigValues.keys.Keys.ToList())
            {
                var value = ConfigValues.keys[n];
                var scene = (PackedScene)ResourceLoader.Load("res://Game/UI/GameKeyRecord.tscn");
                var record = (GameKeyRecord)scene.Instantiate();
                record.currentKey = value;
                record.Name = n;
                record.OnKeyChangeStart += this.onKeyChangeStart;
                this.keyListContainer.AddChild(record);
            }
        }

        private void onKeyChangeStart(string keyName)
        {
            this.keyChangeDialog.openChanger(keyName);
        }
    }
}