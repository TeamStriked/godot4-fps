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

        public LinkedList<CalculatedServerFrame> incomingServerFrames = new LinkedList<CalculatedServerFrame>();

        public ulong startTimestamp = 0;

        public ulong interpolationDelay = 0;

        public override bool isServerPlayer()
        {
            return false;
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            if (!isActivated)
                return;

            //in case it is the first element, set the startup timestamp to count up
            var firstElement = this.incomingServerFrames.FirstOrDefault();
            if (startTimestamp <= 0 && firstElement != null)
            {
                startTimestamp = firstElement.timestamp;
            }

            ulong pastTick = startTimestamp - interpolationDelay;
            var fromNode = this.incomingServerFrames.First;
            var toNode = fromNode.Next;

            while (toNode != null && toNode.Value.timestamp <= pastTick)
            {
                fromNode = toNode;
                toNode = fromNode.Next;
                this.incomingServerFrames.RemoveFirst();
            }

            CalculatedServerFrame newValue = null;
            if (toNode != null)
                newValue = Interpolate(fromNode.Value, toNode.Value, pastTick);
            else
                newValue = fromNode.Value;

            this.ExecPuppetFrame(newValue);
            startTimestamp++;
        }

        public void IncomingServerFrame(CalculatedServerFrame puppetFrame)
        {
            var last = incomingServerFrames.LastOrDefault();
            if (last != null && last.timestamp > puppetFrame.timestamp)
            {
                GD.Print("Skipp puppet input, because receiver newer one at first.");
                return;
            }

            this.incomingServerFrames.AddLast(puppetFrame);
        }

        public void ExecPuppetFrame(CalculatedServerFrame puppetFrame)
        {
            this.DoTeleport(puppetFrame.origin);
            this.DoRotate(puppetFrame.rotation);

            this.playerChar.MotionVelocity = puppetFrame.velocity;
            //  this.playerChar.setAnimationState(puppetFrame.currentAnimation, false);
            this.playerChar.setAnimationTimeScale(puppetFrame.currentAnimationTime);
            this.playerChar.MoveAndSlide();

            this.doFootsteps();
        }

        public static CalculatedServerFrame Interpolate(CalculatedServerFrame from, CalculatedServerFrame to, ulong clientTick)
        {
            float t = ((float)(clientTick - from.timestamp)) / (to.timestamp - from.timestamp);

            return new CalculatedServerFrame
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