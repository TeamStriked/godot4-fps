using Godot;
using System.Collections.Generic;
using System.Linq;
namespace FPS.Game.Logic.Weapon
{

    public partial class WeaponHolder : Node3D
    {

        List<string> weaponList = new List<string>();
        string currentGun = null;
        public override void _Ready()
        {
            foreach (var child in this.GetChildren())
            {
                if (child is Node3D)
                {
                    weaponList.Add((child as Node3D).Name);
                }
            }

            ShowGun(weaponList.FirstOrDefault());
        }

        public void ShowGun(string name)
        {
            currentGun = null;
            foreach (var child in this.GetChildren())
            {
                if (child is Node3D)
                {
                    var weapon = (child as Node3D);
                    var activeGun = weapon.Name == name ? true : false;

                    weapon.Visible = activeGun;

                    if (activeGun)
                        currentGun = weapon.Name;
                }
            }
        }

        public void nextGun()
        {
            if (currentGun != null)
            {
                var currentIndex = this.weaponList.IndexOf(currentGun);
                if (currentIndex + 1 >= this.weaponList.Count)
                {
                    ShowGun(this.weaponList.FirstOrDefault());
                }
                else
                {
                    var newWeapon = this.weaponList[currentIndex + 1];
                    ShowGun(newWeapon);
                }
            }
        }
        public void prevGun()
        {
            if (currentGun != null)
            {
                var currentIndex = this.weaponList.IndexOf(currentGun);
                if (currentIndex - 1 < 0)
                {
                    ShowGun(this.weaponList.LastOrDefault());
                }
                else
                {

                    var newWeapon = this.weaponList[currentIndex - 1];
                    ShowGun(newWeapon);

                }
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