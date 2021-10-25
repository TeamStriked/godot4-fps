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
        const int inputFramesToSend = 10;

        public override bool isServerPlayer()
        {
            return false;
        }

        //vec
        Vector2 mouseDelta = new Vector2();

        // How precise the controller can change direction while not grounded


        // How fast the controller decelerates on the grounded

        private bool isThirdPerson = false;



        private List<float> _rotArrayX = new List<float>();
        private List<float> _rotArrayY = new List<float>();


        public bool isRecoling = false;

        public float recoilSpeed = 25.0f;
        public float recoilAmount = 0.5f;

        public float ROF = 0.12f;

        Vector2 mouseMotion = Vector2.Zero;

        public RecoilModes mode = RecoilModes.AUTO;

        private List<InputFrame> inputFrames = new List<InputFrame>();

        private async void startRecoil()
        {
            isRecoling = true;
            var timer = GetTree().CreateTimer(ROF);
            await ToSignal(timer, "timeout");
            this.isRecoling = false;
        }

        private void handleRecoil(float delta)
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

            base._PhysicsProcess(delta);

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

            //create new frame
            var newFrame = this.calulcateFrame(inputFrame, delta);
            this.execFrame(newFrame);
            this.AppendCalculatedFrame(newFrame);

            //override input frame with new head postion
            inputFrame.mouseMotion = new Vector2(this.playerChar.GetCharRotation(), this.playerChar.GetHeadRotation());

            this.inputFrames.Add(inputFrame);

            if (this.inputFrames.Count >= inputFramesToSend)
            {
                //send input frame to server
                var sendMessage = FPS.Game.Utils.NetworkCompressor.Compress(this.inputFrames);
                RpcId(ClientLogic.serverId, "onClientInput", sendMessage);
                this.inputFrames.Clear();
            }

            //fix godot issue
            handleAnimation();

            //handle recoil
            this.handleRecoil(delta);
        }

        public override void DoFire(Weapon.Weapon weapon)
        {
            weapon.FireGun();
            weapon.GunEffects();

            //do recoil
            if (!isRecoling && mode == RecoilModes.AUTO)
                this.startRecoil();

            this.playerChar.setCameraShake();
        }

        public override void _Process(float delta)
        {
            if (!isActivated)
                return;

            base._Process(delta);

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

        [AnyPeer]
        public override void onServerInput(string inputMessage)
        {
            var uncompress = FPS.Game.Utils.NetworkCompressor.Decompress<CalculatedPuppetFrame>(inputMessage);
            if (uncompress != null)
            {
                var diff = this.playerChar.GlobalTransform.origin - uncompress.origin;
                if (diff.Length() >= 1.0f)
                {
                    FPS.Game.Utils.Logger.InfoDraw("[Client] input lag size " + diff.Length());
                    this.DoTeleport(uncompress.origin);
                }
            }
        }

        bool rightMouseActivated = false;

        public override void Activate()
        {
            this.playerChar.ProcessMode = ProcessModeEnum.Always;

            this.playerChar.setCameraMode(PlayerCameraMode.FPS);
            this.playerChar.setDrawMode(PlayerDrawMode.FPS);

            this.isActivated = true;
            Input.SetMouseMode(Input.MouseMode.Captured);
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