using Godot;
using System;

namespace FPS.Game.Logic.Camera
{
    public partial class FPSCamera : Camera3D
    {

        [Export]
        public float ShakeTime = 0;
        [Export]
        public float ShakeForce = 0;

        public override void _Process(float delta)
        {
            base._Process(delta);
            _shake(delta);
        }

        public void _shake(float _delta)
        {
            if (ShakeTime > 0)
            {
                var gen = new RandomNumberGenerator();
                gen.Randomize();

                HOffset = gen.RandfRange(-ShakeForce, ShakeForce);
                VOffset = gen.RandfRange(-ShakeForce, ShakeForce);
                ShakeTime -= _delta;
            }
            else
            {
                HOffset = 0;
                VOffset = 0;
            }
        }

    }
}