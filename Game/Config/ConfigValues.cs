using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Godot;
namespace FPS.Game.Config
{
    public static class ConfigValues
    {

        const string keysSectionName = "movement_keys";
        const string keySettingSectionName = "movement_settings";
        static public float sensitivityX = 20.0f;
        static public float sensitivityY = 20.0f;

        public static Dictionary<string, Godot.Key> keys = new Dictionary<string, Godot.Key>();

        static public Key getKeyValue(string name)
        {
            return keys[name];
        }

        static void loadDefaultMovementSettings()
        {
            sensitivityX = 20f;
            sensitivityY = 20f;
        }

        static void loadDefaultKeys()
        {
            ConfigValues.keys.Clear();

            foreach (var key in InputMap.GetActions())
            {
                if (!key.ToString().StartsWith("game_"))
                    continue;

                foreach (var action in InputMap.ActionGetEvents(key.ToString()))
                {
                    if (action is InputEventKey)
                    {
                        var rightAction = action as InputEventKey;
                        ConfigValues.keys.Add(key.ToString().Replace("game_", ""), rightAction.Keycode);
                    }
                }
            }
        }

        static public void storeKey(string keyName, Key value)
        {
            keys[keyName] = value;
            saveConfig();
            saveMap();
        }


        static public void setSensitivityX(float x)
        {
            sensitivityX = x;
            saveConfig();
        }

        static public void setSensitivityY(float y)
        {
            sensitivityY = y;
            saveConfig();
        }

        static void saveMap()
        {
            foreach (var key in keys)
            {
                InputMap.ActionEraseEvents("game_" + key.Key.ToString());
                var keyEvent = new InputEventKey();
                keyEvent.Keycode = key.Value;
                InputMap.ActionAddEvent("game_" + key.Key.ToString(), keyEvent);
            }
        }

        static void saveConfig()
        {
            var cfg = new Godot.ConfigFile();
            foreach (var defaultKey in keys)
            {
                cfg.SetValue(keysSectionName, defaultKey.Key, defaultKey.Value.ToString());
            }

            cfg.SetValue(keySettingSectionName, "sensitivityX", sensitivityX);
            cfg.SetValue(keySettingSectionName, "sensitivityY", sensitivityY);

            cfg.Save("user://settings.cfg");
        }

        static void loadKeys()
        {
            loadDefaultKeys();

            var cfg = new Godot.ConfigFile();
            if (cfg.Load("user://settings.cfg") == Error.Ok)
            {
                foreach (var key in keys.Keys.ToList())
                {
                    var origValue = cfg.GetValue(keysSectionName, key);
                    if (origValue != null)
                    {
                        try
                        {
                            var newValue = (Key)Key.Parse(typeof(Key), origValue.ToString());
                            keys[key] = newValue;
                        }
                        catch
                        {
                            FPS.Game.Utils.Logger.LogError("Cant parse " + key);
                        }
                    }
                }
            }

            saveMap();
        }

        static void loadMovement()
        {
            loadDefaultMovementSettings();

            var cfg = new Godot.ConfigFile();
            if (cfg.Load("user://settings.cfg") == Error.Ok)
            {
                var origValueX = cfg.GetValue(keySettingSectionName, "sensitivityX");
                if (origValueX != null)
                {
                    try
                    {
                        sensitivityX = float.Parse(origValueX.ToString());
                    }
                    catch
                    {
                        FPS.Game.Utils.Logger.LogError("Cant parse  sensitivity");
                    }
                }

                var origValueY = cfg.GetValue(keySettingSectionName, "sensitivityY");
                if (origValueY != null)
                {
                    try
                    {
                        sensitivityY = float.Parse(origValueY.ToString());
                    }
                    catch
                    {
                        FPS.Game.Utils.Logger.LogError("Cant parse sensitivityY");
                    }
                }
            }
        }

        static public void loadSettings()
        {
            loadKeys();
            loadMovement();
        }
    }
}