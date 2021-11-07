using Godot;
using System;
using System.Linq;
using FPS.Game.Logic.Player.Handler;
using FPS.Game.Logic.World;

using System.Collections.Generic;
namespace FPS.Game.Logic.Player
{
    public abstract partial class NetworkPlayer : Node3D
    {
        const int maxStoragelFrames = 1024;
        protected List<InputFrame> storedInputFrames = new List<InputFrame>();
        protected List<CalculatedServerFrame> storePuppetFrames = new List<CalculatedServerFrame>();

        public void AppendInputFrame(InputFrame frame)
        {
            this.storedInputFrames.Add(frame);
            if (this.storedInputFrames.Count > maxStoragelFrames)
            {
                var rest = this.storedInputFrames.Count - maxStoragelFrames;
                this.storedInputFrames.RemoveRange(0, rest);
            }
        }
        public void AppendPuppetFrame(CalculatedServerFrame frame)
        {
            this.storePuppetFrames.Add(frame);
            if (this.storePuppetFrames.Count > maxStoragelFrames)
            {
                var rest = this.storePuppetFrames.Count - maxStoragelFrames;
                this.storePuppetFrames.RemoveRange(0, rest);
            }
        }

        public GameWorld world = null;

        private float currentJumpTime = 0.0f;

        public abstract bool isServerPlayer();
        public abstract void Activate();

        public bool isActivated = false;


        protected float crouchDownSpeed = 8;
        protected float crouchUpSpeed = 20;


        protected float crouchColliderMultiplier = 1.5f;

        protected float proneColliderMultiplier = 1.3f;


        [Export]
        protected float proneUpSpeed = 1.8f;

        [Export]
        protected float proneDownSpeed = 1.3f;


        [Export]
        protected float Friction = 6;

        [Export] protected float FrictionSpeedThreshold = 0.5f;

        [Export]
        protected float maxSpeedAir = 1.3f;

        [Export]
        protected float sprintSpeed = 12f;

        [Export]
        protected float speedRechargeMultiplier = 2f;

        [Export]
        protected float speedLooseMultiplier = 0.3f;

        [Export]
        protected float currentSpeedAmount = 1.0f;

        protected float sideStrafeAcceleration = 50.0f;

        protected float walkSpeed = 3.6f;

        protected float proneSpeed = 1.4f;

        [Export]
        protected float defaultSpeed = 6.5f;

        [Export]
        protected float Accel = 14.0f;

        [Export]
        protected float Deaccel = 10.0f; //start speed

        [Export]
        protected float FlyAccel = 2.0f; // stop speed

        public float sideStrafeSpeed = 1.0f;

        protected float jumpForce = 15.0f;

        protected float jumpCrouchForce = 17.5f;

        [Export]
        protected float jumpCoolDown = 0.65f;

        protected float gravity = 36.0f;


        protected CharacterInstance playerChar = null;

        public int networkId = 0;

        protected Vector3 lastTeleportOrigin;

        public virtual void DoFire(Weapon.Weapon weapon)
        {

        }

        [AnyPeer]
        public virtual void onClientInput(string inputMessage)
        {
        }


        [AnyPeer]
        public virtual void onServerInput(string inputMessage)
        {
        }



        public void execFrame(CalculatedFrame frame)
        {
            playerChar.MotionVelocity = frame.velocity;
            playerChar.MoveAndSlide();

            this.lastExecuteFrame = frame;
        }

        public CalculatedFrame lastExecuteFrame = null;

        public void ApplyMouse(Vector2 mouseMotion)
        {
            if (mouseMotion.Length() > 0)
            {
                this.playerChar.rotateFPS(mouseMotion.x, mouseMotion.y);
                this.playerChar.rotateTPSCamera(mouseMotion.y);
            }
        }

        public CalculatedFrame calulcateFrame(InputFrame inputFrame)
        {
            var weapon = this.playerChar.GetCurrentWeapon();
            if (weapon != null)
            {
                if (inputFrame.onShoot)
                {
                    if (weapon.CanShoot())
                    {
                        //send shot to weapons
                        this.DoFire(weapon);
                    }
                }

                weapon.ProcessWeapon(inputFrame.delta);
            }

            var calculatedFrame = new CalculatedFrame();
            var currentSpeed = playerChar.MotionVelocity.Length();
            calculatedFrame.velocity = playerChar.MotionVelocity;
            calculatedFrame.direction = inputFrame.direction;
            calculatedFrame.timestamp = inputFrame.timestamp;
            calculatedFrame.mouseMotion = inputFrame.mouseMotion;
            calculatedFrame.onZoom = inputFrame.onZoom;

            var input = inputFrame.direction.Normalized();

            // get the forward and right directions
            var forward = this.playerChar.Transform.basis.z;
            var right = this.playerChar.Transform.basis.x;
            var relativeDir = (forward * input.y + right * input.x);
            float wishSpeed = relativeDir.Length();

            // apply gravity
            if (!playerChar.IsOnFloor())
            {
                calculatedFrame.crouching = inputFrame.onCrouching;
                calculatedFrame.prone = false;
                calculatedFrame.shifting = false;
                calculatedFrame.onZoom = false;

                Vector3 prevMove = new Vector3(playerChar.MotionVelocity.x, 0, playerChar.MotionVelocity.z);


                Vector3 nextMove = AirAccelerate(relativeDir, prevMove, wishSpeed, Accel, inputFrame.delta);
                nextMove.y = playerChar.MotionVelocity.y;
                nextMove.y -= gravity * inputFrame.delta;

                //add gravity
                calculatedFrame.velocity = nextMove;
            }
            else
            {
                var moveSpeed = defaultSpeed;
                var accel = Accel;
                var deaccel = Deaccel;

                if (inputFrame.onProne)
                {
                    moveSpeed = proneSpeed;

                    calculatedFrame.crouching = false;
                    calculatedFrame.shifting = false;
                    calculatedFrame.sprinting = false;
                }
                else
                {
                    if (inputFrame.onShifting || inputFrame.onCrouching)
                    {
                        moveSpeed = walkSpeed;
                        calculatedFrame.shifting = inputFrame.onShifting;
                        calculatedFrame.crouching = inputFrame.onCrouching;
                        calculatedFrame.sprinting = false;
                    }
                    else
                    {
                        calculatedFrame.shifting = false;

                        if (inputFrame.onSprinting && this.currentSpeedAmount > 0.1 && inputFrame.direction.y != 0)
                        {
                            calculatedFrame.crouching = false;
                            calculatedFrame.prone = false;
                            calculatedFrame.sprinting = true;

                            currentSpeedAmount = Mathf.Clamp(Mathf.Lerp(currentSpeedAmount, 0, speedLooseMultiplier * speedLooseMultiplier * inputFrame.delta), 0, 1.0f);
                            var offset = sprintSpeed - defaultSpeed;
                            moveSpeed = defaultSpeed + (offset * currentSpeedAmount);
                        }
                        else if (!inputFrame.onSprinting)
                        {
                            calculatedFrame.sprinting = false;
                            currentSpeedAmount = Mathf.Clamp(Mathf.Lerp(currentSpeedAmount, 1.0f, speedRechargeMultiplier * inputFrame.delta), 0.0f, 1.0f);
                        }
                    }
                }

                //add speed
                relativeDir.x *= moveSpeed;
                relativeDir.y = 0;
                relativeDir.z *= moveSpeed;

                // jumping
                if (inputFrame.onJumpStart && currentJumpTime <= 0.0f && (lastExecuteFrame == null || lastExecuteFrame.jumpLocked == false) && inputFrame.onProne == false)
                {
                    currentJumpTime = jumpCoolDown;

                    if (!inputFrame.onCrouching)
                        calculatedFrame.velocity.y = jumpForce;
                    else
                    {
                        calculatedFrame.velocity.y = jumpCrouchForce;
                    }

                    calculatedFrame.crouching = false;
                    calculatedFrame.shifting = false;
                    calculatedFrame.sprinting = false;
                    calculatedFrame.jumpLocked = true;
                }
                else if (!inputFrame.onJumpStart)
                {
                    calculatedFrame.jumpLocked = false;
                }

                this.setPlayerCollider(calculatedFrame, inputFrame.delta);
                ApplyFriction(ref relativeDir, inputFrame.delta);

                var _accel = Accel;
                var _deaccel = Deaccel;

                if (input.y == 0 && input.x != 0)
                {
                    accel = sideStrafeAcceleration;
                    deaccel = sideStrafeAcceleration;
                }

                Accelerate(ref calculatedFrame.velocity, relativeDir, _accel, _deaccel, inputFrame.delta);
            }

            currentJumpTime = Mathf.Clamp(currentJumpTime - inputFrame.delta, 0, jumpCoolDown);
            return calculatedFrame;
        }

        public CalculatedServerFrame getCurrentServerFrame(ulong timestamp)
        {
            var clientFrame = new CalculatedServerFrame();
            clientFrame.timestamp = timestamp;
            clientFrame.origin = this.playerChar.GlobalTransform.origin;
            clientFrame.rotation = this.playerChar.GlobalTransform.basis.GetEuler();
            clientFrame.velocity = this.playerChar.MotionVelocity;
            clientFrame.currentAnimation = this.playerChar.getAnimationState();
            clientFrame.currentAnimationTime = this.playerChar.getAnimationScale();

            return clientFrame;
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            this.playerChar = GetNode("char") as CharacterInstance;
            RpcConfig("onClientInput", RPCMode.AnyPeer, false, TransferMode.Unreliable);
            RpcConfig("onServerInput", RPCMode.Auth, false, TransferMode.Reliable);
        }

        protected void handleAnimation()
        {
            if (lastExecuteFrame == null)
                return;

            var walkForward = lastExecuteFrame.direction.y * -1;  //up = 1
            var walkLeft = lastExecuteFrame.direction.x; // -1 == left

            this.playerChar.setAnimationMovement(new Vector2(walkLeft, walkForward));
            this.playerChar.setAnimationState(lastExecuteFrame.onZoom ? 1 : 0);
            var scale = this.playerChar.getSpeed() / this.walkSpeed;
            this.playerChar.setAnimationTimeScale(scale * 2.0f);
            this.playerChar.setAnimationAim(lastExecuteFrame.mouseMotion);



            if (this.playerChar.IsOnFloor())
            {
                this.playerChar.setAnimationFlyScale(0.0f);
            }
            else
            {
                this.playerChar.setAnimationFlyScale(1.0f);
            }

            this.doFootsteps();
        }

        public bool lastJumpState = false;

        public void doFootsteps()
        {
            if (this.playerChar.IsOnFloor())
            {
                if (this.playerChar.getSpeed() > this.walkSpeed && this.nextStepSound <= 0.0f)
                {
                    this.playerChar.doFootstep((this.playerChar.getSpeed() > this.defaultSpeed));
                    this.nextStepSound = 1.0f;
                }
                else if (this.playerChar.getSpeed() > 0.0f)
                {
                    var nextStepReduce = (float)this.GetPhysicsProcessDeltaTime() * (this.playerChar.getSpeed() / this.sprintSpeed) * betweenStepMultiplier;
                    this.nextStepSound -= nextStepReduce;
                }
                else
                {
                    this.nextStepSound = 0.0f;
                }
            }
            else
            {
                this.nextStepSound = 0.0f;
            }

            if (lastJumpState == false && this.playerChar.IsOnFloor())
            {
                this.playerChar.playLandingSound();
            }

            lastJumpState = this.playerChar.IsOnFloor();
        }

        public float nextStepSound = 0.0f;

        public const float betweenStepMultiplier = 3.70f;

        public void currentAnimation()
        {

        }

        protected Vector3 AirAccelerate(Vector3 wishDir, Vector3 prevVelocity, float wishSpeed, float accel, float delta)
        {
            if (wishSpeed > this.maxSpeedAir)
                wishSpeed = this.maxSpeedAir;

            float currentSpeed = prevVelocity.Dot(wishDir);
            float addSpeed = wishSpeed - currentSpeed;

            if (addSpeed <= 0)
                return prevVelocity;

            float accelSpeed = accel * wishSpeed * delta;

            if (accelSpeed > addSpeed)
                accelSpeed = addSpeed;

            return prevVelocity + wishDir * accelSpeed;
        }



        protected private void Accelerate(ref Vector3 playerVelocity, Vector3 direction, float accel, float deaccel, float dt)
        {
            var _accel = Accel;
            if (direction.Dot(playerVelocity) > 0)
                _accel = Deaccel;

            playerVelocity = playerVelocity.Lerp(direction, _accel * dt);
        }

        protected void ApplyFriction(ref Vector3 playerVelocity, float dt)
        {
            var speed = playerVelocity.Length();
            if (speed <= 0.00001)
            {
                return;
            }

            var downLimit = Mathf.Max(speed, FrictionSpeedThreshold); // Don't drop below treshold
            var dropAmount = speed - (downLimit * Friction * dt);
            if (dropAmount < 0)
            {
                dropAmount = 0;
            }

            playerVelocity *= dropAmount / speed; // Reduce the velocity by a certain percent
        }

        protected void setPlayerCollider(CalculatedFrame calculatedFrame, float delta)
        {

            if (calculatedFrame.prone)
            {
                this.playerChar.setBodyHeight(delta, crouchDownSpeed, proneColliderMultiplier, calculatedFrame.prone);
            }
            else
            {
                this.playerChar.setBodyHeight(delta, calculatedFrame.crouching ? crouchDownSpeed : crouchUpSpeed, crouchColliderMultiplier, calculatedFrame.crouching);
            }
        }

        public virtual void DoTeleport(Vector3 origin)
        {
            this.lastTeleportOrigin = origin;

            var gf = this.playerChar.GlobalTransform;
            gf.origin = origin;
            this.playerChar.GlobalTransform = gf;
        }
        public virtual void DoRotate(Vector3 origin)
        {
            this.lastTeleportOrigin = origin;

            var gf = this.playerChar.GlobalTransform;
            gf.basis = new Basis(origin);
            this.playerChar.GlobalTransform = gf;
        }

    }
}