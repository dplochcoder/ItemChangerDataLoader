﻿using Modding;
using MenuChanger;
using ItemChanger;
using RandomizerMod;
using RandomizerMod.RC;
using System.Reflection;
using ICSettings = ItemChanger.Settings;
using static RandomizerMod.Localization;
using UnityEngine;

namespace ItemChangerDataLoader
{
    public class ICDLMod : Mod, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        public ICDLMod() : base("ICDL Mod") { }
        public override string GetVersion() => Version;
        public static ICDLMod Instance { get; private set; }
        public static GlobalSettings GlobalSettings { get; private set; } = new();

        internal static bool icdlStartGame;

        public override void Initialize()
        {
            Instance = this;
            Events.BeforeStartNewGame += BeforeStartNewGame;
            ModeMenu.AddMode(new ICDLModeMenuConstructor("Past Randos", "Past Randos"));
            ModeMenu.AddMode(new ICDLModeMenuConstructor("Plando Plando", "Plandos"));
        }

        private void BeforeStartNewGame()
        {
            if (icdlStartGame) // don't backup a backup
            {
                icdlStartGame = false;
                return;
            }

            if (RandomizerMod.RandomizerMod.IsRandoSave)
            {
                CreateRandoBackup(GlobalSettings.BackupNewRandoSaves);
            }
        }

        private void CreateRandoBackup(BackupRandoType type)
        {
            if (type == BackupRandoType.None) return;

            ICSettings s = ItemChanger.Internal.Ref.Settings;
            RandoModContext ctx = RandomizerMod.RandomizerMod.RS.Context;
            string stamp = DateTime.Now.ToString("yyyy-M-dd--HH-mm-ss") + " - " + ctx.GenerationSettings.Seed.ToString();
            string path = type switch
            {
                BackupRandoType.Manual => Path.Combine(TempDirectory, $"user{GameManager.instance.profileID}", stamp),
                _ => Path.Combine(PastRandoDirectory, stamp)
            };

            try
            {
                if (type == BackupRandoType.Manual)
                {
                    string userPath = Path.Combine(TempDirectory, $"user{GameManager.instance.profileID}");
                    if (Directory.Exists(userPath))
                    {
                        Directory.Delete(userPath, true);
                    }
                }

                DirectoryInfo di = Directory.CreateDirectory(path);
                string dir = di.FullName;
                JsonUtil.Serialize(Path.Combine(dir, "ic.json"), s);
                JsonUtil.Serialize(Path.Combine(dir, "ctx.json"), ctx);
                JsonUtil.Serialize(Path.Combine(dir, "pack.json"), new ICPack
                {
                    Name = ctx.GenerationSettings.Seed.ToString(),
                    Author = $"RandomizerMod {RandomizerMod.RandomizerMod.Version}, {DateTime.Now:yyyy-M-dd}",
                    Description = string.Empty,
                    SupportsRandoTracking = true,
                });
            }
            catch (Exception e)
            {
                LogError($"Error creating rando backup:\n{e}");
            }
        }

        void IGlobalSettings<GlobalSettings>.OnLoadGlobal(GlobalSettings s)
        {
            GlobalSettings = s;
        }

        GlobalSettings IGlobalSettings<GlobalSettings>.OnSaveGlobal()
        {
            return GlobalSettings;
        }

        bool ICustomMenuMod.ToggleButtonInsideMenu => throw new NotImplementedException();

        MenuScreen ICustomMenuMod.GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            return ModMenu.GetMenuScreen(modListMenu);
        }

        public static string Version { get; }

        public static string ICDLDirectory { get; }
        public static string TempDirectory { get; }
        public static string PastRandoDirectory { get; }

        static ICDLMod()
        {
            Assembly a = typeof(ICDLMod).Assembly;
            ICDLDirectory = Path.Combine(Application.persistentDataPath, "ICDL");
            TempDirectory = Path.Combine(ICDLDirectory, "Temp");
            PastRandoDirectory = Path.Combine(ICDLDirectory, "Past Randos");
            try
            {
                Directory.CreateDirectory(ICDLDirectory);
                Directory.CreateDirectory(TempDirectory);
                Directory.CreateDirectory(PastRandoDirectory);
            }
            catch (Exception) { }


            Version v = a.GetName().Version;
            Version = $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }
}