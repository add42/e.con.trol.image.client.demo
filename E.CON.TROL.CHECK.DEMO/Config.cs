using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace E.CON.TROL.CHECK.DEMO
{
    class Config
    {
        public string Name { get; set; } = "KI-Test";

        public string ConnectionStringCoreReceive { get; set; } = "tcp://localhost:55555";

        public string ConnectionStringCoreTransmit { get; set; } = "tcp://localhost:55556";

        public int LogLevel { get; set; } = 0;

        public static Config LoadConfig()
        {
            var cfg = new Config();

            var path = Path.Combine(cfg.GetLocalStorageDirectory(), "Config.cfg");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                cfg = JsonConvert.DeserializeObject<Config>(json);
            }

            return cfg;
        }

        public void SaveConfig()
        {
            var path = Path.Combine(this.GetLocalStorageDirectory(), "Config.cfg");
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public void OpenEditor()
        {
            try
            {
                var path = Path.Combine(this.GetLocalStorageDirectory(), "Config.cfg");
                Process.Start("notepad.exe", path);
            }
            catch { }
        }
    }
}