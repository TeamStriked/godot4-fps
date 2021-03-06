using System.Linq;
using Godot;
using System;
using System.Collections.Generic;

namespace FPS.Game.Logic.Level
{
    public partial class GameLevel : Node
    {
        [Export]
        public NodePath spwanPointsPath = null;

        public GameSpwanPoint findFreeSpwanPoint()
        {
            return this.SpwanPoints.FirstOrDefault(df => !df.inUsage);
        }

        public List<GameSpwanPoint> SpwanPoints
        {
            get
            {
                List<GameSpwanPoint> results = new List<GameSpwanPoint>();

                var spwanPointNode = GetNode(spwanPointsPath);
                if (spwanPointNode == null)
                    return results;

                foreach (var value in spwanPointNode.GetChildren())
                {
                    if (value is GameSpwanPoint)
                    {
                        results.Add(value as GameSpwanPoint);
                    }
                }

                return results;
            }
        }
    }
}