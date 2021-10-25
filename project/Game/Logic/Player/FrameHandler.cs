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
        public bool crouching;
        public bool shifting;
        public bool sprinting;
        public bool prone;
        public bool jumpLocked;
        public Vector2 direction;

        public Vector3 velocity;

    }


    [Serializable]
    public class CalculatedPuppetFrame
    {
        public Vector3 origin;

        public Vector3 velocity;

        public Vector3 rotation;

        public int timestamp;

        public string currentAnimation;

        public float currentAnimationTime;

    }

}