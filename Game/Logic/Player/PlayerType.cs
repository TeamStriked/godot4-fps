using Godot;
using System;
using FPS.Game.Logic.Player.Handler;
using System.Collections.Generic;

namespace FPS.Game.Logic.Player
{
    [Serializable]
    public enum PlayerType
    {
        Local,
        Puppet,
        Server
    }
}