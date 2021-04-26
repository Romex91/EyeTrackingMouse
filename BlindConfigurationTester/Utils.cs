using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.IO;

namespace BlindConfigurationTester
{
    class  Utils
    {
        public static string[] GetConfigurationsList()
        {
            try
            {
                string[] dirs = Directory.GetDirectories(Path.Combine(DataFolder, "Configurations"));
                for (int i = 0; i < dirs.Length; i++)
                {
                    dirs[i] = Path.GetFileName(dirs[i]);
                }
                return dirs;
            }catch(Exception)
            {
                return new string[0]; 
            }
        }

        public static string DataFolder
        {
            get { return Path.Combine( Environment.GetEnvironmentVariable("LocalAppData"), "BlindConfigurationTester"); }
        }

        public static string GetConfigurationDir(string configuration)
        {
            if (configuration == null)
                return eye_tracking_mouse.Helpers.UserDataFolder;
            return Path.Combine(DataFolder, "Configurations", configuration);
        }

        public static string GenerateNewConfigurationName(
            string tag)
        {
            int generated_configs_max_index = 0;
            var existing_configurations = Utils.GetConfigurationsList();
            foreach (var existing_config in existing_configurations)
            {
                if (existing_config == null)
                    continue;

                var tokens = existing_config.Split('_');
                int index = 0;
                if (tokens.Length > 1 &&
                    tokens[0] == tag &&
                    int.TryParse(tokens[1], out index) &&
                    index > generated_configs_max_index)
                {
                    generated_configs_max_index = index;
                }
            }

            return tag+"_" +
                (generated_configs_max_index + 1);
        }

        public static void CreateConfiguration(string configuration)
        {
            string configuration_path = GetConfigurationDir(configuration);
            if (!Directory.Exists(configuration_path))
                Directory.CreateDirectory(configuration_path);
        }
    }
}
