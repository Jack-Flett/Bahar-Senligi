using System;
using System.IO;
using System.Text.Json;
using BaharSenligi.Models;

namespace BaharSenligi.Services
{
    public class DataService
    {
        private static readonly string DataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "data.json");

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true
        };

        public AppData Load()
        {
            try
            {
                if (File.Exists(DataPath))
                {
                    var json = File.ReadAllText(DataPath);
                    return JsonSerializer.Deserialize<AppData>(json, Options) ?? new AppData();
                }
            }
            catch { }
            return new AppData();
        }

        public void Save(AppData data)
        {
            var json = JsonSerializer.Serialize(data, Options);
            File.WriteAllText(DataPath, json);
        }
    }
}
