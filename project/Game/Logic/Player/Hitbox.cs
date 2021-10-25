using Godot;
using System;
using FPS.Game.Utils;
using System.Collections.Generic;
using FPS.Game.Config;

namespace FPS.Game.Logic.Player
{

    public partial class Hitbox : Area3D
    {
        [Export]
        float damageInPercentage = 100;

        public override void _Ready()
        {

        }

    }

}