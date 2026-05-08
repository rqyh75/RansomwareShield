using System.Text.Json;

namespace CanaryAgent.Storage
{
    public class StateStore
    {
        public List<CanaryFileState> Files { get; set; }
        public DateTime LastRunDate { get; set; }
        public DateTime NextFileCreationTime { get; set; }
        public string? TempDirectoryPath { get; set; }

        public StateStore()
        {
            Files = new List<CanaryFileState>();
            LastRunDate = DateTime.MinValue;
            NextFileCreationTime = DateTime.MinValue;
        }
        public void Save(string path)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            File.WriteAllText(path, JsonSerializer.Serialize(this, options));
        }

        public static StateStore Load(string path)
        {
            if (!File.Exists(path))
                return new StateStore();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<StateStore>(json) ?? new StateStore();
        }
    }
}

