/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Auto Bag Renamer", "VisEntities", "1.0.0")]
    [Description("Automatically renames sleeping bags based on their location and surrounding biome.")]
    public class AutoBagRenamer : RustPlugin
    {
        #region Fields

        private static AutoBagRenamer _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Rename Based On Grid")]
            public bool RenameBasedOnGrid { get; set; }

            [JsonProperty("Rename Based On Biome")]
            public bool RenameBasedOnBiome { get; set; }

            [JsonProperty("Bag Name Format")]
            public string BagNameFormat { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                RenameBasedOnGrid = true,
                RenameBasedOnBiome = true,
                BagNameFormat = "{grid} - {biome}"
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (planner == null || gameObject == null)
                return;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return;

            if (!PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return;

            BaseEntity entity = gameObject.ToBaseEntity();
            if (entity == null || !(entity is SleepingBag))
                return;

            NextTick(() =>
            {
                if (entity != null)
                {
                    string grid = string.Empty;
                    string biome = string.Empty;

                    if (_config.RenameBasedOnGrid)
                    {
                        grid = PhoneController.PositionToGridCoord(entity.transform.position);
                    }

                    if (_config.RenameBasedOnBiome)
                    {
                        biome = GetBiome(entity.transform.position);
                    }

                    string newName = _config.BagNameFormat
                        .Replace("{grid}", grid)
                        .Replace("{biome}", biome);

                    SleepingBag sleepingBag = entity as SleepingBag;
                    if (sleepingBag != null && !string.IsNullOrEmpty(newName))
                    {
                        sleepingBag.niceName = newName.Trim(' ', '-');
                    }
                }
            });
        }

        #endregion Oxide Hooks

        #region Biome Retrieval

        private string GetBiome(Vector3 position)
        {
            TerrainBiome.Enum biome = (TerrainBiome.Enum)TerrainMeta.BiomeMap.GetBiomeMaxType(position);
            switch (biome)
            {
                case TerrainBiome.Enum.Arctic:
                    return "Arctic";
                case TerrainBiome.Enum.Tundra:
                    return "Tundra";
                case TerrainBiome.Enum.Temperate:
                    return "Temperate";
                case TerrainBiome.Enum.Arid:
                    return "Arid";
                default:
                    return "Unnamed Bag";
            }
        }

        #endregion Biome Retrieval

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "autobagrenamer.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permissions
    }
}