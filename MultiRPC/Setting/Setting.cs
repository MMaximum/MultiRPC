using System.IO;
using System.Text.Json;

namespace MultiRPC.Setting
{
    public abstract class Setting
    {
        public abstract string Name { get; }

        public void Save()
        {
            var settingFileLocation = Path.Combine(Constants.SettingsFolder, Name + ".json");
            using var stream = File.OpenWrite(settingFileLocation);
            JsonSerializer.Serialize(stream, this, GetType(), Constants.JsonSerializer);
        }
    }
}