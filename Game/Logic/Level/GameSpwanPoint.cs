using Godot;
using System;
using Game.Logic.Team;
namespace Game.Logic.Level
{
    public partial class GameSpwanPoint : Position3D
    {
        [Export]
        public TeamTypes teamType;
    }
}