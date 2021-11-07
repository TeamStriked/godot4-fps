using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using FPS.Game.Logic.Weapon;
using FPS.Game.Logic.World;
using FPS.Game.Logic.Camera;

namespace FPS.Game.Logic.Player
{
    public enum PlayerCameraMode
    {
        FPS,
        TPS,
        NONE
    }
    public enum PlayerDrawMode
    {
        FPS,
        TPS,
        NONE
    }
    public partial class CharacterInstance : CharacterBody3D
    {
        private Node3D _tpsCharacter;

        //physics
        [Export]
        float defaultFov = 65f;

        //physics
        [Export]
        float zoomFov = 50f;

        bool isThirdPerson = false;

        bool zoomIn = false;

        [Export]
        float zoomInSpeed = 16.4f;

        [Export]
        float zoomOutSpeed = 20.3f;




        [Export]
        NodePath weaponHolderPath = null;

        [Export]
        NodePath footstepPlayerPath = null;
        [Export]
        NodePath customSoundPlayerPath = null;

        [Export]
        NodePath aimRayPath = null;

        RayCast3D aimRay = null;
        WeaponHolder weaponHolder = null;
        AudioStreamPlayer3D footstepPlayer = null;
        AudioStreamPlayer3D CustomSoundPlayer = null;

        [Export]
        NodePath cameraNodePath = null;

        [Export]
        NodePath cameraThirdPersonPath = null;

        [Export]
        NodePath tpsCharacterPath = null;


        private Node3D head = null;
        private FPSCamera _FPSCamera;
        private AnimationTree _animationTree;
        private Skeleton3D _skeleton;

        private Camera3D _TPSCamera;

        private CollisionShape3D collider = null;
        private CapsuleShape3D colliderShape = null;


        private float origColliderPositionY = 0;
        private float origColliderShapeHeight = 0;
        private float origHeadY = 0;

        Vector3 cam_translation = Vector3.Zero;

        float bob_cycle = 0.0f;

        [Export] float bob_speed = 0.8f;
        [Export] float bob_factor = 0.2f;
        [Export] float min_weight = 1.2f;
        [Export] float interpolation = 4.0f;



        [Export(PropertyHint.Dir)]
        string WalkSoundPath = "";

        [Export(PropertyHint.Dir)]
        string SprintSoundPath = "";

        System.Collections.Generic.List<AudioStreamSample> WalkSounds = new System.Collections.Generic.List<AudioStreamSample>();
        System.Collections.Generic.List<AudioStreamSample> SprintSounds = new System.Collections.Generic.List<AudioStreamSample>();

        [Export]
        AudioStreamSample landingSound = null;

        public override void _Process(float delta)
        {
            base._Process(delta);

            var weapon = this.GetCurrentWeapon();

            if (zoomIn)
            {
                this._FPSCamera.Fov = Mathf.Lerp(this._FPSCamera.Fov, this.zoomFov, this.zoomInSpeed * delta);

                if (weapon != null)
                {
                    weapon.Position = weapon.Position.Lerp(weapon.zoomPosition, this.zoomInSpeed * delta);
                }
            }
            else
            {
                this._FPSCamera.Fov = Mathf.Lerp(this._FPSCamera.Fov, FPS.Game.Config.ConfigValues.fov, this.zoomOutSpeed * delta);
                if (weapon != null)
                {
                    weapon.Position = weapon.Position.Lerp(weapon.defaultPosition, this.zoomOutSpeed * delta);
                }
            }
        }

        private int currentAnimationName;
        private Godot.Vector2 lastStrafeSpace = Godot.Vector2.Zero;
        private float currentAnimationScale;


        public void setAnimationAim(Vector2 aimPos)
        {
            if (_animationTree != null)
            {
                this._animationTree.Set("parameters/aim_up_down/add_amount", aimPos.y * -1);
            }
        }
        public void setAnimationState(int state)
        {
            if (_animationTree != null)
            {
                this._animationTree.Set("parameters/movement/current", state);
                currentAnimationName = state;
            }
        }

        public const float animationSpeed = 100f;

        public void setAnimationMovement(Godot.Vector2 movement)
        {
            if (_animationTree != null)
            {
                Vector2 vec2 = (Vector2)this._animationTree.Get("parameters/strafe_space/blend_position");

                this._animationTree.Set("parameters/strafe_space/blend_position", vec2.Lerp(movement, (float)this.GetProcessDeltaTime() * animationSpeed));
                this._animationTree.Set("parameters/walk_space/blend_position", vec2.Lerp(movement, (float)this.GetProcessDeltaTime() * animationSpeed));

                lastStrafeSpace = movement;
            }
        }

        public int getAnimationState()
        {
            return currentAnimationName;
        }
        public float getAnimationScale()
        {
            return currentAnimationScale;
        }

        public void setAnimationTimeScale(float timeScale)
        {
            if (this._animationTree != null)
            {
                currentAnimationScale = timeScale;
                this._animationTree.Set("parameters/movement_time/scale", timeScale);
            }
        }

        public void setAnimationFlyScale(float timeScale)
        {
            if (this._animationTree != null)
            {
                var flyScale = Mathf.Lerp(this.getAnimationFlyScale(), timeScale, (float)this.GetProcessDeltaTime() * animationSpeed);
                this._animationTree.Set("parameters/fly/blend_amount", flyScale);
            }
        }

        public float getAnimationFlyScale()
        {
            if (this._animationTree != null)
            {
                return (float)this._animationTree.Get("parameters/fly/blend_amount");
            }

            return 0.0f;
        }

        public void setBobbing(float delta)
        {
            var hv = MotionVelocity;
            hv.y = 0.0f;

            var speed = MotionVelocity.Length();

            var bob_weight = Mathf.Min(hv.Length() / speed, 10.0f);

            if (bob_weight >= min_weight)
            {
                bob_cycle = (bob_cycle + 360.0f * delta * bob_weight * bob_speed) % 360.0f;
            }
            else
            {
                bob_cycle = 0.0f;
            }

            var factor = bob_factor * bob_weight;
            var cam_transform = cam_translation;

            cam_transform -= Transform.basis.x * Mathf.Sin(Mathf.Deg2Rad(bob_cycle)) * factor;
            cam_transform += Transform.basis.y * Mathf.Abs(Mathf.Sin(Mathf.Deg2Rad(bob_cycle))) * factor * 1.5f;

            cam_translation = cam_translation.Lerp(Vector3.Zero, interpolation * delta);


            var ot = this.Camera.Transform;
            ot.origin = this.Camera.Transform.origin.Lerp(cam_transform, interpolation * delta);

            this.Camera.Transform = ot;
        }


        public void setCameraZoom(bool zoomIn)
        {
            this.zoomIn = zoomIn;
        }

        public void setCameraMode(PlayerCameraMode mode)
        {
            if (mode == PlayerCameraMode.TPS)
            {
                this._TPSCamera.Visible = true;
                this._TPSCamera.Current = true;

                this._FPSCamera.Current = false;
                this._FPSCamera.Visible = false;
            }
            else if (mode == PlayerCameraMode.FPS)
            {
                this._FPSCamera.Current = true;
                this._FPSCamera.Visible = true;

                this._TPSCamera.Current = false;
                this._TPSCamera.Visible = false;
            }
            else
            {
                this._FPSCamera.Current = false;
                this._TPSCamera.Current = false;
                this._FPSCamera.Visible = false;
                this._TPSCamera.Visible = false;
            }
        }


        public void setDrawMode(PlayerDrawMode mode)
        {
            if (mode == PlayerDrawMode.TPS)
            {
                this._tpsCharacter.Visible = true;
                enableShadowing(this._tpsCharacter, GeometryInstance3D.ShadowCastingSetting.On);
                enableShadowing(this._FPSCamera, GeometryInstance3D.ShadowCastingSetting.Off);
            }
            else if (mode == PlayerDrawMode.FPS)
            {
                this._tpsCharacter.Visible = true;
                enableShadowing(this._tpsCharacter, GeometryInstance3D.ShadowCastingSetting.ShadowsOnly);
                enableShadowing(this._FPSCamera, GeometryInstance3D.ShadowCastingSetting.Off);
            }
            else
            {
                this._tpsCharacter.Visible = false;
                enableShadowing(this._FPSCamera, GeometryInstance3D.ShadowCastingSetting.Off);
                enableShadowing(this._FPSCamera, GeometryInstance3D.ShadowCastingSetting.Off);
            }
        }

        private void enableShadowing(Node node, GeometryInstance3D.ShadowCastingSetting setting)
        {
            if (node is MeshInstance3D)
            {
                (node as MeshInstance3D).CastShadow = setting;
            }

            foreach (var child in node.GetChildren())
            {
                if (child is Node)
                    this.enableShadowing(child as Node, setting);
            }

        }


        //cam look
        const float minLookAngleY = -85.0f;
        const float maxLookAngleY = 85.0f;
        public void rotateTPSCamera(float headRotation)
        {
            var tpsCamera = this._TPSCamera.Rotation;
            tpsCamera.x -= Mathf.Deg2Rad(headRotation);
            tpsCamera.x = Mathf.Deg2Rad(Mathf.Clamp(Mathf.Rad2Deg(tpsCamera.x), minLookAngleY, maxLookAngleY));
            this._TPSCamera.Rotation = tpsCamera;
        }

        public void rotateFPS(float charRotation, float headRotation)
        {
            var rotY = this.Rotation;
            rotY.y += -Mathf.Deg2Rad(charRotation);
            this.Rotation = rotY;

            //clamp that shit
            var headRotX = this.head.Rotation;
            headRotX.x += -Mathf.Deg2Rad(headRotation);
            headRotX.x = Mathf.Deg2Rad(Mathf.Clamp(Mathf.Rad2Deg(headRotX.x), minLookAngleY, maxLookAngleY));
            this.head.Rotation = headRotX;
        }


        public void rotateFPSAfterRecoil(float charRotation, float headRotation)
        {
            var rotY = this.Rotation;
            rotY.y = Mathf.Deg2Rad(charRotation);
            this.Rotation = rotY;

            //clamp that shit
            var headRotX = this.head.Rotation;
            headRotX.x = Mathf.Deg2Rad(headRotation);
            headRotX.x = Mathf.Deg2Rad(Mathf.Clamp(Mathf.Rad2Deg(headRotX.x), minLookAngleY, maxLookAngleY));
            this.head.Rotation = headRotX;
        }

        public float GetCharRotation()
        {
            return this.Rotation.y;
        }

        public float GetHeadRotation()
        {
            var headRot = this.head.Rotation;
            return headRot.x;
        }

        public void SetCharRotation(float value)
        {
            var rot = this.Rotation;
            rot.y = value;
            this.Rotation = rot;
        }

        public void SetHeadRotation(float value)
        {
            var headRot = this.head.Rotation;
            headRot.x = value;
            this.head.Rotation = headRot;
        }

        public float getShapeDivider()
        {
            return this.colliderShape.Height / this.origColliderShapeHeight;
        }

        public void setBodyHeight(float delta, float upSpeed, float multiplier, bool onCrouching = false)
        {

            var shape = this.colliderShape.Height;
            shape = Mathf.Lerp(shape, this.origColliderShapeHeight - ((onCrouching ? 1.0f : 0.0f) * multiplier), upSpeed * delta);
            this.colliderShape.Height = shape;

            //set tps character model pos
            var reducer = 1.0f - (shape / this.origColliderShapeHeight);

            var tf = this._tpsCharacter.Transform;
            tf.origin.y = reducer * this.colliderShape.Height;
            this._tpsCharacter.Transform = tf;
        }

        public float getSpeed()
        {
            var temp = MotionVelocity;
            temp.y = 0;

            return temp.Length();
        }

        public void IgnoreOwnHitboxes()
        {
            this.aimRay.AddException(this);
            this.detectHitBoxes(_tpsCharacter);
        }
        private void detectHitBoxes(Node instance)
        {
            if (instance is Hitbox)
            {
                this.aimRay.AddException(instance as Hitbox);
            }
            else
            {
                foreach (var item in instance.GetChildren())
                {
                    if (item is Hitbox)
                    {
                        this.detectHitBoxes(item as Node);
                    }
                }
            }
        }

        public Weapon.Weapon GetCurrentWeapon()
        {
            return this.weaponHolder.currentGun;
        }

        public void setCameraShake(float shakeForce = 0.002f, float shakeTime = 0.2f)
        {
            this._FPSCamera.ShakeForce = shakeForce;
            this._FPSCamera.ShakeTime = shakeTime;
        }

        public RayCast3D getRaycast3D()
        {
            return this.aimRay;
        }

        public System.Collections.Generic.List<AudioStreamSample> loadFootstepSounds(string path)
        {
            var fileList = new System.Collections.Generic.List<AudioStreamSample>();
            var folder = new Godot.Directory();
            folder.Open(path);
            folder.ListDirBegin();

            while (true)
            {
                var file = folder.GetNext();
                if (file == "" || file == null)
                    break;
                else if (!file.BeginsWith(".") && file.EndsWith(".wav"))
                {
                    var filePath = System.IO.Path.Combine(path, file);
                    var load = GD.Load<AudioStreamSample>(filePath);
                    fileList.Add(load);
                }
            }

            folder.ListDirEnd();
            return fileList;
        }

        public override void _EnterTree()
        {
            base._EnterTree();


            /*
                        foreach (var filePath in WalkSoundPaths)
                        {
                            var load = GD.Load<AudioStreamMP3>(filePath);
                            this.WalkSounds.Add(load);
                        }
            */
            this._FPSCamera = GetNode<FPSCamera>(cameraNodePath);
            this._TPSCamera = GetNode(cameraThirdPersonPath) as Camera3D;
            this._tpsCharacter = GetNode(tpsCharacterPath) as Node3D;
            this.weaponHolder = GetNode(weaponHolderPath) as WeaponHolder;
            this.footstepPlayer = GetNode<AudioStreamPlayer3D>(footstepPlayerPath);
            this.CustomSoundPlayer = GetNode<AudioStreamPlayer3D>(customSoundPlayerPath);
            this.aimRay = GetNode(aimRayPath) as RayCast3D;

            this._FPSCamera.Current = false;
            this._TPSCamera.Current = false;

            this.detectAnimationTree(this._tpsCharacter);
            this.detectSkeleton(this._tpsCharacter);

            if (this._animationTree != null)
                this._animationTree.Active = false;



            if (this._skeleton != null)
            {
                this._skeleton.ShowRestOnly = true;
                this._skeleton.ClearBonesGlobalPoseOverride();
                this._skeleton.ClearBonesLocalPoseOverride();
                this._skeleton.ShowRestOnly = false;
            }

            this.head = GetNode("head") as Node3D;

            this.collider = GetNode("collider") as CollisionShape3D;
            this.colliderShape = this.collider.Shape as CapsuleShape3D;
        }

        public bool canPlayFootstepSound = true;
        public int currentStep = 0;

        public void playLandingSound()
        {
            if (this.landingSound != null)
            {
                var randm = new RandomNumberGenerator();
                randm.Randomize();

                this.CustomSoundPlayer.PitchScale = randm.RandfRange(0.8f, 1.2f);
                this.CustomSoundPlayer.UnitDb = randm.RandfRange(0.8f, 1.0f);
                this.CustomSoundPlayer.Stream = this.landingSound;
                this.CustomSoundPlayer.Play(0);
            }
        }

        public void doFootstep(bool isSprint)
        {
            System.Random rnd = new System.Random();

            var list = (isSprint) ? this.SprintSounds : this.WalkSounds;
            if (list.Count <= 0)
                return;

            int index = rnd.Next(list.Count);
            var item = list[index];

            if (item != null)
            {
                var randm = new RandomNumberGenerator();
                randm.Randomize();
                this.canPlayFootstepSound = false;
                this.footstepPlayer.PitchScale = randm.RandfRange(0.8f, 1.2f);
                this.footstepPlayer.UnitDb = randm.RandfRange(0.8f, 1.0f);
                this.footstepPlayer.Stream = item;
                this.footstepPlayer.Play(0);

                var pos = this.footstepPlayer.Position;
                pos.z = (currentStep % 2 == 0) ? -0.3f : 0.3f;
                this.footstepPlayer.Position = pos;

                currentStep++;

                if (currentStep == 100)
                    currentStep = 0;
            }
        }

        private bool detectSkeleton(Node instance)
        {
            if (instance is Skeleton3D)
            {
                this._skeleton = instance as Skeleton3D;
                return true;
            }
            else
            {
                foreach (var item in instance.GetChildren())
                {
                    if (item is Node)
                    {
                        var result = this.detectSkeleton(item as Node);
                        if (result == true)
                            return true;
                    }
                }
            }

            return false;
        }

        private bool detectAnimationTree(Node instance)
        {
            if (instance is AnimationTree)
            {
                this._animationTree = instance as AnimationTree;
                return true;
            }
            else
            {
                foreach (var item in instance.GetChildren())
                {
                    if (item is Node)
                    {
                        var result = this.detectAnimationTree(item as Node);
                        if (result == true)
                            return true;
                    }
                }
            }

            return false;
        }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            this.WalkSounds = this.loadFootstepSounds(WalkSoundPath);
            this.SprintSounds = this.loadFootstepSounds(SprintSoundPath);

            this.origColliderPositionY = this.collider.Transform.origin.y;
            this.origHeadY = this.head.Transform.origin.y;
            this.origColliderShapeHeight = this.colliderShape.Height;

            this._FPSCamera.Current = false;
            this._TPSCamera.Current = false;
            this._tpsCharacter.Visible = false;
            this._FPSCamera.Visible = false;
            this._FPSCamera.Fov = FPS.Game.Config.ConfigValues.fov;

            this._animationTree.Active = true;
            this.setAnimationState(0);
            this.setAnimationMovement(Godot.Vector2.Zero);
            this.IgnoreOwnHitboxes();
        }

        public FPSCamera Camera
        {
            get
            {
                return _FPSCamera;
            }
        }
    }
}