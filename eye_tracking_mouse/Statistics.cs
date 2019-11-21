using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    class Statistics
    {
        public Statistics()
        {
            Settings.OptionsChanged += UpdateCurrentItemKey;
            UpdateCurrentItemKey(null, null); 
        }

        public void OnClick()
        {
            items[current_item_key].clicks++;
            AsyncSaver.Save(Filepath, GetDeepCopy);
        }

        public void OnCalibrate()
        {
            items[current_item_key].calibrations++;
            AsyncSaver.Save(Filepath, GetDeepCopy);
        }

        public static Statistics LoadFromFile()
        {
            Statistics statistics = new Statistics();
            if (File.Exists(Filepath))
            {
                statistics.items = JsonConvert.DeserializeObject<Dictionary<string, StatisticsItem>>(File.ReadAllText(Filepath));
            }
            return statistics;
        }

        private void UpdateCurrentItemKey(object sender, EventArgs e)
        {
            current_item_key = JsonConvert.SerializeObject(Options.Instance.calibration, Formatting.None);
            if (!items.ContainsKey(current_item_key))
            {
                items.Add(current_item_key, new StatisticsItem());
            }
        }

        private static string Filepath { get { return Path.Combine(Helpers.GetLocalFolder(), "statistics.json"); } }

        private object GetDeepCopy()
        {
            return items.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
        }

        private class StatisticsItem
        {
            public System.UInt64 clicks = 0;
            public System.UInt64 calibrations = 0;

            public StatisticsItem Clone()
            {
                return new StatisticsItem { calibrations = this.calibrations, clicks = this.clicks };
            }
        }

        private string current_item_key = "";
        private Dictionary<string, StatisticsItem> items = new Dictionary<string, StatisticsItem>();
    }
}
