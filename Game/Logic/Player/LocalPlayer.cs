using Godot;
using System;
using FPS.Game.Utils;
using System.Collections.Generic;
using FPS.Game.Config;
using FPS.Game.Logic.Player.Handler;
using FPS.Game.Logic.Client;

namespace FPS.Game.Logic.Player
{
    public partial class LocalPlayer : NetworkPlayer
    {
        public override bool isServerPlayer()
        {
            return false;
        }

        const int _averageFromThisManySteps = 3;

        //vec
        Vector2 mouseDelta = new Vector2();

        // How precise the controller can change direction while not grounded


        // How fast the controller decelerates on the grounded

        private bool isThirdPerson = false;

        public bool canHandleInput = true;


        private List<float> _rotArrayX = new List<float>();
        private List<float> _rotArrayY = new List<float>();


        [Authority]
        public override void onNetworkTeleport(Vector3 origin)
        {
            GD.Print("Cliend receive teleport on " + origin);
            base.DoTeleport(origin);
        }

        // Called when the node enters the scene tree for the first time.
        public override void _PhysicsProcess(float delta)
        {
            if (!canHandleInput)
                return;

            if (Input.GetMouseMode() != Input.MouseMode.Captured)
                return;

            if (Input.IsActionJustPressed("game_camera_switch") && this.IsProcessingInput())
            {
                isThirdPerson = !isThirdPerson;

                if (isThirdPerson)
                {
                    this.playerChar.setCameraMode(PlayerCameraMode.TPS);
                    this.playerChar.setDrawMode(PlayerDrawMode.TPS);
                }
                else
                {
                    this.playerChar.setCameraMode(PlayerCameraMode.FPS);
                    this.playerChar.setDrawMode(PlayerDrawMode.FPS);
                }
            }
            var inputFrame = (this.IsProcessingInput()) ? InputHandler.getInputFrame() : new InputFrame();
            var newFrame = this.calulcateFrame(inputFrame, delta);
            this.execFrame(newFrame);
            lastFrame = newFrame;

            inputFrame.mouseMotion = new Vector2(this.playerChar.GetCharRotation(), this.playerChar.GetHeadRotation());

            //send input frame to server
            var sendMessage = FPS.Game.Utils.NetworkCompressor.Compress(inputFrame);

            RpcId(ClientLogic.serverId, "onClientInput", sendMessage);

            //fix godot issue
            handleAnimation();
        }

        Vector2 mouseMotion = Vector2.Zero;

        /**
         * TODO: REPLACE WITH https://github.com/joelpt/onclick5/blob/master/Assets/SimpleSmoothMouseLook.cs*
         *
         */
        public override void _Process(float delta)
        {
            base._Process(delta);

            if (!canHandleInput)
                return;

            if (Input.GetMouseMode() != Input.MouseMode.Captured)
                return;

            if (mouseDelta.Length() > 0)
            {
                var charRotation = mouseDelta.x * ConfigValues.sensitivityX * delta;
                var headRotation = mouseDelta.y * ConfigValues.sensitivityY * delta;

                ApplyMouse(new Vector2(charRotation, headRotation));
            }

            mouseDelta = Vector2.Zero;

            if (!playerChar.IsOnFloor())
            {
                this.playerChar.setCameraZoom(false);
            }
            else
            {
                this.playerChar.setCameraZoom(rightMouseActivated);
            }
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if (!canHandleInput)
            {
                @event.Dispose();
                return;
            }

            if (@event is InputEventMouseMotion)
            {
                mouseDelta = (@event as InputEventMouseMotion).Relative;
            }

            if (@event is InputEventMouseButton)
            {
                if ((@event as InputEventMouseButton).ButtonIndex == MouseButton.Right)
                {
                    rightMouseActivated = (@event as InputEventMouseButton).Pressed;
                }
            }

            @event.Dispose();
        }
        bool rightMouseActivated = false;

        public override void Activate()
        {
            this.canHandleInput = true;
            Input.SetMouseMode(Input.MouseMode.Captured);

            this.playerChar.setCameraMode(PlayerCameraMode.FPS);
            this.playerChar.setDrawMode(PlayerDrawMode.FPS);
        }

        public void Dectivate()
        {
            this.canHandleInput = false;
            Input.SetMouseMode(Input.MouseMode.Visible);
            this.playerChar.setCameraMode(PlayerCameraMode.NONE);
            this.playerChar.setDrawMode(PlayerDrawMode.NONE);
        }
    }
}