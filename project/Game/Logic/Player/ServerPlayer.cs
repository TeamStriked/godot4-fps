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

        public override void _PhysicsProcess(float delta)
        {
            if (!isActivated || !calculated)
                return;

            base._PhysicsProcess(delta);

            while (inputQueue.Count > 0)
            {
                var lastInput = inputQueue.Dequeue();
                this.playerChar.SetCharRotation(lastInput.mouseMotion.x);
                this.playerChar.SetHeadRotation(lastInput.mouseMotion.y);

                var newFrame = this.calulcateFrame(lastInput, delta);
                this.execFrame(newFrame);

                lastFrame = newFrame;

                handleAnimation();
            }

            var puppetFrame = new CalculatedPuppetFrame();
            puppetFrame.origin = this.playerChar.GlobalTransform.origin;
            puppetFrame.rotation = this.playerChar.GlobalTransform.basis.GetEuler();
            puppetFrame.velocity = this.playerChar.MotionVelocity;
            puppetFrame.currentAnimation = this.playerChar.getAnimationState();
            puppetFrame.currentAnimationTime = this.playerChar.getAnimationScale();

            var sendMessage = FPS.Game.Utils.NetworkCompressor.Compress(puppetFrame);
            RpcId(networkId, "onServerInput", sendMessage);

            this.world.updateAllPuppets(networkId, sendMessage);
        }

        [AnyPeer]
        public override void onClientInput(string inputMessage)
        {
            calculated = true;
            var lastInput = FPS.Game.Utils.NetworkCompressor.Decompress<InputFrame>(inputMessage);
            inputQueue.Enqueue(lastInput);
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