using System.Threading;
using Godot;
using System;
namespace FPS.Game.UI
{


    public partial class GameLog : CanvasLayer
    {
        public override void _Ready()
        {
            FPS.Game.Utils.Logger.OnMessageListUpdate += updateList;
        }

        public void updateList()
        {
            GetNode<RichTextLabel>("log").Text = FPS.Game.Utils.Logger.lastMessages.ToArray().Join(System.Environment.NewLine);
        }
    }

}