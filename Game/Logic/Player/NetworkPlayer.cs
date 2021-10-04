using Godot;
using System;
namespace FPS.Game.Logic.Player
{
    public abstract partial class NetworkPlayer : Node3D
    {

        public override void _Ready()
        {
            RpcConfig("onNetworkTeleport", RPCMode.Any, TransferMode.Reliable);
            this.playerChar = GetNode(charPath) as CharacterInstance;
        }


        [Export]
        protected float crouchUpSpeed = 12;

        [Export]
        protected float crouchDownSpeed = 6;


        [Export]
        protected float crouchColliderMultiplier = 0.6f;

        [Export]
        protected float proneColliderMultiplier = 0.3f;


        [Export]
        protected float proneUpSpeed = 1.8f;

        [Export]
        protected float proneDownSpeed = 1.3f;


        [Export]
        protected float Friction = 15;

        [Export] protected float FrictionSpeedThreshold = 0.5f;

        [Export]
        protected float maxSpeedAir = 1.3f;

        [Export]
        protected float sprintSpeed = 12f;

        [Export]
        protected float speedRechargeMultiplier = 2f;

        [Export]
        protected float speedLooseMultiplier = 0.3f;

        [Export]
        protected float currentSpeedAmount = 1.0f;

        [Export]
        protected float walkSpeed = 2.6f;

        [Export]
        protected float proneSpeed = 0.6f;

        [Export]
        protected float defaultSpeed = 6.5f;

        [Export]
        protected float Accel = 6;

        [Export]
        protected float Deaccel = 8; //start speed

        [Export]
        protected float FlyAccel = 4; // stop speed

        [Export]
        protected float jumpForce = 8.5f;

        [Export]
        protected float jumpCrouchForce = 10.0f;

        [Export]
        protected float jumpCoolDown = 0.65f;

        [Export]
        protected float gravity = 21.0f;




        [Export]
        NodePath charPath = null;

        protected CharacterInstance playerChar = null;

        public int networkId = 0;


        [Puppet]
        public virtual void onNetworkTeleport(Vector3 origin)
        {
        }

        public virtual void DoTeleport(Vector3 origin)
        {
            var gf = this.playerChar.GlobalTransform;
            gf.origin = origin;
            this.playerChar.GlobalTransform = gf;
        }
    }
}