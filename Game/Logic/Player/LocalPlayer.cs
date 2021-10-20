using Godot;
using System;
using FPS.Game.Utils;
using System.Collections.Generic;
using FPS.Game.Config;
using FPS.Game.Logic.Player.Handler;
using FPS.Game.Logic.Client;
using FPS.Game.Logic.Weapon;

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



        private List<float> _rotArrayX = new List<float>();
        private List<float> _rotArrayY = new List<float>();

        public override void _Ready()
        {
            base._Ready();
        }


        [Authority]
        public override void onNetworkTeleport(Vector3 origin)
        {
            FPS.Game.Utils.Logger.InfoDraw("Cliend receive teleport on " + origin);
            base.DoTeleport(origin);
        }

        public bool isRecoling = false;

        public float recoilSpeed = 25.0f;
        public float recoilAmount = 0.5f;

        public float ROF = 0.12f;

        public RecoilModes mode = RecoilModes.AUTO;


        public async void startRecoil()
        {
            isRecoling = true;
            var timer = GetTree().CreateTimer(ROF);
            await ToSignal(timer, "timeout");
            this.isRecoling = false;
        }

        public void recoiling(float delta)
        {
            if (isRecoling)
            {
                var headRotation = Mathf.Rad2Deg(this.playerChar.GetHeadRotation());
                var headLerped = Mathf.Lerp(headRotation, headRotation + recoilAmount, recoilSpeed * delta);

                //set head rotation

                var random = new RandomNumberGenerator();
                random.Randomize();

                var charRotation = Mathf.Rad2Deg(this.playerChar.GetCharRotation());
                var lerped = Mathf.Lerp(charRotation, charRotation + random.RandfRange(-0.5f, 0.5f), recoilSpeed * delta);

                this.playerChar.rotateFPSAfterRecoil(lerped, headLerped);
            }
        }

        // Called when the node enters the scene tree for the first time.
        public override void _PhysicsProcess(float delta)
        {
            if (!isActivated)
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

            //do recoil
            if (Input.IsActionPressed("fire") && !isRecoling && mode == RecoilModes.AUTO)
                startRecoil();

            else if (Input.IsActionJustPressed("fire") && !isRecoling && mode == RecoilModes.SINGLE)
                startRecoil();

            //handle recoil
            recoiling(delta);

            //send shot to weapons

            if (Input.IsActionPressed("fire"))
                this.DoFire();
        }


        public override void DoFire()
        {
            base.DoFire();
        }

        Vector2 mouseMotion = Vector2.Zero;

        public override void _Process(float delta)
        {
            base._Process(delta);

            if (!isActivated)
                return;

            if (Input.GetMouseMode() != Input.MouseMode.Captured)
                return;

            if (mouseDelta.Length() > 0)
            {
                var charRotation = mouseDelta.x * ConfigValues.sensitivityX;
                var headRotation = mouseDelta.y * ConfigValues.sensitivityY;

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

            if (!isActivated)
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

            this.playerChar.setCameraMode(PlayerCameraMode.FPS);
            this.playerChar.setDrawMode(PlayerDrawMode.FPS);

            this.isActivated = true;
            Input.SetMouseMode(Input.MouseMode.Captured);

            this.playerChar.DisableHitboxes();
        }

        public void Dectivate()
        {
            this.isActivated = false;
            Input.SetMouseMode(Input.MouseMode.Visible);
            this.playerChar.setCameraMode(PlayerCameraMode.NONE);
            this.playerChar.setDrawMode(PlayerDrawMode.NONE);
        }
    }
}