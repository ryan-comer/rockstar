using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Assets.scripts.utility
{
    public static class ConfigUtilities
    {

        public static string ReadConfig(string configPath, string configKey)
        {
            checkConfigExists(configPath);

            using (var configFile = new StreamReader(File.OpenRead(configPath)))
            {
                string configText = configFile.ReadToEnd();
                if (string.IsNullOrEmpty(configText))
                {
                    return null;
                }

                var jObject = JObject.Parse(configText);
                if (!jObject.ContainsKey(configKey))
                {
                    return null;
                }
                else
                {
                    return jObject[configKey].ToString();
                }
                
            }
        }

        public static void WriteConfig(string configPath, string configKey, string configValue)
        {
            checkConfigExists(configPath);

            string oldJson = "";
            using (var configFile = new StreamReader(File.OpenRead(configPath)))
            {
                oldJson = configFile.ReadToEnd();
            }

            JObject jObject;
            if (string.IsNullOrEmpty(oldJson))
            {
                jObject = new JObject();
            }
            else
            {
                jObject = JObject.Parse(oldJson);
            }

            jObject[configKey] = configValue;
            string newJson = jObject.ToString();

            using (var configFile = new StreamWriter(configPath, false))
            {
                configFile.Write(newJson);
            }
        }

        private static void checkConfigExists(string path)
        {
            if (!File.Exists(path))
            {
                var sw = File.AppendText(path);
                sw.Close();
            }
        }

    }
}
