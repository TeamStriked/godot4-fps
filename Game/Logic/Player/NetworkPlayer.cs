using Godot;
using System;
using FPS.Game.Logic.Player.Handler;

namespace FPS.Game.Logic.Player
{
    public abstract partial class NetworkPlayer : Node3D
    {
        protected CalculatedFrame lastFrame = new CalculatedFrame();

        private float currentJumpTime = 0.0f;

        public abstract bool isServerPlayer();

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            if (Input.IsActionJustPressed("game_reset_tp") && this.IsProcessingInput())
            {
                this.DoTeleport(this.lastTeleportOrigin);
            }
        }

        public void execFrame(CalculatedFrame frame)
        {
            playerChar.MotionVelocity = frame.velocity;
            playerChar.MoveAndSlide();
        }

        public void ApplyMouse(Vector2 mouseMotion)
        {
            if (mouseMotion.Length() > 0)
            {
                this.playerChar.rotateFPS(mouseMotion.x, mouseMotion.y);
                this.playerChar.rotateTPSCamera(mouseMotion.y);
            }
        }

        public CalculatedFrame calulcateFrame(InputFrame inputFrame, float delta)
        {
            var calculatedFrame = new CalculatedFrame();
            var currentSpeed = playerChar.MotionVelocity.Length();
            calculatedFrame.velocity = playerChar.MotionVelocity;
            calculatedFrame.direction = inputFrame.direction;

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

                Vector3 prevMove = new Vector3(playerChar.MotionVelocity.x, 0, playerChar.MotionVelocity.z);


                Vector3 nextMove = AirAccelerate(relativeDir, prevMove, wishSpeed, Accel, delta);
                nextMove.y = playerChar.MotionVelocity.y;
                nextMove.y -= gravity * delta;

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

                            currentSpeedAmount = Mathf.Clamp(Mathf.Lerp(currentSpeedAmount, 0, speedLooseMultiplier * speedLooseMultiplier * delta), 0, 1.0f);
                            var offset = sprintSpeed - defaultSpeed;
                            moveSpeed = defaultSpeed + (offset * currentSpeedAmount);
                        }
                        else if (!inputFrame.onSprinting)
                        {
                            calculatedFrame.sprinting = false;
                            currentSpeedAmount = Mathf.Clamp(Mathf.Lerp(currentSpeedAmount, 1.0f, speedRechargeMultiplier * delta), 0.0f, 1.0f);
                        }
                    }
                }

                //add speed
                relativeDir.x *= moveSpeed;
                relativeDir.y = 0;
                relativeDir.z *= moveSpeed;

                // jumping
                if (inputFrame.onJumpStart && currentJumpTime <= 0.0f && lastFrame.jumpLocked == false && inputFrame.onProne == false)
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

                this.setPlayerCollider(calculatedFrame, delta);

                ApplyFriction(ref relativeDir, delta);
                var _accel = Accel;
                var _deaccel = Deaccel;


                if (input.y == 0 && input.x != 0)
                {
                    accel = sideStrafeAcceleration;
                    deaccel = sideStrafeAcceleration;
                }

                Accelerate(ref calculatedFrame.velocity, relativeDir, _accel, _deaccel, delta);
            }

            currentJumpTime = Mathf.Clamp(currentJumpTime - delta, 0, jumpCoolDown);
            return calculatedFrame;
        }


        [AnyPeer]
        public virtual void onClientInput(string inputMessage)
        {
        }

        public override void _EnterTree()
        {
            base._EnterTree();

            RpcConfig("onNetworkTeleport", RPCMode.Auth, false, TransferMode.Reliable);
            RpcConfig("onPuppetUpdate", RPCMode.Auth, false, TransferMode.Reliable);
            RpcConfig("onClientInput", RPCMode.AnyPeer, false, TransferMode.Unreliable);

            var scene = (PackedScene)ResourceLoader.Load("res://Game/Logic/Player/CharacterInstance.tscn");
            scene.ResourceLocalToScene = true;
            var character = (CharacterInstance)scene.Instantiate();
            this.AddChild(character);
            this.playerChar = character;
        }

        protected void handleAnimation()
        {
            var calculatedFrame = this.lastFrame;

            if (this.playerChar.getSpeed() > this.defaultSpeed)
            {
                var scale = this.playerChar.getSpeed() / this.sprintSpeed;

                this.playerChar.setAnimationState((calculatedFrame.direction.y < 0) ? "run" : "run_back");
                this.playerChar.setAnimationTimeScale(scale);
            }
            else if (this.playerChar.getSpeed() > 1.0f)
            {
                var scale = this.playerChar.getSpeed() / this.walkSpeed;

                if (calculatedFrame.crouching)
                {
                    this.playerChar.setAnimationState((calculatedFrame.direction.y < 0) ? "shift_crouch" : "shift_crouch_back");
                }
                else
                {
                    this.playerChar.setAnimationState((calculatedFrame.direction.y < 0) ? "shift" : "shift_back");
                }

                this.playerChar.setAnimationTimeScale(scale);
            }
            else
            {
                this.playerChar.setAnimationTimeScale(1.0f);

                if (calculatedFrame.crouching)
                {
                    this.playerChar.setAnimationState("crouch_idle");
                }
                else
                {
                    this.playerChar.setAnimationState("idle");
                }
            }
        }

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
            //change prone to steps by collider shape height (> 0.6, > 0.3)
            var upSpeed = crouchUpSpeed;
            var downSpeed = crouchDownSpeed;

            if (this.playerChar.getShapeDivider() < this.crouchColliderMultiplier)
            {
                upSpeed = proneUpSpeed;
                downSpeed = proneDownSpeed;
            }

            if (calculatedFrame.prone)
            {
                this.playerChar.setBodyHeight(delta, upSpeed, downSpeed, proneColliderMultiplier, calculatedFrame.prone);
            }
            else
            {
                this.playerChar.setBodyHeight(delta, upSpeed, downSpeed, crouchColliderMultiplier, calculatedFrame.crouching);
            }
        }

        public override void _Ready()
        {
        }


        [Export]
        protected float crouchUpSpeed = 12;

        [Export]
        protected float crouchDownSpeed = 6;


        [Export]
        protected float crouchColliderMultiplier = 0.6f;

        [Export]
        protected float proneColliderMultiplier = 0.3f;


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

        [Export]
        protected float jumpForce = 8.5f;

        [Export]
        protected float jumpCrouchForce = 10.0f;

        [Export]
        protected float jumpCoolDown = 0.65f;

        [Export]
        protected float gravity = 21.0f;




        protected CharacterInstance playerChar = null;

        public int networkId = 0;


        [Authority]
        public virtual void onNetworkTeleport(Vector3 origin)
        {
        }

        [Authority]
        public virtual void onPuppetUpdate(string message)
        {
        }

        protected Vector3 lastTeleportOrigin;

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