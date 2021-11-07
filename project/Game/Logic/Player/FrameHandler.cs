using Godot;
using System;
using FPS.Game.Utils;
using System.Collections.Generic;
using FPS.Game.Config;

namespace FPS.Game.Logic.Player
{
    [Serializable]
    public class CalculatedFrame
    {
        public bool onZoom;
        public bool crouching;
        public bool shifting;
        public bool sprinting;
        public bool prone;
        public bool jumpLocked;
        public Vector2 direction;
        public Vector2 mouseMotion;


        public Vector3 velocity;

        public ulong timestamp;

    }


    [Serializable]
    public class CalculatedServerFrame
    {
        public Vector3 origin;

        public Vector3 velocity;

        public Vector3 rotation;

        public ulong timestamp;

        public int currentAnimation;

        public float currentAnimationTime;

    }

}