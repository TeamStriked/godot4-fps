using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
namespace FPS.Game.Utils
{

    public static class Logger
    {

        public delegate void MessageListUpdate();
        public static event MessageListUpdate OnMessageListUpdate;
        public static List<string> lastMessages = new List<string>();
        public static void InfoDraw(string message)
        {
            GD.Print(message);
            addMessage(message);
        }

        public static void Info(string message)
        {
            GD.Print(message);
            addMessage(message);
        }

        public static void LogError(string message)
        {
            GD.PrintErr(message);
            addMessage(message);
        }

        private static void addMessage(string message)
        {
            lastMessages.Add(message);
            if (lastMessages.Count > 5)
            {
                lastMessages.RemoveRange(0, lastMessages.Count - 5);
            }
            if (OnMessageListUpdate != null)
                OnMessageListUpdate();
        }
    }
}