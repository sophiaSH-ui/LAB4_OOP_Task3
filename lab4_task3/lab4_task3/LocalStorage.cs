using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

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
        private static readonly string FilePath = "owners.json";

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

            string json = JsonSerializer.Serialize(owners, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            });

            File.WriteAllText(FilePath, json);
        }
    }
}