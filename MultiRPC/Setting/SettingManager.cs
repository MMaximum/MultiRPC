﻿using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace MultiRPC.Setting
{
    //TODO: Use source gen to make JsonTypeInfo (Ensures we can process json even when trimming)
    public static class SettingManager<T> 
        where T : BaseSetting, new()
    {
        private static readonly Lazy<T> LazySetting = new Lazy<T>(() =>
        {
            var setting = new T();
            
            var settingFileLocation = Path.Combine(Constants.SettingsFolder, setting.Name + ".json");
            if (File.Exists(settingFileLocation))
            {
                using var fileSteam = File.OpenRead(settingFileLocation);
                var fileSetting = JsonSerializer.Deserialize<T>(fileSteam);
                if (fileSetting != null)
                {
                    setting = fileSetting;
                }
            }

            if (setting is INotifyPropertyChanged settingNotify)
            {
                settingNotify.PropertyChanged += (sender, args) =>
                {
                    setting.Save();
                };
            }
            return setting;
       });

        public static T Setting => LazySetting.Value;
    }
}