using Godot;
using System;
using FPS.Game.Utils;
using System.Collections.Generic;
namespace FPS.Game.Logic.Player
{
    public partial class LocalPlayer : NetworkPlayer
    {
        [Export]
        Curve3D path = new Curve3D();

        [Export]
        NodePath cameraNodePath = null;

        [Export]
        NodePath cameraThirdPersonPath = null;

        [Export]
        NodePath tpsCharacterPath = null;

        bool isThirdPerson = false;

        //physics


        [Export] float sprintSpeed = 12f;




        [Export] float speedRechargeMultiplier = 2f;



        [Export] float speedLooseMultiplier = 0.3f;



        [Export] float currentSpeedAmount = 1.0f;


        [Export] float walkSpeed = 2.6f;


        [Export] float defaultSpeed = 6.0f;



        [Export] float Accel = 6;


        [Export] float Deaccel = 8; //start speed 


        [Export] float FlyAccel = 4; // stop speed


        [Export] float jumpForce = 9.0f;


        [Export] float jumpCoolDown = 0.7f;

        private float currentJumpTime = 0.0f;


        [Export] float gravity = 21.0f;

        //cam look
        float minLookAngleY = -90.0f;
        float maxLookAngleY = 90.0f;

        float minLookAngleX = -360.0f;
        float maxLookAngleX = 360.0f;

        float lookSensitivityX = 20.0f;
        float lookSensitivityY = 20.0f;



        [Export] private float crouchUpSpeed = 6;



        [Export] private float crouchDownSpeed = 18;

        //vec
        Vector3 vel = new Vector3();
        Vector2 mouseDelta = new Vector2();

        private Camera3D _camera;
        private Camera3D _thirdPersonCamera;

        private Node3D _tpsCharacter;

        private bool activated = false;

        // How precise the controller can change direction while not grounded 


        [Export] private float AirControlPrecision = 16f;

        // When moving only forward, increase air control dramatically


        [Export] private float AirControlAdditionForward = 8f;
        // Stop if under this speed



        [Export] private float FrictionSpeedThreshold = 0.5f;

        // How fast the controller decelerates on the grounded


        [Export] private float Friction = 15;

        private bool onCrouching = false;



        private Node3D head = null;
        private CollisionShape3D collider = null;
        private CapsuleShape3D colliderShape = null;

        private float origColliderPositionY = 0;
        private float origColliderShapeHeight = 0;
        private float origHeadY = 0;
        private float crouchColliderMultiplier = 0.6f;

        private const int _averageFromThisManySteps = 3;

        private List<float> _rotArrayX = new List<float>();
        private List<float> _rotArrayY = new List<float>();

        private float rotationX = 0F;
        private float rotationY = 0F;



        [Puppet]
        public override void onNetworkTeleport(Vector3 origin)
        {
            GD.Print("Cliend receive teleport on " + origin);
            base.DoTeleport(origin);
        }

        // Called when the node enters the scene tree for the first time.
        public override void _PhysicsProcess(float delta)
        {
            if (!activated)
                return;

            if (Input.GetMouseMode() != Input.MouseMode.Captured)
                return;

            var currentSpeed = LinearVelocity.Length();

            vel = LinearVelocity;

            // reset the x and z velocity
            var input = new Vector2();

            //movement inputs
            if (Input.IsActionPressed("move_forward"))
                input.y -= 1f;
            if (Input.IsActionPressed("move_backward"))
                input.y += 1f;
            if (Input.IsActionPressed("move_left"))
                input.x -= 1f;
            if (Input.IsActionPressed("move_right"))
                input.x += 1f;

            if (Input.IsActionPressed("crouch"))
                onCrouching = true;
            else
                onCrouching = false;

            if (Input.IsActionJustPressed("camera_switch"))
                isThirdPerson = !isThirdPerson;

            input = input.Normalized();

            // get the forward and right directions
            var forward = Transform.basis.z;
            var right = Transform.basis.x;
            var relativeDir = (forward * input.y + right * input.x);

            // apply gravity
            if (!IsOnFloor())
            {
                onCrouching = false;

                if (Mathf.Abs(relativeDir.z) > 0.0001) // Pure side velocity doesn't allow air control
                {
                    ApplyAirControl(ref vel, relativeDir, delta);
                }

                //add gravity
                vel.y -= gravity * delta;
            }
            else
            {
                var moveSpeed = defaultSpeed;
                var accel = Accel;
                var deaccel = Deaccel;

                if (Input.IsActionPressed("move_walk") || onCrouching)
                {
                    moveSpeed = walkSpeed;
                }


                if (Input.IsActionPressed("move_sprint") && this.currentSpeedAmount > 0.1 && input.y != 0)
                {
                    onCrouching = false;

                    currentSpeedAmount = Mathf.Clamp(Mathf.Lerp(currentSpeedAmount, 0, speedLooseMultiplier * speedLooseMultiplier * delta), 0, 1.0f);
                    var offset = sprintSpeed - defaultSpeed;
                    moveSpeed = defaultSpeed + (offset * currentSpeedAmount);
                }
                else if (!Input.IsActionPressed("move_sprint"))
                {
                    currentSpeedAmount = Mathf.Clamp(Mathf.Lerp(currentSpeedAmount, 1.0f, speedRechargeMultiplier * delta), 0.0f, 1.0f);
                }

                //add speed
                relativeDir.x *= moveSpeed;
                relativeDir.y = 0;
                relativeDir.z *= moveSpeed;

                // jumping 
                if (Input.IsActionJustPressed("jump") && currentJumpTime <= 0.0f)
                {
                    currentJumpTime = jumpCoolDown;
                    vel.y = jumpForce;
                    onCrouching = false;
                }
                else
                {
                    ApplyFriction(ref relativeDir, delta);
                }

                Accelerate(ref vel, relativeDir, Accel, Deaccel, delta);

            }

            LinearVelocity = vel;
            MoveAndSlide();

            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();

            //   if (GetSlideCollisionCount() >= 1)
            // {
            //   GetSlideCollision(0).Dispose();
            //}

            currentJumpTime = Mathf.Clamp(currentJumpTime - delta, 0, jumpCoolDown);
            GD.Print(this.currentSpeedAmount);

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

        private void ApplyAirControl(ref Vector3 playerVelocity, Vector3 accelDir, float dt)
        {
            // This only happens in the horizontal plane
            // TODO: Verify that these work with various gravity values
            var playerDirHorz = playerVelocity.ToHorizontal().Normalized();
            var playerSpeedHorz = playerVelocity.ToHorizontal().Length();

            var dot = playerDirHorz.Dot(accelDir);
            if (dot > 0)
            {
                var k = AirControlPrecision * dot * dot * dt;

                // CPMA thingy:
                // If we want pure forward movement, we have much more air control
                var isPureForward = Mathf.Abs(mouseDelta.x) < 0.0001 && Mathf.Abs(mouseDelta.y) > 0;
                if (isPureForward)
                {
                    k *= AirControlAdditionForward;
                }

                // A little bit closer to accelDir
                playerDirHorz = playerDirHorz * playerSpeedHorz + accelDir * k;
                playerDirHorz = playerDirHorz.Normalized();

                // Assign new direction, without touching the vertical speed
                playerVelocity = (playerDirHorz * playerSpeedHorz).ToHorizontal() + (-Vector3.Down) * playerVelocity.VerticalComponent();
            }
        }

        public override void _Ready()
        {
            this.head = GetNode("head") as Node3D;
            this._camera = GetNode(cameraNodePath) as Camera3D;
            this._thirdPersonCamera = GetNode(cameraThirdPersonPath) as Camera3D;
            this._tpsCharacter = GetNode(tpsCharacterPath) as Node3D;

            this.collider = GetNode("collider") as CollisionShape3D;
            this.colliderShape = this.collider.Shape as CapsuleShape3D;

            this.origColliderPositionY = this.collider.Transform.origin.y;
            this.origHeadY = this.head.Transform.origin.y;
            this.origColliderShapeHeight = this.colliderShape.Height;
        }

        private void setBodyHeight(float delta)
        {
            if (onCrouching)
            {
                this.setHeight(this.origColliderShapeHeight * this.crouchColliderMultiplier, origColliderPositionY * this.crouchColliderMultiplier, origHeadY * this.crouchColliderMultiplier, delta, crouchUpSpeed);
            }
            else if (!onCrouching)
            {
                this.setHeight(this.origColliderShapeHeight, origColliderPositionY, origHeadY, delta, crouchDownSpeed);
            }
        }

        private void setHeight(float shapeHeightTarget, float targetColliderPosY, float targetHeaderPosY, float speed, float delta)
        {
            var heightOffset = shapeHeightTarget;
            this.colliderShape.Height = Mathf.Lerp(this.colliderShape.Height, shapeHeightTarget, delta * speed);

            //set shape y
            var gd = this.collider.Transform;
            gd.origin = gd.origin.Lerp(new Vector3(0, targetColliderPosY, 0), delta * speed);
            this.collider.Transform = gd;

            //set shape y
            var gdHead = this.head.Transform;
            gdHead.origin = gdHead.origin.Lerp(new Vector3(0, targetHeaderPosY, 0), delta * speed);
            this.head.Transform = gdHead;
        }

        public void setCamera()
        {
            if (isThirdPerson)
            {
                this._camera.Current = false;
                this._thirdPersonCamera.Current = true;
                this._tpsCharacter.Visible = true;
            }
            else
            {
                this._camera.Current = true;
                this._thirdPersonCamera.Current = false;
                this._tpsCharacter.Visible = false;
            }
        }

        public override void _Process(float delta)
        {

            if (!activated)
                return;

            if (Input.GetMouseMode() != Input.MouseMode.Captured)
                return;

            setCamera();

            setBodyHeight(delta);

            if (mouseDelta.Length() > 0)
            {
                // rotate the camera along the x axis
                rotationX += -mouseDelta.x * lookSensitivityX * delta;
                rotationY += -mouseDelta.y * lookSensitivityY * delta;

                // Add current rot to array, at end
                _rotArrayX.Add(rotationX);
                _rotArrayY.Add(rotationY);

                if (_rotArrayX.Count >= _averageFromThisManySteps)
                    _rotArrayX.RemoveAt(0);

                if (_rotArrayY.Count >= _averageFromThisManySteps)
                    _rotArrayY.RemoveAt(0);

                //Add all of these rotations together
                float rotAverageX = 0f;
                float rotAverageY = 0f;
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

                rotAverageY = Mathf.Clamp(rotAverageY, minLookAngleY, maxLookAngleY);
                rotAverageX = Mathf.Clamp(rotAverageX, minLookAngleX, maxLookAngleX);

                Quaternion xRotation = new Quaternion(Vector3.Up, rotAverageX);
                Quaternion headRotation = new Quaternion(Vector3.Left, rotAverageY);

                var rot = this.Rotation;
                rot.y = Mathf.Deg2Rad(rotAverageX);
                this.Rotation = rot;

                var headRot = this.head.Rotation;
                headRot.x = Mathf.Deg2Rad(rotAverageY);
                this.head.Rotation = headRot;

                var tpsCamera = this._thirdPersonCamera.Rotation;
                tpsCamera.x = Mathf.Deg2Rad(rotAverageY);
                this._thirdPersonCamera.Rotation = tpsCamera;


                mouseDelta = Vector2.Zero;
            }

        }


        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if (!activated)
                return;

            if (@event is InputEventMouseMotion)
            {
                mouseDelta = (@event as InputEventMouseMotion).Relative;
            }

            @event.Dispose();
        }

        public void Activate()
        {
            if (this.Camera != null)
            {
                Input.SetMouseMode(Input.MouseMode.Captured);
                this.Camera.Current = true;
                this.activated = true;
            }
        }

        public void Dectivate()
        {
            if (this.Camera != null)
            {
                Input.SetMouseMode(Input.MouseMode.Visible);
                this.Camera.Current = false;
                this.activated = false;
            }
        }

        public Camera3D Camera
        {
            get
            {
                return _camera;
            }
        }
    }
}