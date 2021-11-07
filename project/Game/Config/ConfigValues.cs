using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Godot;
namespace FPS.Game.Config
{
    public static class ConfigValues
    {

        const string keysSectionName = "keys";
        const string mouseSettingSectionName = "mouse";
        const string audioSettingsSectionName = "audio";
        const string videoSettingsSectionName = "video";
        static public float sensitivityX = 20.0f;
        static public float sensitivityY = 20.0f;

        static public float masterVolume = 1.0f;

        static public float fov = 65f;

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

        static public void setFov(float x)
        {
            fov = x;
            saveConfig();
        }

        static public void setSensitivityY(float y)
        {
            sensitivityY = y;
            saveConfig();
        }

        static public void setMasterVolume(float y)
        {
            masterVolume = y;

            var bus = AudioServer.GetBusIndex("Master");
            AudioServer.SetBusVolumeDb(bus, linear2db(masterVolume));

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

            cfg.SetValue(mouseSettingSectionName, "sensitivityX", sensitivityX);
            cfg.SetValue(mouseSettingSectionName, "sensitivityY", sensitivityY);
            cfg.SetValue(audioSettingsSectionName, "masterVolume", masterVolume);
            cfg.SetValue(mouseSettingSectionName, "fov", fov);

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

        static void parseValues(Godot.ConfigFile cfg, string section, string key, ref float value)
        {
            var cfgValue = cfg.GetValue(section, key);
            if (cfgValue != null)
            {
                try
                {
                    value = float.Parse(cfgValue.ToString());
                }
                catch
                {
                    FPS.Game.Utils.Logger.LogError("Cant parse " + key);
                }
            }
        }

        static void loadMovement()
        {
            loadDefaultMovementSettings();

            var cfg = new Godot.ConfigFile();
            if (cfg.Load("user://settings.cfg") == Error.Ok)
            {
                parseValues(cfg, mouseSettingSectionName, "sensitivityX", ref sensitivityX);
                parseValues(cfg, mouseSettingSectionName, "sensitivityY", ref sensitivityY);
                parseValues(cfg, mouseSettingSectionName, "fov", ref fov);
                parseValues(cfg, audioSettingsSectionName, "masterVolume", ref masterVolume);
            }

            var bus = AudioServer.GetBusIndex("Master");
            AudioServer.SetBusVolumeDb(bus, linear2db(masterVolume));
        }


        static float db2linear(float p_db) { return Mathf.Exp(p_db * 0.11512925464970228420089957273422f); }
        static float linear2db(float p_linear) { return Mathf.Log(p_linear) * 8.6858896380650365530225783783321f; }


        static public void loadSettings()
        {
            loadKeys();
            loadMovement();
        }
    }
}