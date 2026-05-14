using System;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo( "TemporalProspector",
    Description = "Temporally prospect the resources of the world",
    Website     = "https://github.com/AlexDGr8r/temporal-prospector",
    Authors     = new []{ "AlexDGr8r" } )]

namespace TemporalProspector
{
    public class TemporalProspectorMod : ModSystem
    {
        private const string ModDomain = "temporalprospector";
        private const string ConfigSyncChannelName = "temporalprospectorconfig";

        private ICoreAPI coreApi;
        private ICoreClientAPI clientApi;
        private ICoreServerAPI serverApi;
        private IServerNetworkChannel serverChannel;

        private bool assetsLoaded;
        private bool configLibSubscribed;
        private bool useConfigLibRuntime;
        private object configLibModSystem;
        private Delegate configLibSettingChangedHandler;
        private Delegate configLibConfigsLoadedHandler;

        public static TemporalProspectorConfig Config { get; private set; }

        public static int MaxParticleTargets =>
            Config?.MaxParticleTargets ?? TemporalProspectorConfig.DefaultMaxParticleTargets;

        public static int GetConfiguredDurabilityCost(int radius)
        {
            double durabilityCostPercent = Config?.DurabilityCostPercent ?? TemporalProspectorConfig.DefaultDurabilityCostPercent;
            if (radius <= 0 || durabilityCostPercent <= 0)
            {
                return 0;
            }

            return (int)Math.Round(radius * durabilityCostPercent / 100d);
        }

        public static int GetConfiguredSearchRadius(int toolMode)
        {
            if (Config == null)
            {
                return GetDefaultSearchRadius(toolMode);
            }

            return toolMode switch
            {
                1 => Config.MediumSearchRadius,
                2 => Config.LongSearchRadius,
                3 => Config.ExtraLongSearchRadius,
                _ => Config.ShortSearchRadius,
            };
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            coreApi = api;
            useConfigLibRuntime = TrySubscribeToConfigLib(api);
            Config = CreateInitialConfig(api);

            if (usesFallbackConfigSync)
            {
                api.Network.RegisterChannel(ConfigSyncChannelName)
                    .RegisterMessageType<TemporalProspectorConfigPacket>();
            }

            api.RegisterItemClass("ItemTemporalProspectingPick", typeof(ItemTemporalProspectingPick));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            clientApi = api;
            if (usesFallbackConfigSync)
            {
                api.Network.GetChannel(ConfigSyncChannelName)
                    .SetMessageHandler<TemporalProspectorConfigPacket>(OnConfigSyncPacket);
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            serverApi = api;
            if (usesFallbackConfigSync)
            {
                serverChannel = api.Network.GetChannel(ConfigSyncChannelName);
                api.Event.PlayerNowPlaying += SendConfigToPlayer;
            }
        }

        public override double ExecuteOrder() => 0.55;

        public override void AssetsLoaded(ICoreAPI api)
        {
            base.AssetsLoaded(api);

            if (useConfigLibRuntime)
            {
                OnConfigLibConfigsLoaded();
            }

            ApplyDurabilityConfig(api);

            if (api.Side == EnumAppSide.Server || api is ICoreClientAPI capi && capi.IsSinglePlayer)
            {
                ApplyRecipeConfig(api);
            }

            assetsLoaded = true;
        }

        public override void Dispose()
        {
            assetsLoaded = false;

            if (serverApi != null)
            {
                serverApi.Event.PlayerNowPlaying -= SendConfigToPlayer;
            }

            if (configLibModSystem != null)
            {
                try
                {
                    Type systemType = configLibModSystem.GetType();
                    if (configLibSettingChangedHandler != null)
                    {
                        systemType.GetEvent("SettingChanged")
                            ?.RemoveEventHandler(configLibModSystem, configLibSettingChangedHandler);
                    }

                    if (configLibConfigsLoadedHandler != null)
                    {
                        systemType.GetEvent("ConfigsLoaded")
                            ?.RemoveEventHandler(configLibModSystem, configLibConfigsLoadedHandler);
                    }
                }
                catch
                {
                    // Ignore cleanup failures during shutdown.
                }
            }

            base.Dispose();
        }

        public static int GetConfiguredDurability(string itemCodePath, int fallback)
        {
            if (string.IsNullOrEmpty(itemCodePath) || Config == null)
            {
                return fallback;
            }

            return itemCodePath switch
            {
                "temporalprospectingpick-copper" => Config.CopperDurability,
                "temporalprospectingpick-tinbronze" => Config.TinBronzeDurability,
                "temporalprospectingpick-bismuthbronze" => Config.BismuthBronzeDurability,
                "temporalprospectingpick-blackbronze" => Config.BlackBronzeDurability,
                "temporalprospectingpick-gold" => Config.GoldDurability,
                "temporalprospectingpick-silver" => Config.SilverDurability,
                "temporalprospectingpick-iron" => Config.IronDurability,
                "temporalprospectingpick-meteoriciron" => Config.MeteoricIronDurability,
                "temporalprospectingpick-steel" => Config.SteelDurability,
                _ => fallback,
            };
        }

        private static int GetDefaultSearchRadius(int toolMode)
        {
            return toolMode switch
            {
                1 => TemporalProspectorConfig.DefaultMediumSearchRadius,
                2 => TemporalProspectorConfig.DefaultLongSearchRadius,
                3 => TemporalProspectorConfig.DefaultExtraLongSearchRadius,
                _ => TemporalProspectorConfig.DefaultShortSearchRadius,
            };
        }

        private bool usesFallbackConfigSync => !useConfigLibRuntime;

        private TemporalProspectorConfig CreateInitialConfig(ICoreAPI api)
        {
            if (useConfigLibRuntime)
            {
                return TemporalProspectorConfig.CreateDefault();
            }

            // Try to prevent multiplayer/server configs from touching singleplayer/local configs.
            return (api.Side == EnumAppSide.Server || api is ICoreClientAPI { IsSinglePlayer: true })
                ? TemporalProspectorConfig.LoadLocal(api)
                : TemporalProspectorConfig.CreateDefault();
        }

        private static void ApplyRecipeConfig(ICoreAPI api)
        {
            AssetLocation stoneRecipeLocation =
                new(ModDomain, "recipes/grid/temporal-prospectingpick-stone.json");
            IAsset stoneRecipeAsset = api.Assets.TryGet(stoneRecipeLocation);

            if (stoneRecipeAsset == null)
            {
                api.Logger.Warning($"[{ModDomain}] Could not find recipe asset {stoneRecipeLocation}; skipping recipe config patch.");
                return;
            }

            try
            {
                string[] enabledStoneVariants = GetEnabledStoneVariants();
                JToken token = JToken.Parse(stoneRecipeAsset.ToText());
                bool changed = PatchStoneRecipeAssetJson(ref token, enabledStoneVariants);

                if (changed)
                {
                    stoneRecipeAsset.Data = Encoding.UTF8.GetBytes(token.ToString(Formatting.Indented) + "\n");
                    stoneRecipeAsset.IsPatched = true;
                }
            }
            catch (Exception exception)
            {
                api.Logger.Error($"[{ModDomain}] Failed to patch recipe asset {stoneRecipeLocation}: {exception}");
            }
        }

        private static string[] GetEnabledStoneVariants()
        {
            int enabledCount = 0;
            if (Config.EnableChalkRecipe) enabledCount++;
            if (Config.EnableHaliteRecipe) enabledCount++;
            if (Config.EnableLimestoneRecipe) enabledCount++;

            string[] enabledVariants = new string[enabledCount];
            int index = 0;

            if (Config.EnableChalkRecipe) enabledVariants[index++] = "chalk";
            if (Config.EnableHaliteRecipe) enabledVariants[index++] = "halite";
            if (Config.EnableLimestoneRecipe) enabledVariants[index] = "limestone";

            return enabledVariants;
        }

        private static bool PatchStoneRecipeAssetJson(ref JToken token, string[] enabledStoneVariants)
        {
            if (enabledStoneVariants.Length == 0)
            {
                if (token is JArray recipeArray)
                {
                    if (recipeArray.Count == 0)
                    {
                        return false;
                    }

                    recipeArray.RemoveAll();
                    return true;
                }

                token = new JArray();
                return true;
            }

            JArray desiredAllowedVariants = [.. enabledStoneVariants];

            bool changed = false;

            if (token is JArray recipeArrayToken)
            {
                foreach (JToken child in recipeArrayToken)
                {
                    if (child is JObject recipeObject)
                    {
                        changed |= PatchStoneRecipeObject(recipeObject, desiredAllowedVariants);
                    }
                }

                return changed;
            }

            if (token is JObject singleRecipeObject)
            {
                return PatchStoneRecipeObject(singleRecipeObject, desiredAllowedVariants);
            }

            return false;
        }

        private static bool PatchStoneRecipeObject(JObject recipeObject, JArray desiredAllowedVariants)
        {
            if (recipeObject["ingredients"] is not JObject ingredients ||
                ingredients["S"] is not JObject stoneIngredient)
            {
                return false;
            }

            if (!string.Equals((string)stoneIngredient["code"], "game:stone-*", StringComparison.Ordinal))
            {
                return false;
            }

            if (stoneIngredient["allowedVariants"] is JArray currentAllowedVariants && JToken.DeepEquals(currentAllowedVariants, desiredAllowedVariants))
            {
                return false;
            }

            stoneIngredient["allowedVariants"] = new JArray(desiredAllowedVariants);
            return true;
        }

        private static void ApplyDurabilityConfig(ICoreAPI api)
        {
            SetDurability(api, "copper", Config.CopperDurability);
            SetDurability(api, "tinbronze", Config.TinBronzeDurability);
            SetDurability(api, "bismuthbronze", Config.BismuthBronzeDurability);
            SetDurability(api, "blackbronze", Config.BlackBronzeDurability);
            SetDurability(api, "gold", Config.GoldDurability);
            SetDurability(api, "silver", Config.SilverDurability);
            SetDurability(api, "iron", Config.IronDurability);
            SetDurability(api, "meteoriciron", Config.MeteoricIronDurability);
            SetDurability(api, "steel", Config.SteelDurability);
        }

        private static void SetDurability(ICoreAPI api, string metal, int durability)
        {
            Item item = api.World.GetItem(new AssetLocation(ModDomain, "temporalprospectingpick-" + metal));
            item?.Durability = durability;
        }

        private bool TrySubscribeToConfigLib(ICoreAPI api)
        {
            if (configLibSubscribed)
            {
                return true;
            }

            if (!api.ModLoader.IsModEnabled("configlib"))
            {
                return false;
            }

            configLibModSystem = api.ModLoader.GetModSystem("ConfigLib.ConfigLibModSystem");
            if (configLibModSystem == null)
            {
                api.Logger.Warning($"[{ModDomain}] ConfigLib is enabled but ConfigLibModSystem couldn't be found.");
                return false;
            }

            Type systemType = configLibModSystem.GetType();
            EventInfo settingChangedEvent = systemType.GetEvent("SettingChanged");
            if (settingChangedEvent == null)
            {
                api.Logger.Warning($"[{ModDomain}] ConfigLib SettingChanged event couldn't be found.");
                return false;
            }

            if (settingChangedEvent != null && configLibSettingChangedHandler == null)
            {
                MethodInfo method = GetType().GetMethod(nameof(OnConfigLibSettingChanged),
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    try
                    {
                        configLibSettingChangedHandler =
                            Delegate.CreateDelegate(settingChangedEvent.EventHandlerType, this, method);
                        settingChangedEvent.AddEventHandler(configLibModSystem, configLibSettingChangedHandler);
                    }
                    catch (Exception exception)
                    {
                        configLibSettingChangedHandler = null;
                        api.Logger.Warning($"[{ModDomain}] Failed to hook ConfigLib SettingChanged: {exception}");
                        return false;
                    }
                }
            }

            EventInfo configsLoadedEvent = systemType.GetEvent("ConfigsLoaded");
            if (configsLoadedEvent == null)
            {
                api.Logger.Warning($"[{ModDomain}] ConfigLib ConfigsLoaded event couldn't be found.");
                return false;
            }

            if (configsLoadedEvent != null && configLibConfigsLoadedHandler == null)
            {
                MethodInfo method = GetType().GetMethod(nameof(OnConfigLibConfigsLoaded),
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    try
                    {
                        configLibConfigsLoadedHandler =
                            Delegate.CreateDelegate(configsLoadedEvent.EventHandlerType, this, method);
                        configsLoadedEvent.AddEventHandler(configLibModSystem, configLibConfigsLoadedHandler);
                    }
                    catch (Exception exception)
                    {
                        configLibConfigsLoadedHandler = null;
                        api.Logger.Warning($"[{ModDomain}] Failed to hook ConfigLib ConfigsLoaded: {exception}");
                        return false;
                    }
                }
            }

            configLibSubscribed = true;
            return true;
        }

        private void OnConfigLibSettingChanged(string domain, object _config, object _setting)
        {
            if (!string.Equals(domain, ModDomain, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RefreshConfigFromConfigLib();
        }

        private void OnConfigLibConfigsLoaded()
        {
            RefreshConfigFromConfigLib();
        }

        private void RefreshConfigFromConfigLib()
        {
            if (configLibModSystem == null)
            {
                return;
            }

            // Grab a default config to let multiplayer clients mirror
            // server values without touching their local config file.
            TemporalProspectorConfig nextConfig = TemporalProspectorConfig.CreateDefault();

            try
            {
                Type systemType = configLibModSystem.GetType();
                MethodInfo getConfigMethod = systemType.GetMethod("GetConfig", [typeof(string)]);
                object configDefinition = getConfigMethod?.Invoke(configLibModSystem, [ModDomain]);
                if (configDefinition == null)
                {
                    return;
                }

                MethodInfo assignAllMethod =
                    configDefinition.GetType().GetMethod("AssignSettingsValues", [typeof(object)]);
                assignAllMethod?.Invoke(configDefinition, [nextConfig]);
            }
            catch
            {
                return;
            }

            SetConfig(nextConfig);
        }

        private void SetConfig(TemporalProspectorConfig nextConfig)
        {
            TemporalProspectorConfig previousConfig = Config?.Clone() ?? TemporalProspectorConfig.CreateDefault();
            Config = (nextConfig ?? TemporalProspectorConfig.CreateDefault()).Normalize();

            if (!assetsLoaded)
            {
                return;
            }

            ApplyRuntimeConfig();
            NotifyRecipeReloadRequirement(previousConfig, Config);
        }

        private void ApplyRuntimeConfig()
        {
            ICoreAPI runtimeApi = serverApi ?? (ICoreAPI)clientApi ?? coreApi;
            if (runtimeApi != null)
            {
                ApplyDurabilityConfig(runtimeApi);
            }

            if (serverApi != null && usesFallbackConfigSync)
            {
                BroadcastConfigToPlayers();
            }
        }

        private void NotifyRecipeReloadRequirement(TemporalProspectorConfig previousConfig, TemporalProspectorConfig currentConfig)
        {
            if (serverApi == null || previousConfig == null || currentConfig == null)
            {
                return;
            }

            if (previousConfig.EnableChalkRecipe != currentConfig.EnableChalkRecipe ||
                previousConfig.EnableHaliteRecipe != currentConfig.EnableHaliteRecipe ||
                previousConfig.EnableLimestoneRecipe != currentConfig.EnableLimestoneRecipe)
            {
                serverApi.Logger.Notification($"[{ModDomain}] Recipe toggle changes were loaded into config, but crafting recipe changes still require a restart or world reload to take effect.");
            }
        }

        private void BroadcastConfigToPlayers()
        {
            if (serverApi == null || Config == null)
            {
                return;
            }

            foreach (IPlayer player in serverApi.World.AllOnlinePlayers)
            {
                if (player is IServerPlayer serverPlayer)
                {
                    SendConfigToPlayer(serverPlayer);
                }
            }
        }

        private void SendConfigToPlayer(IServerPlayer player)
        {
            if (Config == null)
            {
                return;
            }

            serverChannel?.SendPacket(Config.ToPacket(), player);
        }

        private void OnConfigSyncPacket(TemporalProspectorConfigPacket packet)
        {
            SetConfig(TemporalProspectorConfig.FromPacket(packet));
        }
    }
}
