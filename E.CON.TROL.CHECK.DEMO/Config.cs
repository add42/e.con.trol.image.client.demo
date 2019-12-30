using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace E.CON.TROL.CHECK.DEMO
{
    class Config
    {
        public static Config Instance { get; private set; }

        public string Name { get; set; } = "E.CON.TROL.CHECK.KI.1";

        public string ServerAddress { get; set; } = "127.0.0.1";

        public ushort CameraNumber = 5;

        public int LogLevel { get; set; } = 0;

        public bool ReturnBoxResultIo { get; set; } = true;

        public string GetConnectionStringCore4Receiving()
        {
            return $"tcp://{ServerAddress}:55555";
        }

        public string GetConnectionStringCore4Transmit()
        {
            return $"tcp://{ServerAddress}:55556";
        }

        public string GetConnectionString4Images()
        {
            return $"tcp://{ServerAddress}:5556{CameraNumber}";
        }

        public static void LoadConfig()
        {
            var cfg = new Config();

            var path = Path.Combine(cfg.GetLocalStorageDirectory(), "Config.cfg");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                cfg = JsonConvert.DeserializeObject<Config>(json);
            }

            cfg.SaveConfig();

            Instance = cfg;
        }

        public void SaveConfig()
        {
            var path = Path.Combine(this.GetLocalStorageDirectory(), "Config.cfg");
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public Task OpenEditor()
        {
            var task = Task.Run(() =>
            {
                try
                {
                    var path = Path.Combine(this.GetLocalStorageDirectory(), "Config.cfg");
                    var process = Process.Start("notepad.exe", path);
                    process.WaitForExit();

                    var json = File.ReadAllText(path);
                    JsonConvert.PopulateObject(json, this);
                }
                catch { }
            });

            return task;
        }
    }
}