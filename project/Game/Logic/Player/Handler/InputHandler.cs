using Godot;
using System;
using FPS.Game.Utils;
using System.Collections.Generic;
using FPS.Game.Config;

namespace FPS.Game.Logic.Player.Handler
{
    [Serializable]
    public struct InputFrame
    {
        public Vector2 direction;
        public Vector2 mouseMotion;
        public bool onCrouching;
        public bool onProne;
        public bool onShifting;
        public bool onSprinting;
        public bool onJumpStart;

    }
    public static class InputHandler
    {
        public static InputFrame getInputFrame()
        {
            var newFrame = new InputFrame();

            newFrame.direction = Vector2.Zero;

            //movement inputs
            if (Input.IsActionPressed("game_moveForward"))
                newFrame.direction.y -= 1f;
            if (Input.IsActionPressed("game_moveBackward"))
                newFrame.direction.y += 1f;

            if (Input.IsActionPressed("game_moveLeft"))
                newFrame.direction.x -= 1f;
            if (Input.IsActionPressed("game_moveRight"))
                newFrame.direction.x += 1f;


            if (Input.IsActionPressed("game_crouching"))
                newFrame.onCrouching = true;
            else
                newFrame.onCrouching = false;

            if (Input.IsActionPressed("game_prone"))
                newFrame.onProne = true;
            else
                newFrame.onProne = false;

            if (Input.IsActionPressed("game_shifting"))
                newFrame.onShifting = true;
            else
                newFrame.onShifting = false;

            if (Input.IsActionPressed("game_sprinting"))
                newFrame.onSprinting = true;
            else
                newFrame.onSprinting = false;

            if (Input.IsActionJustPressed("game_jumpUp"))
                newFrame.onJumpStart = true;
            else
                newFrame.onJumpStart = false;

            return newFrame;
        }
    }
}