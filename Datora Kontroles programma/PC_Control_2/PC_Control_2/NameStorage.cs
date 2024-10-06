using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PC_Control_2
{
    public static class NameStorage
    {
        private static string filePath = "client_names.json";
        private static Dictionary<string, string> names = new Dictionary<string, string>();

        static NameStorage()
        {
            LoadNames();
        }

        public static void SaveName(string ip, string name)
        {
            names[ip] = name;
            SaveNames();
        }

        public static string GetName(string ip)
        {
            return names.ContainsKey(ip) ? names[ip] : string.Empty;
        }

        private static void SaveNames()
        {
            var json = JsonConvert.SerializeObject(names, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private static void LoadNames()
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                names = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
        }
    }
}