using Godot;
using FPS.Game.Config;

namespace Game.bootloader
{
    public partial class Autoload : Node
    {
        public override void _Ready()
        {
            ConfigValues.loadSettings();
        }
    }
}