using System.Threading;
using Godot;
using System;
namespace FPS.Game.UI
{

    public partial class GameGraph : CanvasLayer
    {
        Label _label = null;

        public static double inTraffic = 0;
        public static double outTraffic = 0;
        public static float serverPosDifference = 0;

        public override void _Ready()
        {
            this._label = GetNode("Content") as Label;
        }

        public override void _PhysicsProcess(float delta)
        {
            var memBytes = OS.GetStaticMemoryUsage();
            var vidMemBytes = (float)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed);

            this._label.Text = " Mem: " + Convert(memBytes) + " NetIN: " + Convert((float)inTraffic) + "/s NetOut: " + Convert((float)outTraffic) + "/s Diff: " + serverPosDifference + " VMem: " + Convert(vidMemBytes) + " FPS: " + Performance.GetMonitor(Performance.Monitor.TimeFps) + " Objects:" + Performance.GetMonitor(Performance.Monitor.ObjectCount) + " Resources:" + Performance.GetMonitor(Performance.Monitor.ObjectResourceCount) + " Nodes:" + Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
        }

        string Convert(float bytes)
        {
            string[] Group = { "Bytes", "KB", "MB", "GB", "TB" };
            float B = bytes; int G = 0;
            while (B >= 1024 && G < 5)
            {
                B /= 1024;
                G += 1;
            }
            float truncated = (float)(Math.Truncate((double)B * 100.0) / 100.0);
            string load = (truncated + " " + Group[G]);

            return load;
        }
    }

}