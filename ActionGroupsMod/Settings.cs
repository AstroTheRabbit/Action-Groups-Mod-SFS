using System;
using SFS.IO;
using UITools;
using UnityEngine;

namespace ActionGroupsMod
{
    public class Settings : ModSettings<Settings.Data>
    {
        private static Settings main;
        protected override FilePath SettingsFile => new FolderPath(Main.main.ModFolder).ExtendToFile("settings.txt");

        protected override void RegisterOnVariableChange(Action onChange)
        {
            Application.quitting += onChange;
        }

        public static void Init()
        {
            main = new Settings();
            main.Initialize();
        }

        public class Data
        {
            public bool WindowMinimized { get; set; }
        }
    }
}