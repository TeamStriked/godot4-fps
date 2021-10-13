using Godot;
using System;
using FPS.Game.Logic.Player.Handler;
namespace FPS.Game.Logic.Player
{
    public partial class PuppetPlayer : NetworkPlayer
    {
        // Declare member variables here. Examples:
        // private int a = 2;
        // private string b = "text";

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            this.playerChar.setCameraMode(PlayerCameraMode.NONE);
            this.playerChar.setDrawMode(PlayerDrawMode.TPS);
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            var newFrame = this.calulcateFrame(new InputFrame(), delta);
            this.execFrame(newFrame);
            lastFrame = newFrame;
        }

        [Authority]
        public override void onPuppetUpdate(string message)
        {
            var puppetFrame = FPS.Game.Utils.NetworkCompressor.Decompress<CalculatedPuppetFrame>(message);
            this.DoTeleport(puppetFrame.origin);
            this.DoRotate(puppetFrame.rotation);
            this.playerChar.MotionVelocity = puppetFrame.velocity;
            this.playerChar.setAnimationState(puppetFrame.currentAnimation);
            this.playerChar.setAnimationTimeScale(puppetFrame.currentAnimationTime);
        }

    }
}