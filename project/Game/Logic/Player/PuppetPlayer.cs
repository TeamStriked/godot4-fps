using Godot;
using System;
using FPS.Game.Logic.Player.Handler;
using System.Collections.Generic;
using System.Linq;
namespace FPS.Game.Logic.Player
{
    public partial class PuppetPlayer : NetworkPlayer
    {
        public int InterpolationDelay = 12;

        public Queue<CalculatedPuppetFrame> incomingServerFrames = new Queue<CalculatedPuppetFrame>();

        public override bool isServerPlayer()
        {
            return false;
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            if (!isActivated)
                return;

            if (incomingServerFrames.Count > 0)
            {
                var q = incomingServerFrames.Dequeue();
                this.ExecPuppetFrame(q);
            }

            if (this.playerChar.getSpeed() > this.walkSpeed)
            {
                this.playerChar.doFootstep();
            }
        }

        public void IncomingServerFrame(CalculatedPuppetFrame puppetFrame)
        {
            this.incomingServerFrames.Enqueue(puppetFrame);
        }

        public void ExecPuppetFrame(CalculatedPuppetFrame puppetFrame)
        {
            this.DoTeleport(puppetFrame.origin);
            this.DoRotate(puppetFrame.rotation);
            this.playerChar.MotionVelocity = puppetFrame.velocity;
            this.playerChar.setAnimationState(puppetFrame.currentAnimation);
            this.playerChar.setAnimationTimeScale(puppetFrame.currentAnimationTime);
        }

        public static CalculatedPuppetFrame Interpolate(CalculatedPuppetFrame from, CalculatedPuppetFrame to, int clientTick)
        {
            float t = ((float)(clientTick - from.timestamp)) / (to.timestamp - from.timestamp);

            return new CalculatedPuppetFrame
            {
                origin = from.origin.Lerp(to.origin, t),
                rotation = from.rotation.Lerp(to.rotation, t),
                timestamp = 0
            };
        }

        public override void Activate()
        {
            this.playerChar.ProcessMode = ProcessModeEnum.Always;
            this.playerChar.setCameraMode(PlayerCameraMode.NONE);
            this.playerChar.setDrawMode(PlayerDrawMode.TPS);
            this.isActivated = true;

        }

    }
}