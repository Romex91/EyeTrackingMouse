using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    public class Statistics
    {
        public Statistics() { }

        public void OnClick()
        {
            clicks++;
            FilesSavingQueue.Save(Filepath, GetDeepCopy);
        }

        public void OnCalibrate()
        {
            calibrations++;
            FilesSavingQueue.Save(Filepath, GetDeepCopy);
        }

        public static Statistics LoadFromFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return JsonConvert.DeserializeObject<Statistics>(File.ReadAllText(path));
                }
            }
            catch (Exception)
            {
            }
            return new Statistics();
        }

        public static string Filepath { get { return Path.Combine(Helpers.UserDataFolder, "statistics.json"); } }

        private string GetDeepCopy()
        {
            return JsonConvert.SerializeObject(this);
        }

        public System.UInt64 clicks = 0;
        public System.UInt64 calibrations = 0;
    }
}
