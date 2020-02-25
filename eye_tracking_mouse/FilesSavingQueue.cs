using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    class FilesSavingQueue
    {
        // Suitable when you have large files that updates frequently.
        // |get_serialized_content| may be called immediately or in the future when enough time passes since the last flush.
        public static void Save(string filepath, Func<string> get_serialized_content)
        {
            lock (Helpers.locker)
            {
                if (queue.ContainsKey(filepath))
                    queue.Remove(filepath);
                queue.Add(filepath, get_serialized_content);

                if ((DateTime.Now - last_save_time).TotalSeconds > 600 && (save_to_file_task == null || save_to_file_task.IsCompleted))
                {
                    var deep_copies_to_save = SerializeQueue();

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
                SaveSynchronously(SerializeQueue());
            }
        }

        private static Dictionary<string, string> SerializeQueue()
        {
            Dictionary<string, string> serialized_queue = queue.ToDictionary(entry => entry.Key, entry => entry.Value());
            queue.Clear();
            return serialized_queue;
        }

        private static void SaveSynchronously(Dictionary<string, string> deep_copies_to_save)
        {
            foreach (var item in deep_copies_to_save)
            {
                File.WriteAllText(item.Key, item.Value);
            }
        }

        private static DateTime last_save_time = DateTime.Now;
        private static Task save_to_file_task = null;
        private static readonly Dictionary<string, Func<string>> queue = new Dictionary<string, Func<string>>();
    }
}
