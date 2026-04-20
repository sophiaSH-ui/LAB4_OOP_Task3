using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using lab4_task3.DTO;

namespace lab4_task3
{
    public class OwnerJson
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public static class LocalStorage
    {
        private static readonly string PlotsPath = "plots.json";
        private static readonly string FilePath = "owners.json";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        public static List<Plot> LoadPlots()
        {
            if (!File.Exists(PlotsPath)) return new List<Plot>();
            string json = File.ReadAllText(PlotsPath);
            return JsonSerializer.Deserialize<List<Plot>>(json) ?? new List<Plot>();
        }

        public static void UpdatePlot(int id, Plot plot)
        {
            plot.Id = id;
            SavePlot(plot); 
        }

        public static void SavePlot(Plot plot)
        {
            var plots = LoadPlots();

            if (plot.Id == 0) plot.Id = plots.Count > 0 ? plots.Max(p => p.Id) + 1 : 1;

            int index = plots.FindIndex(p => p.Id == plot.Id);
            if (index >= 0) plots[index] = plot;
            else plots.Add(plot);

            string json = JsonSerializer.Serialize(plots, JsonOptions);
            File.WriteAllText(PlotsPath, json);
        }

        public static List<OwnerJson> LoadOwners()
        {
            if (!File.Exists(FilePath))
                return new List<OwnerJson>();

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<OwnerJson>>(json) ?? new List<OwnerJson>();
        }

        public static void SaveOwner(OwnerJson owner)
        {
            var owners = LoadOwners();

            int index = owners.FindIndex(o => o.Id == owner.Id);
            if (index >= 0)
                owners[index] = owner;
            else
                owners.Add(owner);

            string json = JsonSerializer.Serialize(owners, JsonOptions);
            File.WriteAllText(FilePath, json);
        }
    }
}