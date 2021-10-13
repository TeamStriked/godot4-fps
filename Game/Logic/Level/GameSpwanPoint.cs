using Godot;
using System;
using FPS.Game.Logic.Team;
namespace FPS.Game.Logic.Level
{
    public partial class GameSpwanPoint : Position3D
    {
        [Export]
        public TeamTypes teamType;

        [Export]
        public bool inUsage;
    }
}