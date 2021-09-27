using System.Linq;
using Godot;
using System;
using System.Collections.Generic;

namespace Game.Logic.Level
{
    public partial class GameLevel : Node
    {
        [Export]
        public NodePath spwanPointsPath = null;

        public GameSpwanPoint findFreeSpwanPoint(){

            return this.SpwanPoints[0];
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