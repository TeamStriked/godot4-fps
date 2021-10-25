using Godot;
using System;
using FPS.Game.Logic.Player.Handler;
namespace FPS.Game.Logic.Player
{
    public partial class PuppetPlayer : NetworkPlayer
    {
        public override bool isServerPlayer()
        {
            return false;
        }


        public override void _PhysicsProcess(float delta)
        {
            if (!isActivated)
                return;

            base._PhysicsProcess(delta);

            var newFrame = this.calulcateFrame(new InputFrame(), delta);
            this.execFrame(newFrame);
            lastFrame = newFrame;
        }

        public void IncomingServerFrame(CalculatedPuppetFrame puppetFrame)
        {
            this.DoTeleport(puppetFrame.origin);
            this.DoRotate(puppetFrame.rotation);
            this.playerChar.MotionVelocity = puppetFrame.velocity;
            this.playerChar.setAnimationState(puppetFrame.currentAnimation);
            this.playerChar.setAnimationTimeScale(puppetFrame.currentAnimationTime);
        }


        public override void Activate()
        {
            this.playerChar.ProcessMode = ProcessModeEnum.Always;

            this.playerChar.setCameraMode(PlayerCameraMode.NONE);
            this.playerChar.setDrawMode(PlayerDrawMode.TPS);
        }

    }
}