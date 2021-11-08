using Godot;
using System;
using FPS.Game.Logic.Player;
public partial class WeaponHolderPosition : Node3D
{

    private float timer = 0.0f;
    float bobbingSpeed = 0.04f;
    float bobbingAmount = 0.05f;
    private float xInit, yInit;
    public float xOffset, yOffset;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        xInit = Position.x;
        yInit = Position.y;

        xOffset = xInit;
        yOffset = yInit;
    }
    public void Reset()
    {
        xOffset = xInit;
        yOffset = yInit;
    }


    public override void _Process(float delta)
    {
        base._Process(delta);
        float xMovement = 0.0f;
        float yMovement = 0.0f;
        float horizontal = Input.GetActionRawStrength("game_moveRight") - Input.GetActionRawStrength("game_moveLeft");
        float vertical = Input.GetActionRawStrength("game_moveBackward") - Input.GetActionRawStrength("game_moveForward");

        var cam = this.GetParent<Camera3D>();
        var head = cam.GetParent<Node3D>();
        var character = head.GetParent<CharacterInstance>();

        Vector3 calcPosition = Position;
        var scale = character.getSpeed() / 6.5f;

        if (Mathf.Abs(horizontal) == 0 && Mathf.Abs(vertical) == 0)
        {
            timer = 0.0f;
        }
        else
        {
            xMovement = Mathf.Sin(timer) / 2;
            yMovement = -Mathf.Sin(timer);

            // if (!controller.IsAiming && controller.IsRunning)
            //  {
            //      timer += bobbingSpeed * 1.2f;
            //      xMovement *= 1.5f;
            //      yMovement *= 1.5f;
            //  }
            // else
            //   {

            timer += bobbingSpeed * scale;
            //  }

            if (timer > Mathf.Pi * 2)
            {
                timer = timer - (Mathf.Pi * 2);
            }
        }

        var boobing = bobbingAmount * scale;
        if (xMovement != 0)
        {
            float translateChange = xMovement * boobing;
            float totalAxes = Mathf.Abs(horizontal) + Mathf.Abs(vertical);
            totalAxes = Mathf.Clamp(totalAxes, 0.0f, 1.0f);
            translateChange = totalAxes * translateChange;

            calcPosition.x = xOffset + translateChange;
        }
        else
        {
            calcPosition.x = xOffset;
        }

        if (yMovement != 0)
        {
            float translateChange = yMovement * boobing;
            float totalAxes = Mathf.Abs(horizontal) + Mathf.Abs(vertical);
            totalAxes = Mathf.Clamp(totalAxes, 0.0f, 1.0f);
            translateChange = totalAxes * translateChange;

            calcPosition.y = yOffset + translateChange;
        }
        else
        {
            calcPosition.y = yOffset;
        }

        Position = Position.Lerp(calcPosition, delta);
    }

    Vector2 mouseMotion = Vector2.Zero;

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventMouseMotion)
        {
            mouseMotion = -(@event as InputEventMouseMotion).Relative;
        }
    }
}
