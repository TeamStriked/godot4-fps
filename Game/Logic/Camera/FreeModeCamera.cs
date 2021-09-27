using Godot;
using System;

namespace Game.Logic.Camera
{
    [Tool]
    public partial class FreeModeCamera : Camera3D
    {
        [Export]
        float sensitivity = 0.45f;

        [Export]
        public bool activated = false;

        Vector2 _mouse_position = new Vector2(0.0f, 0.0f);
        float _total_pitch = 0.0f;

        // Movement state
        Vector3 _direction = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 _velocity = new Vector3(0.0f, 0.0f, 0.0f);
        float _acceleration = 30f;
        float _deceleration = -10f;
        float _vel_multiplier = 4f;

        // Keyboard state
        bool _w = false;
        bool _s = false;
        bool _a = false;
        bool _d = false;
        bool _q = false;
        bool _e = false;
        
        bool _wasClicked = false;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            
        }

        public override void _Process(float delta)
        {
            base._Process(delta);

            if(!activated)
                return;

            _update_mouselook();
            _update_movement(delta);
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if(!activated)
                return;

            if (@event is InputEventMouseMotion)
            {
                var ev = @event as InputEventMouseMotion;
                _mouse_position = ev.Relative;
            }

            if (@event is InputEventMouseButton)
            {
                var ev = @event as InputEventMouseButton;
                switch (ev.ButtonIndex)
                {
                    case MouseButton.Left:
                        _wasClicked = !_wasClicked;
                        Input.SetMouseMode(_wasClicked ? Input.MouseMode.Captured : Input.MouseMode.Visible);

                        break;
                    case MouseButton.WheelUp:
                        _vel_multiplier = Math.Clamp(_vel_multiplier * 1.1f, 0.2f, 20f); break;
                    case MouseButton.WheelDown:
                        _vel_multiplier = Math.Clamp(_vel_multiplier / 1.1f, 0.2f, 20f); break;
                }
                
            }

            // Receives key input
            if (@event is InputEventKey)
            {
                var ev = @event as InputEventKey;

                switch (ev.Keycode)
                {
                    case Key.W:
                        _w = ev.Pressed; break;
                    case Key.Escape:
                        _wasClicked = false;
                        Input.SetMouseMode(Input.MouseMode.Visible);
                        break;
                    case Key.S:
                        _s = ev.Pressed; break;
                    case Key.A:
                        _a = ev.Pressed; break;
                    case Key.D:
                        _d = ev.Pressed; break;
                    case Key.Q:
                        _q = ev.Pressed; break;
                    case Key.E:
                        _e = ev.Pressed; break;
                }
            }
        }

        //Updates camera movement
        private void _update_movement(float delta)
        {
            // Computes desired direction from key states
            _direction = new Vector3(Convert.ToSingle(_d) - Convert.ToSingle(_a),
                                 Convert.ToSingle(_e) - Convert.ToSingle(_q),
                                 Convert.ToSingle(_s) - Convert.ToSingle(_w));

            // Computes the change in velocity due to desired direction and "drag"
            // The "drag" is a constant acceleration on the camera to bring it's velocity to 0
            var offset = _direction.Normalized() * _acceleration * _vel_multiplier * delta + _velocity.Normalized() * _deceleration * _vel_multiplier * delta;

            // Checks if we should bother translating the camera
            if (_direction == Vector3.Zero && offset.LengthSquared() > _velocity.LengthSquared())
            {
                // Sets the velocity to 0 to prevent jittering due to imperfect deceleration
                _velocity = Vector3.Zero;
            }
            else
            {
                //Clamps speed to stay within maximum value (_vel_multiplier)
                _velocity.x = Mathf.Clamp(_velocity.x + offset.x, -_vel_multiplier, _vel_multiplier);
                _velocity.y = Mathf.Clamp(_velocity.y + offset.y, -_vel_multiplier, _vel_multiplier);
                _velocity.z = Mathf.Clamp(_velocity.z + offset.z, -_vel_multiplier, _vel_multiplier);

                Translate(_velocity * delta);
            }
        }

        // Updates mouse look 
        private void _update_mouselook()
        {
            //Only rotates mouse if the mouse is captured
            if (Input.GetMouseMode() == Input.MouseMode.Captured)
            {
                _mouse_position *= sensitivity;

                var yaw = _mouse_position.x;

                var pitch = _mouse_position.y;
                _mouse_position = new Vector2(0, 0);
                // Prevents looking up/down too far
                pitch = Math.Clamp(pitch, -90 - _total_pitch, 90 - _total_pitch);
                _total_pitch += pitch;

                RotateY(Mathf.Deg2Rad(-yaw));
                RotateObjectLocal(new Vector3(1, 0, 0), Mathf.Deg2Rad(-pitch));
            }


        }

    }
}