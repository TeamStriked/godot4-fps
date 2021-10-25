using Godot;
using System.Collections.Generic;
using System.Linq;
namespace FPS.Game.Logic.Weapon
{

    public partial class WeaponHolder : Node3D
    {

        List<string> weaponList = new List<string>();
        public Weapon currentGun = null;
        private int currentWeaponIndex = 0;

        public override void _Ready()
        {
            foreach (var child in this.GetChildren())
            {
                if (child is Node)
                {
                    weaponList.Add((child as Node).Name);
                }
            }

            ShowGun();
        }

        public void ShowGun()
        {
            GD.Print("new weapon index" + currentWeaponIndex);
            int weaponIndex = 0;
            foreach (var child in this.GetChildren())
            {
                if (child is Node3D)
                {
                    var weapon = (child as Node3D);
                    var activeGun = weaponIndex == currentWeaponIndex ? true : false;
                    weapon.Visible = activeGun;

                    if (activeGun == true && weapon is Weapon)
                    {
                        currentGun = weapon as Weapon;
                    }

                    weaponIndex++;
                }
            }
        }

        public void nextGun()
        {
            if (currentWeaponIndex + 1 >= this.weaponList.Count)
            {
                currentWeaponIndex = 0;
                ShowGun();
            }
            else
            {
                currentWeaponIndex += 1;
                ShowGun();
            }
        }

        public void prevGun()
        {

            if (currentWeaponIndex - 1 < 0)
            {
                currentWeaponIndex = this.weaponList.Count - 1;
                ShowGun();
            }
            else
            {
                currentWeaponIndex = currentWeaponIndex - 1;
                ShowGun();
            }

        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if (@event is InputEventMouseButton)
            {
                InputEventMouseButton emb = (InputEventMouseButton)@event;
                if (emb.IsPressed())
                {
                    if (emb.ButtonIndex == MouseButton.WheelUp)
                    {
                        this.nextGun();
                    }
                    if (emb.ButtonIndex == MouseButton.WheelDown)
                    {
                        this.prevGun();
                    }
                }
            }

            @event.Dispose();
        }
    }

}