using System;
using Godot;

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
        NodePath cameraNodePath = null;

        [Export]
        NodePath cameraThirdPersonPath = null;

        [Export]
        NodePath tpsCharacterPath = null;


        private Node3D head = null;
        private Camera3D _camera;
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
                this._camera.Fov = Mathf.Lerp(this._camera.Fov, this.zoomFov, this.zoomInSpeed * delta);
            }
            else
            {
                this._camera.Fov = Mathf.Lerp(this._camera.Fov, this.defaultFov, this.zoomOutSpeed * delta);
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
                this._camera.Current = false;
                this._camera.Visible = false;
                this._thirdPersonCamera.Current = true;
            }
            else if (mode == PlayerCameraMode.FPS)
            {
                this._camera.Visible = true;
                this._camera.Current = true;
                this._thirdPersonCamera.Current = false;
            }
            else
            {
                this._camera.Current = false;
                this._camera.Visible = false;
                this._thirdPersonCamera.Current = false;
            }
        }

        public void setDrawMode(PlayerDrawMode mode)
        {
            if (mode == PlayerDrawMode.TPS)
            {
                this._camera.Visible = false;
                this._tpsCharacter.Visible = true;
                enableShadowing(this._tpsCharacter, false);
            }
            else if (mode == PlayerDrawMode.FPS)
            {

                this._camera.Visible = true;
                this._tpsCharacter.Visible = true;
                enableShadowing(this._tpsCharacter, true);
            }
            else
            {
                this._camera.Visible = false;
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
        const float minLookAngleY = -90.0f;
        const float maxLookAngleY = 90.0f;
        public void rotateTPSCamera(float headRotation)
        {
            var tpsCamera = this._thirdPersonCamera.Rotation;
            tpsCamera.x -= Mathf.Deg2Rad(headRotation);
            tpsCamera.x = Mathf.Deg2Rad(Mathf.Clamp(Mathf.Rad2Deg(tpsCamera.x), minLookAngleY, maxLookAngleY));
            this._thirdPersonCamera.Rotation = tpsCamera;
        }


        public void rotateFPS(float charRotation, float headRotation)
        {
            this.RotateY(-Mathf.Deg2Rad(charRotation));
            this.head.RotateX(-Mathf.Deg2Rad(headRotation));

            var headRot = this.head.Rotation;
            headRot.x = Mathf.Deg2Rad(Mathf.Clamp(Mathf.Rad2Deg(headRot.x), minLookAngleY, maxLookAngleY));
            this.head.Rotation = headRot;
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

        public void setBodyHeight(float delta, float upSpeed, float downSpeed, float multiplier, bool onCrouching = false)
        {
            if (onCrouching)
            {
                this.setColliderHeight(this.origColliderShapeHeight * multiplier, this.origColliderPositionY, this.origHeadY * multiplier, downSpeed, delta);
            }
            else if (!onCrouching)
            {
                this.setColliderHeight(this.origColliderShapeHeight, this.origColliderPositionY, this.origHeadY, upSpeed, delta);
            }
        }

        private void setColliderHeight(float shapeHeightTarget, float posY, float posHeadY, float speed, float delta)
        {
            //set new shape height
            //  var newHeight = Mathf.Lerp(this.colliderShape.Height, shapeHeightTarget, delta * speed);
            var newHeight = shapeHeightTarget;

            this.colliderShape.Height = newHeight;

            //set shape pos y
            var gd = this.collider.Transform;
            gd.origin.y = (posY / this.origColliderShapeHeight) * newHeight;
            this.collider.Transform = gd;

            //set head pos y
            var gdHead = this.head.Transform;
            gdHead.origin.y = Mathf.Lerp(gdHead.origin.y, posHeadY, delta * speed);
            this.head.Transform = gdHead;
        }

        public float getSpeed()
        {
            var temp = MotionVelocity;
            temp.y = 0;

            return temp.Length();
        }


        public override void _EnterTree()
        {
            base._EnterTree();

            this._camera = GetNode(cameraNodePath) as Camera3D;
            this._thirdPersonCamera = GetNode(cameraThirdPersonPath) as Camera3D;
            this._tpsCharacter = GetNode(tpsCharacterPath) as Node3D;
            this._tree = GetNode(animationTreeNodePath) as AnimationTree;

            this.head = GetNode("head") as Node3D;

            this.collider = GetNode("collider") as CollisionShape3D;
            this.colliderShape = this.collider.Shape as CapsuleShape3D;
        }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {

            this.origColliderPositionY = this.collider.Transform.origin.y;
            this.origHeadY = this.head.Transform.origin.y;
            this.origColliderShapeHeight = this.colliderShape.Height;

            this._camera.Current = false;
            this._thirdPersonCamera.Current = false;
            this._tpsCharacter.Visible = false;
            this._camera.Visible = false;

            this.setAnimationState("idle");
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