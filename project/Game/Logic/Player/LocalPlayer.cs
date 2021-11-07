using Godot;
using System;
using FPS.Game.Utils;
using System.Collections.Generic;
using FPS.Game.Config;
using FPS.Game.Logic.Player.Handler;
using FPS.Game.Logic.Client;
using FPS.Game.Logic.Weapon;
using System.Linq;

namespace FPS.Game.Logic.Player
{
    public partial class LocalPlayer : NetworkPlayer
    {
        const float CAMERA_MOUSE_ROTATION_SPEED = 0.001f;

        const int inputFramesToSend = 10;

        public override bool isServerPlayer()
        {
            return false;
        }


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

        private List<InputFrame> sendingInputFrameList = new List<InputFrame>();

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

        public bool showNetworkDebug = false;

        // Called when the node enters the scene tree for the first time.
        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            if (Input.IsActionJustPressed("game_reset_tp"))
            {
                showNetworkDebug = !showNetworkDebug;

                this.GetNode<MeshInstance3D>("ServerFrame").Visible = showNetworkDebug;
                this.GetNode<MeshInstance3D>("ClientFrame").Visible = showNetworkDebug;
            }

            if (!isActivated)
                return;

            if (restoreFrame != null)
            {
                var foundIndex = this.storedInputFrames.FindIndex(df => df.timestamp == restoreFrame.timestamp);
                if (foundIndex < 0)
                {
                    restoreFrame = null;
                    return;
                }

                var foundInputFrame = this.storedInputFrames[foundIndex];

                GD.Print("Start with" + foundInputFrame.timestamp);
                var currentMouseMotion = new Vector2(this.playerChar.GetCharRotation(), this.playerChar.GetHeadRotation());

                //reset to position to the first frame
                this.playerChar.SetCharRotation(foundInputFrame.mouseMotion.x);
                this.playerChar.SetHeadRotation(foundInputFrame.mouseMotion.y);
                this.playerChar.MotionVelocity = restoreFrame.velocity;
                this.DoTeleport(restoreFrame.origin);
                this.playerChar.MoveAndSlide();

                this.storePuppetFrames.Clear();
                this.AppendPuppetFrame(restoreFrame);

                //clear input frames
                this.sendingInputFrameList.Clear();

                if (this.storedInputFrames.Count > 0)
                {
                    var list = this.storedInputFrames.FindAll(df => df.timestamp > restoreFrame.timestamp);
                    var total = list.Count;
                    foreach (var item in list)
                    {
                        GD.Print("Attach " + item.timestamp);

                        this.playerChar.SetCharRotation(item.mouseMotion.x);
                        this.playerChar.SetHeadRotation(item.mouseMotion.y);

                        //create new frame
                        var newCalculatedFrame = this.calulcateFrame(item);
                        this.execFrame(newCalculatedFrame);

                        this.AppendPuppetFrame(this.getCurrentServerFrame(item.timestamp));
                        this.sendingInputFrameList.Add(item);
                    }
                }


                //restore old mouse postion
                this.playerChar.SetCharRotation(currentMouseMotion.x);
                this.playerChar.SetHeadRotation(currentMouseMotion.y);

                restoreFrame = null;

                return;
            }

            if (Input.GetMouseMode() == Input.MouseMode.Captured)
            {
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
            }

            var inputFrame = (this.IsProcessingInput() && Input.GetMouseMode() == Input.MouseMode.Captured)
                ? InputHandler.getInputFrame() : new InputFrame();

            inputFrame.timestamp = Time.GetTicksMsec();
            inputFrame.delta = delta;

            //override input frame with new head postion
            inputFrame.mouseMotion = new Vector2(this.playerChar.GetCharRotation(), this.playerChar.GetHeadRotation());

            //create new frame
            var newFrame = this.calulcateFrame(inputFrame);
            this.execFrame(newFrame);

            this.sendingInputFrameList.Add(inputFrame);

            this.AppendInputFrame(inputFrame);
            this.AppendPuppetFrame(this.getCurrentServerFrame(inputFrame.timestamp));

            this.sendInputFrames();

            //fix godot issue
            handleAnimation();

            //handle recoil
            this.handleRecoil(delta);
        }

        public void sendInputFrames()
        {
            if (this.sendingInputFrameList.Count >= inputFramesToSend)
            {
                //send input frame to server
                var sendMessage = FPS.Game.Utils.NetworkCompressor.Compress(this.sendingInputFrameList);
                RpcId(ClientLogic.serverId, "onClientInput", sendMessage);
                this.sendingInputFrameList.Clear();
            }
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
                if (Input.GetMouseMode() == Input.MouseMode.Captured && restoreFrame == null)
                {
                    var mouseDelta = (@event as InputEventMouseMotion).Relative;

                    var charRotation = mouseDelta.x * ConfigValues.sensitivityX * CAMERA_MOUSE_ROTATION_SPEED;
                    var headRotation = mouseDelta.y * ConfigValues.sensitivityY * CAMERA_MOUSE_ROTATION_SPEED;

                    ApplyMouse(new Vector2(charRotation, headRotation));
                }
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

        public const float inputTreshold = 0.001f;

        public CalculatedServerFrame restoreFrame = null;

        [AnyPeer]
        public override void onServerInput(string inputMessage)
        {
            if (restoreFrame != null)
                return;

            var uncompress = FPS.Game.Utils.NetworkCompressor.Decompress<CalculatedServerFrame>(inputMessage);
            if (uncompress != null)
            {
                var foundIndex = this.storedInputFrames.FindIndex(df => df.timestamp == uncompress.timestamp);
                var foundPuppetIndex = this.storePuppetFrames.FindIndex(df => df.timestamp == uncompress.timestamp);

                if (foundIndex > -1 && foundPuppetIndex > -1)
                {
                    var inputFrame = this.storedInputFrames[foundIndex];
                    var puppetFrame = this.storePuppetFrames[foundPuppetIndex];

                    var diff = puppetFrame.origin - uncompress.origin;
                    FPS.Game.UI.GameGraph.serverPosDifference = diff.Length();

                    var sf = this.GetNode<MeshInstance3D>("ServerFrame");
                    var cf = this.GetNode<MeshInstance3D>("ClientFrame");

                    var sgt = sf.GlobalTransform;
                    sgt.origin = uncompress.origin;
                    sgt.origin.y += 1.0f;
                    sf.GlobalTransform = sgt;

                    var cgt = cf.GlobalTransform;
                    cgt.origin = puppetFrame.origin;
                    cgt.origin.y += 1.0f;
                    cf.GlobalTransform = cgt;

                    if (diff.Length() >= inputTreshold)
                    {
                        //clear puppet Frames
                        FPS.Game.Utils.Logger.InfoDraw("[Client] Restore from input lag " + diff.Length());
                        restoreFrame = uncompress;
                    }
                }
                else
                {
                    GD.Print("Cant find timestamp!");
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