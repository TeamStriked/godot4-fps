using Godot;
using System;
using FPS.Game.Utils;
using System.Collections.Generic;
using FPS.Game.Config;
namespace FPS.Game.Logic.Player
{
    public partial class LocalPlayer : NetworkPlayer
    {
        //cam look
        const float minLookAngleY = -90.0f;
        const float maxLookAngleY = 90.0f;

        const float minLookAngleX = -360.0f;
        const float maxLookAngleX = 360.0f;
        const int _averageFromThisManySteps = 3;

        //vec
        Vector2 mouseDelta = new Vector2();

        private float currentJumpTime = 0.0f;

        // How precise the controller can change direction while not grounded


        // How fast the controller decelerates on the grounded

        private bool isThirdPerson = false;


        private bool onCrouching = false;
        private bool onProne = false;
        private bool onSprint = false;
        private bool onShifting = false;

        private Vector2 direction = Vector2.Zero;

        public bool canHandleInput = true;


        private List<float> _rotArrayX = new List<float>();
        private List<float> _rotArrayY = new List<float>();

        private float rotationX = 0F;
        private float rotationY = 0F;

        private bool jumpLocked = false;

        [Puppet]
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

            var currentSpeed = playerChar.LinearVelocity.Length();

            var vel = playerChar.LinearVelocity;

            // reset the x and z velocity
            var input = new Vector2();

            //movement inputs
            if (Input.IsActionPressed("game_moveForward"))
                input.y -= 1f;
            if (Input.IsActionPressed("game_moveBackward"))
                input.y += 1f;

            if (Input.IsActionPressed("game_moveLeft"))
                input.x -= 1f;
            if (Input.IsActionPressed("game_moveRight"))
                input.x += 1f;

            this.direction = input;

            if (Input.IsActionPressed("game_crouching"))
                onCrouching = true;
            else
                onCrouching = false;

            if (Input.IsActionPressed("game_prone"))
                onProne = true;
            else
                onProne = false;

            if (Input.IsActionJustPressed("game_camera_switch"))
            {
                isThirdPerson = !isThirdPerson;

                if (isThirdPerson)
                {
                    this.playerChar.setCameraMode(PlayerCameraMode.TPS);
                }
                else
                {
                    this.playerChar.setCameraMode(PlayerCameraMode.FPS);
                }
            }

            input = input.Normalized();

            // get the forward and right directions
            var forward = this.playerChar.Transform.basis.z;
            var right = this.playerChar.Transform.basis.x;
            var relativeDir = (forward * input.y + right * input.x);
            float wishSpeed = relativeDir.Length();

            // apply gravity
            if (!playerChar.IsOnFloor())
            {
                onCrouching = false;
                onProne = false;
                onShifting = false;

                Vector3 prevMove = new Vector3(playerChar.LinearVelocity.x, 0, playerChar.LinearVelocity.z);
                Vector3 nextMove = AirAccelerate(relativeDir, prevMove, wishSpeed, Accel, delta);
                nextMove.y = playerChar.LinearVelocity.y;
                nextMove.y -= gravity * delta;

                //add gravity
                vel = nextMove;
            }
            else
            {
                var moveSpeed = defaultSpeed;
                var accel = Accel;
                var deaccel = Deaccel;

                if (onProne)
                {
                    moveSpeed = proneSpeed;
                    onCrouching = false;
                }
                else
                {
                    if (Input.IsActionPressed("game_shifting") || onCrouching)
                    {
                        moveSpeed = walkSpeed;
                        onShifting = true;

                    }
                    else
                    {
                        onShifting = false;

                        if (Input.IsActionPressed("game_sprinting") && this.currentSpeedAmount > 0.1 && input.y != 0)
                        {
                            onCrouching = false;
                            onProne = false;
                            onSprint = true;

                            currentSpeedAmount = Mathf.Clamp(Mathf.Lerp(currentSpeedAmount, 0, speedLooseMultiplier * speedLooseMultiplier * delta), 0, 1.0f);
                            var offset = sprintSpeed - defaultSpeed;
                            moveSpeed = defaultSpeed + (offset * currentSpeedAmount);
                        }
                        else if (!Input.IsActionPressed("game_sprinting"))
                        {
                            onSprint = false;
                            currentSpeedAmount = Mathf.Clamp(Mathf.Lerp(currentSpeedAmount, 1.0f, speedRechargeMultiplier * delta), 0.0f, 1.0f);
                        }
                    }
                }

                //add speed
                relativeDir.x *= moveSpeed;
                relativeDir.y = 0;
                relativeDir.z *= moveSpeed;

                // jumping
                if (Input.IsActionPressed("game_jumpUp") && currentJumpTime <= 0.0f && !jumpLocked && !onProne)
                {
                    currentJumpTime = jumpCoolDown;

                    if (!onCrouching)
                        vel.y = jumpForce;
                    else
                    {
                        vel.y = jumpCrouchForce;
                    }

                    onCrouching = false;
                    onShifting = false;
                    onSprint = false;

                    jumpLocked = true;
                }
                else if (!Input.IsActionPressed("game_jumpUp"))
                {
                    jumpLocked = false;
                }

                ApplyFriction(ref relativeDir, delta);
                Accelerate(ref vel, relativeDir, Accel, Deaccel, delta);
            }

            playerChar.LinearVelocity = vel;



            playerChar.MoveAndSlide();

            //fix godot issue
            currentJumpTime = Mathf.Clamp(currentJumpTime - delta, 0, jumpCoolDown);

            handleAnimation();
        }

        private void handleAnimation()
        {
            if (this.playerChar.getSpeed() > this.defaultSpeed)
            {
                var scale = this.playerChar.getSpeed() / this.sprintSpeed;

                this.playerChar.setAnimationState((this.direction.y < 0) ? "run" : "run_back");
                this.playerChar.setAnimationTimeScale(scale);
            }
            else if (this.playerChar.getSpeed() > 1.0f)
            {
                var scale = this.playerChar.getSpeed() / this.walkSpeed;

                if (this.onCrouching)
                {
                    this.playerChar.setAnimationState((this.direction.y < 0) ? "shift_crouch" : "shift_crouch_back");
                }
                else
                {
                    this.playerChar.setAnimationState((this.direction.y < 0) ? "shift" : "shift_back");
                }

                this.playerChar.setAnimationTimeScale(scale);
            }
            else
            {
                this.playerChar.setAnimationTimeScale(1.0f);

                if (this.onCrouching)
                {
                    this.playerChar.setAnimationState("crouch_idle");
                }
                else
                {
                    this.playerChar.setAnimationState("idle");
                }
            }
        }

        private void Accelerate(ref Vector3 playerVelocity, Vector3 direction, float accel, float deaccel, float dt)
        {
            var _accel = Accel;
            if (direction.Dot(playerVelocity) > 0)
                _accel = Deaccel;

            playerVelocity = playerVelocity.Lerp(direction, _accel * dt);
        }

        private void ApplyFriction(ref Vector3 playerVelocity, float dt)
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



        Vector3 AirAccelerate(Vector3 wishDir, Vector3 prevVelocity, float wishSpeed, float accel, float delta)
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


        public override void _Ready()
        {
            base._Ready();
        }

        public void setPlayerCollider(float delta)
        {
            //change prone to steps by collider shape height (> 0.6, > 0.3)

            var upSpeed = crouchUpSpeed;
            var downSpeed = crouchDownSpeed;

            if (this.playerChar.getShapeDivider() < this.crouchColliderMultiplier)
            {
                upSpeed = proneUpSpeed;
                downSpeed = proneDownSpeed;
            }

            if (this.onProne)
            {
                this.playerChar.setBodyHeight(delta, upSpeed, downSpeed, proneColliderMultiplier, onProne);
            }
            else
            {
                this.playerChar.setBodyHeight(delta, upSpeed, downSpeed, crouchColliderMultiplier, onCrouching);
            }
        }

        /**
         * TODO: REPLACE WITH https://github.com/joelpt/onclick5/blob/master/Assets/SimpleSmoothMouseLook.cs*
         *
         */
        public override void _Process(float delta)
        {

            if (!canHandleInput)
                return;

            if (Input.GetMouseMode() != Input.MouseMode.Captured)
                return;


            this.setPlayerCollider(delta);

            if (!playerChar.IsOnFloor())
            {
                this.playerChar.setCameraZoom(false);
            }
            else
            {
                this.playerChar.setCameraZoom(rightMouseActivated);
            }

            if (mouseDelta.Length() > 0)
            {
                // rotate the camera along the x axis
                rotationX += -mouseDelta.x * ConfigValues.sensitivityX * delta;
                rotationY += -mouseDelta.y * ConfigValues.sensitivityY * delta;
                float rotAverageX = 0f;
                float rotAverageY = 0f;

                // Add current rot to array, at end
                _rotArrayX.Add(rotationX);
                _rotArrayY.Add(rotationY);

                if (_rotArrayX.Count >= _averageFromThisManySteps)
                    _rotArrayX.RemoveAt(0);

                if (_rotArrayY.Count >= _averageFromThisManySteps)
                    _rotArrayY.RemoveAt(0);

                //Add all of these rotations together
                for (int i_counterX = 0; i_counterX < _rotArrayX.Count; i_counterX++)
                {
                    rotAverageX += _rotArrayX[i_counterX];
                }

                for (int i_counterY = 0; i_counterY < _rotArrayY.Count; i_counterY++)
                {
                    rotAverageY += _rotArrayY[i_counterY];
                }

                // Get average
                rotAverageX /= _rotArrayX.Count;
                rotAverageY /= _rotArrayY.Count;

                if (rotationX > maxLookAngleX)
                    rotationX -= maxLookAngleX;

                if (rotationX < minLookAngleX)
                    rotationX -= minLookAngleX;

                rotAverageY = Mathf.Clamp(rotationY, minLookAngleY, maxLookAngleY);
                rotAverageX = Mathf.Clamp(rotationX, minLookAngleX, maxLookAngleX);

                this.playerChar.rotateFPS(rotAverageX, rotAverageY);
                this.playerChar.rotateTPSCamera(rotAverageY);

                mouseDelta = Vector2.Zero;
            }
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if (!canHandleInput)
                return;

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

        public void Activate()
        {
            this.canHandleInput = true;
            Input.SetMouseMode(Input.MouseMode.Captured);
            this.playerChar.setCameraMode(PlayerCameraMode.FPS);
        }

        public void Dectivate()
        {
            this.canHandleInput = false;
            Input.SetMouseMode(Input.MouseMode.Visible);
            this.playerChar.setCameraMode(PlayerCameraMode.NONE);
        }
    }
}