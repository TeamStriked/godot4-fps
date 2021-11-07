using Godot;
using System.Collections.Generic;
using System.Linq;
using System;
namespace FPS.Game.Logic.Weapon
{
    public partial class Weapon : Node3D
    {
        [Export]
        NodePath smokeEffectPath = null;
        [Export]
        NodePath muzzleEffectPath = null;

        [Export]
        NodePath shotLightPath = null;

        [Export]
        NodePath audioShotPlayerPath = null;

        AudioStreamPlayer3D audioShotPlayer = null;

        GPUParticles3D smokeEffect = null;
        GPUParticles3D muzzleEffect = null;
        OmniLight3D shotLight = null;

        public float fireRate = 12.0f;

        private float fireTimeout = 0f;

        [Export]
        private float damagePerBulletInHp = 0f;

        public Vector3 defaultPosition = Vector3.Zero;

        private Vector3 _zoomPosition = Vector3.Zero;

        [Export]
        public Vector3 zoomPosition = Vector3.Zero;


        public override void _EnterTree()
        {
            base._EnterTree();
        }


        public override void _ExitTree()
        {
            base._ExitTree();

            if (Engine.IsEditorHint())
                this.Position = this.defaultPosition;
        }
        public override void _Ready()
        {
            base._Ready();
            if (!Engine.IsEditorHint())
            {
                this.smokeEffect = GetNode<GPUParticles3D>(smokeEffectPath);
                this.muzzleEffect = GetNode<GPUParticles3D>(muzzleEffectPath);
                this.shotLight = GetNode<OmniLight3D>(shotLightPath);
                this.audioShotPlayer = GetNode<AudioStreamPlayer3D>(audioShotPlayerPath);
            }

            this.defaultPosition = this.Position;
        }

        public bool CanShoot()
        {
            return (fireTimeout <= 0.0f);
        }

        public void FireGun()
        {
            this.fireTimeout = this.fireRate / 100;
        }

        public void GunEffects()
        {
            this.shotLight.LightEnergy = 2.0f;
            this.smokeEffect.Emitting = true;
            this.muzzleEffect.Emitting = true;

            //play sound
            var stream = this.audioShotPlayer.Stream as AudioStreamMP3;
            stream.Loop = false;

            this.audioShotPlayer.Play();
        }

        public void ProcessWeapon(float delta)
        {
            //dim the shot light
            this.shotLight.LightEnergy = Mathf.Lerp(this.shotLight.LightEnergy, 0, 5 * delta);

            if (this.fireTimeout > 0.0f)
            {
                this.fireTimeout -= delta;
            }
        }

    }
}
