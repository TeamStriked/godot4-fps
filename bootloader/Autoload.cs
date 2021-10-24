using Godot;
using FPS.Game.Config;
using System;

namespace Game.bootloader
{
    public partial class Autoload : Node
    {
        public override void _Ready()
        {
            ConfigValues.loadSettings();
        }


        public override void _Process(float delta)
        {
            base._Process(delta);
            //GC.Collect(GC.MaxGeneration);
            //GC.WaitForPendingFinalizers();
        }


    }
}