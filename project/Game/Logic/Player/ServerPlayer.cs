using Godot;
using System;
using FPS.Game.Logic.Player.Handler;
using System.Collections.Generic;
using FPS.Game.Logic.Server;
using System.Linq;

namespace FPS.Game.Logic.Player
{
    public partial class ServerPlayer : NetworkPlayer
    {
        bool calculated = false;

        public override void DoFire(Weapon.Weapon weapon)
        {
            weapon.FireGun();

            var rayCast = this.playerChar.getRaycast3D();
            if (rayCast.IsColliding())
            {
                var collider = rayCast.GetCollider();
                if (collider is StaticBody3D)
                {
                    FPS.Game.Utils.Logger.InfoDraw("Hit wall at " + rayCast.GetCollisionPoint());
                    this.world.sendDecalsToClients(rayCast.GetCollisionPoint(), rayCast.GetCollisionNormal());
                }
                else if (collider is Hitbox)
                {
                    var name = (collider as Hitbox).Name;
                    FPS.Game.Utils.Logger.InfoDraw("Hit hitbox " + name + " on " + rayCast.GetCollisionPoint());
                }
            }
        }

        public override bool isServerPlayer()
        {
            return true;
        }

        Queue<InputFrame> inputQueue = new Queue<InputFrame>();

        public int serverTick = 0;

        public InputFrame lastFrame = new InputFrame();

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            if (!isActivated || !calculated)
                return;

            serverTick++;

            if (inputQueue.Count > 0)
            {
                while (inputQueue.Count > 0)
                {
                    var lastInput = inputQueue.Dequeue();
                    this.playerChar.SetCharRotation(lastInput.mouseMotion.x);
                    this.playerChar.SetHeadRotation(lastInput.mouseMotion.y);

                    var newFrame = this.calulcateFrame(lastInput, delta);
                    this.execFrame(newFrame);
                    this.AppendCalculatedFrame(newFrame);

                    handleAnimation();

                    this.lastFrame = lastInput;

                    var puppetFrame = new CalculatedPuppetFrame();
                    puppetFrame.timestamp = serverTick;
                    puppetFrame.origin = this.playerChar.GlobalTransform.origin;
                    puppetFrame.rotation = this.playerChar.GlobalTransform.basis.GetEuler();
                    puppetFrame.velocity = this.playerChar.MotionVelocity;
                    puppetFrame.currentAnimation = this.playerChar.getAnimationState();
                    puppetFrame.currentAnimationTime = this.playerChar.getAnimationScale();
                    var sendMessagePuppet = FPS.Game.Utils.NetworkCompressor.Compress(puppetFrame);
                    this.world.updateAllPuppets(networkId, sendMessagePuppet);
                }

                var clientFrame = new CalculatedPuppetFrame();
                clientFrame.timestamp = serverTick;
                clientFrame.origin = this.playerChar.GlobalTransform.origin;
                clientFrame.rotation = this.playerChar.GlobalTransform.basis.GetEuler();
                clientFrame.velocity = this.playerChar.MotionVelocity;
                clientFrame.currentAnimation = this.playerChar.getAnimationState();
                clientFrame.currentAnimationTime = this.playerChar.getAnimationScale();

                var sendMessage = FPS.Game.Utils.NetworkCompressor.Compress(clientFrame);
                RpcId(networkId, "onServerInput", sendMessage);
            }
            else
            {
                var newFrame = this.calulcateFrame(this.lastFrame, delta);
                this.execFrame(newFrame);

            }
        }

        [AnyPeer]
        public override void onClientInput(string inputMessage)
        {
            calculated = true;
            var lastInputs = FPS.Game.Utils.NetworkCompressor.Decompress<List<InputFrame>>(inputMessage);
            foreach (var item in lastInputs)
            {
                inputQueue.Enqueue(item);
            }
        }

        public override void Activate()
        {
            this.playerChar.ProcessMode = ProcessModeEnum.Always;
            this.playerChar.setCameraMode(PlayerCameraMode.NONE);
            this.playerChar.setDrawMode(PlayerDrawMode.NONE);
            this.isActivated = true;
        }

    }
}