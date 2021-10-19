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
        NodePath animationTreeNodePath = null;

        [Export]
        NodePath weaponHolderPath = null;

        [Export]
        NodePath footstepPlayerPath = null;

        [Export]
        NodePath aimRayPath = null;

        RayCast3D aimRay = null;
        WeaponHolder weaponHolder = null;
        AudioStreamPlayer3D footstepPlayer = null;

        [Export]
        NodePath cameraNodePath = null;

        [Export]
        NodePath cameraThirdPersonPath = null;

        [Export]
        NodePath tpsCharacterPath = null;


        private Node3D head = null;
        private FPSCamera _FPSCamera;
        private AnimationTree _tree;

        private Camera3D _thirdPersonCamera;

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

        public override void _Process(float delta)
        {
            base._Process(delta);

            if (zoomIn)
            {
                this._FPSCamera.Fov = Mathf.Lerp(this._FPSCamera.Fov, this.zoomFov, this.zoomInSpeed * delta);
            }
            else
            {
                this._FPSCamera.Fov = Mathf.Lerp(this._FPSCamera.Fov, this.defaultFov, this.zoomOutSpeed * delta);
            }
        }

        private string currentAnimationName;
        private float currentAnimationScale;

        public void setAnimationState(string name)
        {
            var obj = this._tree.Get("parameters/state/playback");
            if (obj != null)
            {
                (obj as AnimationNodeStateMachinePlayback).Travel(name);
                currentAnimationName = name;
            }
        }

        public string getAnimationState()
        {
            return currentAnimationName;
        }
        public float getAnimationScale()
        {
            return currentAnimationScale;
        }

        public void setAnimationTimeScale(float timeScale)
        {
            currentAnimationScale = timeScale;
            this._tree.Set("parameters/scale/scale", timeScale);
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
                this._FPSCamera.Current = false;
                this._FPSCamera.Visible = false;
                this._thirdPersonCamera.Current = true;
            }
            else if (mode == PlayerCameraMode.FPS)
            {
                this._FPSCamera.Visible = true;
                this._FPSCamera.Current = true;
                this._thirdPersonCamera.Current = false;
            }
            else
            {
                this._FPSCamera.Current = false;
                this._FPSCamera.Visible = false;
                this._thirdPersonCamera.Current = false;
            }
        }

        public void setDrawMode(PlayerDrawMode mode)
        {
            if (mode == PlayerDrawMode.TPS)
            {
                this._FPSCamera.Visible = false;
                this._tpsCharacter.Visible = true;
                enableShadowing(this._tpsCharacter, false);
            }
            else if (mode == PlayerDrawMode.FPS)
            {

                this._FPSCamera.Visible = true;
                this._tpsCharacter.Visible = true;
                enableShadowing(this._tpsCharacter, true);
            }
            else
            {
                this._FPSCamera.Visible = false;
                this._tpsCharacter.Visible = false;
            }
        }

        private void enableShadowing(Node node, bool enableShadows)
        {
            if (node is MeshInstance3D)
            {
                (node as MeshInstance3D).CastShadow = (enableShadows) ? GeometryInstance3D.ShadowCastingSetting.ShadowsOnly : GeometryInstance3D.ShadowCastingSetting.On;
                (node as MeshInstance3D).GiMode = (enableShadows) ? GeometryInstance3D.GIMode.Dynamic : GeometryInstance3D.GIMode.Baked;
            }

            foreach (var child in node.GetChildren())
            {
                if (child is Node)
                    this.enableShadowing(child as Node, enableShadows);
            }

        }


        //cam look
        const float minLookAngleY = -85.0f;
        const float maxLookAngleY = 85.0f;
        public void rotateTPSCamera(float headRotation)
        {
            var tpsCamera = this._thirdPersonCamera.Rotation;
            tpsCamera.x -= Mathf.Deg2Rad(headRotation);
            tpsCamera.x = Mathf.Deg2Rad(Mathf.Clamp(Mathf.Rad2Deg(tpsCamera.x), minLookAngleY, maxLookAngleY));
            this._thirdPersonCamera.Rotation = tpsCamera;
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

        public Vector3 GetHeadPosition()
        {
            return this.head.Transform.origin;
        }


        public void SetHeadPosition(Vector3 pos)
        {
            var tf = this.head.Transform;
            tf.origin = pos;
            this.head.Transform = tf;
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

        public void DisableHitboxes()
        {
            foreach (var box in this.GetNode("hitboxes").GetChildren())
            {
                if (box is Area3D)
                    this.GetNode("hitboxes").RemoveChild(box as Area3D);
            }

        }

        public void DoFire()
        {
            if (this.weaponHolder.currentGun != null)
            {
                if (this.weaponHolder.currentGun.CanShoot())
                {
                    this.weaponHolder.currentGun.FireGun();

                    this._FPSCamera.ShakeForce = 0.002f;
                    this._FPSCamera.ShakeTime = 0.2f;

                    if (this.aimRay.IsColliding())
                    {
                        var collider = this.aimRay.GetCollider();
                        if (collider is StaticBody3D)
                        {
                            GameWorld.TriggerNewDecal(this.aimRay.GetCollisionPoint(), this.aimRay.GetCollisionNormal(), collider as StaticBody3D);
                        }
                    }
                }
            }
        }

        [Export(PropertyHint.ArrayType)]
        System.Collections.Generic.List<string> WalkSoundPaths = new System.Collections.Generic.List<string>();

        System.Collections.Generic.List<AudioStreamMP3> WalkSounds = new System.Collections.Generic.List<AudioStreamMP3>();

        public override void _EnterTree()
        {
            base._EnterTree();

            foreach (var filePath in WalkSoundPaths)
            {
                var load = GD.Load<AudioStreamMP3>(filePath);
                this.WalkSounds.Add(load);
            }

            this._FPSCamera = GetNode<FPSCamera>(cameraNodePath);
            this._thirdPersonCamera = GetNode(cameraThirdPersonPath) as Camera3D;
            this._tpsCharacter = GetNode(tpsCharacterPath) as Node3D;
            this._tree = GetNode(animationTreeNodePath) as AnimationTree;
            this.weaponHolder = GetNode(weaponHolderPath) as WeaponHolder;
            this.footstepPlayer = GetNode<AudioStreamPlayer3D>(footstepPlayerPath);
            this.aimRay = GetNode(aimRayPath) as RayCast3D;

            this.head = GetNode("head") as Node3D;

            this.collider = GetNode("collider") as CollisionShape3D;
            this.colliderShape = this.collider.Shape as CapsuleShape3D;

        }

        public bool canPlayFootstepSound = true;


        public void doFootstep()
        {
            System.Random rnd = new System.Random();
            int index = rnd.Next(this.WalkSounds.Count);
            var item = this.WalkSounds[index];

            if (item != null)
            {
                if (canPlayFootstepSound)
                {
                    this.canPlayFootstepSound = false;
                    item.Loop = false;
                    item.LoopOffset = 0;
                    this.footstepPlayer.Stream = item;
                    this.footstepPlayer.Play(0);
                }
                else
                {
                    if (this.footstepPlayer.GetPlaybackPosition() >= this.footstepPlayer.Stream.GetLength())
                    {
                        this.canPlayFootstepSound = true;
                    }
                }
            }
        }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {

            this.origColliderPositionY = this.collider.Transform.origin.y;
            this.origHeadY = this.head.Transform.origin.y;
            this.origColliderShapeHeight = this.colliderShape.Height;

            this._FPSCamera.Current = false;
            this._thirdPersonCamera.Current = false;
            this._tpsCharacter.Visible = false;
            this._FPSCamera.Visible = false;

            this.setAnimationState("idle");
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