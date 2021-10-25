using System.Linq;
using System.Threading;
using Godot;
using System;
using FPS.Game.Config;

namespace FPS.Game.UI
{
    public partial class GameSettings : CanvasLayer
    {
        public delegate void DisconnectEvent();
        public event DisconnectEvent OnDisconnect;

        [Export]
        NodePath sensPathX = null;

        [Export]
        NodePath sensPathY = null;

        SpinBox sensX = null;
        SpinBox sensY = null;


        [Export]
        NodePath disconnectPath = null;

        [Export]
        NodePath containerPath = null;
        [Export]
        NodePath soundVolumePath = null;

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
        Button disconnectButton = null;
        VBoxContainer keyListContainer = null;
        KeyConfirmationDialog keyChangeDialog = null;

        OptionButton resChanger = null;
        OptionButton windowModeChanger = null;
        public bool isOpen = false;

        HSlider volumeSlider;

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
            this.disconnectButton = GetNode(disconnectPath) as Button;
            this.rootContainer = GetNode(rootContainerPath) as Control;
            this.resChanger = GetNode(resChangerPath) as OptionButton;
            this.windowModeChanger = GetNode(windowModeChangerPath) as OptionButton;
            this.volumeSlider = GetNode(soundVolumePath) as HSlider;
            this.resChanger.ItemSelected += onResChanged;
            this.windowModeChanger.ItemSelected += onFullScreenChanged;

            this.keyListContainer = GetNode(keyContainerPath) as VBoxContainer;
            this.keyChangeDialog = GetNode(keyChangeDialogPath) as KeyConfirmationDialog;
            this.getCurentList();

            this.rootContainer.Visible = false;

            this.disconnectButton.PressedSignal += () =>
            {
                OnDisconnect();
            };

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

            var bus = AudioServer.GetBusIndex("Master");
            this.volumeSlider.Value = db2linear(AudioServer.GetBusVolumeDb(bus));
            this.volumeSlider.ValueChanged += (float value) =>
            {
                AudioServer.SetBusVolumeDb(bus, linear2db(value));
            };
        }

        float db2linear(float p_db) { return Mathf.Exp(p_db * 0.11512925464970228420089957273422f); }
        float linear2db(float p_linear) { return Mathf.Log(p_linear) * 8.6858896380650365530225783783321f; }


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