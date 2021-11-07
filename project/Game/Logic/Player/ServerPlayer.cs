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

        public InputFrame lastFrame = new InputFrame();


        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            if (!isActivated)
                return;

            if (inputQueue.Count > 0)
            {
                bool onPositionChange = false;
                while (inputQueue.Count > 0)
                {
                    var lastInput = inputQueue.Dequeue();

                    if (lastInput.timestamp <= lastFrame.timestamp)
                        continue;

                    this.playerChar.SetCharRotation(lastInput.mouseMotion.x);
                    this.playerChar.SetHeadRotation(lastInput.mouseMotion.y);

                    var newFrame = this.calulcateFrame(lastInput);
                    this.execFrame(newFrame);

                    handleAnimation();
                    this.lastFrame = lastInput;
                    onPositionChange = true;
                }

                if (onPositionChange)
                {
                    var sendMessage = FPS.Game.Utils.NetworkCompressor.Compress(this.getCurrentServerFrame(this.lastFrame.timestamp));
                    RpcId(networkId, "onServerInput", sendMessage);

                    this.world.updateAllPuppets(networkId, sendMessage);
                }

            }

        }

        [AnyPeer]
        public override void onClientInput(string inputMessage)
        {
            var lastInputs = FPS.Game.Utils.NetworkCompressor.Decompress<List<InputFrame>>(inputMessage);
            lastInputs.ForEach(o => inputQueue.Enqueue(o));
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