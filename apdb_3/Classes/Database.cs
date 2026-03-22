using Spectre.Console;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace apdb_3.Classes
{
    public class Database
    {
        private static string GetFilePath(string table)
        {
            return Path.Combine("Database", $"{table}.json");
        }

        public static void AddRecord(string table, Object data)
        {
            string filePath = GetFilePath(table);
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Database file for '{table}' not found.");

            string jsonContent = File.ReadAllText(filePath);
            JObject root = JObject.Parse(jsonContent);

            JArray tableArray = (JArray)root[table];
            if (tableArray == null)
            {
                tableArray = new JArray();
                root[table] = tableArray;
            }

            tableArray.Add(JToken.FromObject(data));
            File.WriteAllText(filePath, root.ToString(Formatting.Indented));
        }

        public static void DeleteRecord(string table, Object data)
        {
            string filePath = GetFilePath(table);
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Database file for '{table}' not found.");

            string jsonContent = File.ReadAllText(filePath);
            JObject root = JObject.Parse(jsonContent);
            JArray tableArray = (JArray)root[table];

            if (tableArray != null)
            {
                JToken tokenToRemove = JToken.FromObject(data);
                JToken match = null;

                foreach (JToken item in tableArray)
                {
                    if (JToken.DeepEquals(item, tokenToRemove))
                    {
                        match = item;
                        break;
                    }
                }

                if (match != null)
                {
                    match.Remove();
                    File.WriteAllText(filePath, root.ToString(Formatting.Indented));
                }
            }
        }
        public static T GetRecord<T>(string table, string searchProperty, string searchString)
        {
            string filePath = GetFilePath(table);

            if (!File.Exists(filePath)) return default(T);

            string jsonContent = File.ReadAllText(filePath);
            JObject root = JObject.Parse(jsonContent);
            JArray tableArray = (JArray)root[table];

            if (tableArray != null)
            {
                foreach (JObject item in tableArray)
                {
                    if (item[searchProperty] != null && item[searchProperty].ToString() == searchString)
                    {
                        return item.ToObject<T>();
                    }
                }
            }

            return default(T);
        }

        public static List<T> GetRecords<T>(string table)
        {
            string filePath = GetFilePath(table);

            if (!File.Exists(filePath)) return new List<T>();

            string jsonContent = File.ReadAllText(filePath);
            JObject root = JObject.Parse(jsonContent);
            JArray tableArray = (JArray)root[table];

            if (tableArray != null)
            {
                var serializer = new JsonSerializer();

                if (typeof(T) == typeof(Gear))
                {
                    serializer.Converters.Add(new GearConverter());
                }

                return tableArray.ToObject<List<T>>(serializer);
            }

            return new List<T>();
        }

        public static void UpdateRecord(string table, string searchProperty, string searchString, string propertyToUpdate, object newValue)
        {
            string filePath = GetFilePath(table);
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Database file for '{table}' not found.");

            string jsonContent = File.ReadAllText(filePath);
            JObject root = JObject.Parse(jsonContent);
            JArray tableArray = (JArray)root[table];

            if (tableArray != null)
            {
                foreach (JObject item in tableArray)
                {
                    if (item[searchProperty] != null && item[searchProperty].ToString() == searchString)
                    {
                        item[propertyToUpdate] = JToken.FromObject(newValue);

                        File.WriteAllText(filePath, root.ToString(Formatting.Indented));
                        return;
                    }
                }
            }
        }
    }
}