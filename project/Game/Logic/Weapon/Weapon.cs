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

        public override void _EnterTree()
        {
            base._EnterTree();
        }
        public override void _Ready()
        {
            base._Ready();

            this.smokeEffect = GetNode<GPUParticles3D>(smokeEffectPath);
            this.muzzleEffect = GetNode<GPUParticles3D>(muzzleEffectPath);
            this.shotLight = GetNode<OmniLight3D>(shotLightPath);
            this.audioShotPlayer = GetNode<AudioStreamPlayer3D>(audioShotPlayerPath);
        }

        public bool CanShoot()
        {
            return (fireTimeout <= 0.0f);
        }

        public void FireGun()
        {
            if (!this.CanShoot())
                return;

            this.shotLight.LightEnergy = 2.0f;
            this.smokeEffect.Emitting = true;
            this.muzzleEffect.Emitting = true;

            this.fireTimeout = this.fireRate / 100;

            //play sound
            var stream = this.audioShotPlayer.Stream as AudioStreamMP3;
            stream.Loop = false;

            this.audioShotPlayer.Play();
        }


        public override void _Process(float delta)
        {
            base._Process(delta);

            //dim the shot light
            this.shotLight.LightEnergy = Mathf.Lerp(this.shotLight.LightEnergy, 0, 5 * delta);

            if (this.fireTimeout > 0.0f)
            {
                this.fireTimeout -= delta;
            }
        }

    }
}
