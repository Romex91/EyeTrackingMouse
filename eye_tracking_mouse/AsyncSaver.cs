using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    class AsyncSaver
    {
        public static void Save(string filepath, Func<object> get_deep_copy)
        {
            lock (Helpers.locker)
            {
                if (savers.ContainsKey(filepath))
                    savers.Remove(filepath);
                savers.Add(filepath, get_deep_copy);

                if ((DateTime.Now - last_save_time).TotalSeconds > 60 && (save_to_file_task == null || save_to_file_task.IsCompleted))
                {
                    var deep_copies_to_save = CreateDeepCopies();

                    save_to_file_task = Task.Factory.StartNew(() => { SaveSynchronously(deep_copies_to_save); });
                    last_save_time = DateTime.Now;
                }
            }
        }

        public static void FlushSynchroniously()
        {
            lock (Helpers.locker)
            {
                if (save_to_file_task != null && !save_to_file_task.IsCompleted) 
                    save_to_file_task.Wait();
                SaveSynchronously(CreateDeepCopies());
            }
        }

        private static Dictionary<string, object> CreateDeepCopies()
        {
            Dictionary<string, object> deep_copies = savers.ToDictionary(entry => entry.Key, entry => entry.Value());
            savers.Clear();
            return deep_copies;
        }

        private static void SaveSynchronously(Dictionary<string, object> deep_copies_to_save)
        {
            foreach (var item in deep_copies_to_save)
            {
                File.WriteAllText(item.Key, JsonConvert.SerializeObject(item.Value, Formatting.Indented));
            }
        }

        private static DateTime last_save_time = DateTime.Now;
        private static Task save_to_file_task = null;
        private static readonly Dictionary<string, Func<object>> savers = new Dictionary<string, Func<object>>();
    }
}
