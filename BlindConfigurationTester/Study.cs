using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Diagnostics;

namespace BlindConfigurationTester
{
    public static class Extensions
    {
        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
    public struct Configuration
    {
        public bool save_changes;
        public string name;
    }

    public struct Session
    {
        public int points_count;
        public int size_of_circle;
        public string instructions;
    }

    public class Study
    {
        [JsonIgnore]
        public string name;
        public int number_of_completed_sessions = 0;
        public List<Configuration> configurations = new List<Configuration>() { new Configuration { name = null, save_changes=true } };

        public List<Session> sessions = new List<Session> { new Session { points_count = 100, size_of_circle = 15, instructions = "This text will be shown to user before session." } };

        public static string StudiesFolder
        {
            get { return Path.Combine(Utils.DataFolder, "Studies"); }
        }

        [JsonIgnore]
        public string StudyResultsFolder
        {
            get { return GetStudyFolder(name); }
        }

        public Study(string name)
        {
            this.name = name;
        }

        public void StartSession()
        {
            if (Directory.Exists(GetSessionResultsPath(number_of_completed_sessions))) {
                MessageBox.Show("Session" + (number_of_completed_sessions  + 1) + 
                    " already contains results. Remove them manualy to proceed.");
                return;
            }


            if (number_of_completed_sessions >= sessions.Count)
            {
                MessageBox.Show("All sessions are finished");
                return;
            }
            Session session = sessions[number_of_completed_sessions];

            if ((new SessionInstructionsWindow(session.instructions)).ShowDialog() != true)
                return;

            if (!InputInitWindow.input.IsLoaded)
                (new InputInitWindow()).ShowDialog();


            var rand = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId));

            List<Tuple<int, int>> points = new List<Tuple<int, int>>();
            for (int i = 0; i < session.points_count; i++)
                points.Add(new Tuple<int, int>(rand.Next(0, int.MaxValue), rand.Next(0, int.MaxValue)));

            List<int> configurations_indices = new List<int>();
            for (int i = 0; i < configurations.Count; i++)
                configurations_indices.Add(i);

            configurations_indices.Shuffle(rand);

            foreach (var configuration_index in configurations_indices)
            {
                Utils.RunApp(configurations[configuration_index].name, configurations[configuration_index].save_changes, () =>
                {
                    TakeSnapshotBeforeSession(configurations[configuration_index].name);
                }, () =>
                {
                    Thread.Sleep(2000);
                    InputInitWindow.input.SendKey(Interceptor.Keys.WindowsKey, Interceptor.KeyState.E0);
                    (new SessionWindow(points, session.size_of_circle)).ShowDialog();
                    TakeSnapshotAfterSession(configurations[configuration_index].name);
                    InputInitWindow.input.SendKey(Interceptor.Keys.WindowsKey, Interceptor.KeyState.E0 | Interceptor.KeyState.Up);
                });
            }

            number_of_completed_sessions++;
            SaveToFile();

            UpdateResultsJson();
        }

        public static string GetStudyFolder(string study_name)
        {
            return Path.Combine(StudiesFolder, study_name);
        }

        static public Study Load(string study_name)
        {
            string json_path = Path.Combine(GetStudyFolder(study_name), "study.json");
            if (File.Exists(json_path))
            {
                while(true)
                {
                    try
                    {
                        var study = JsonConvert.DeserializeObject<Study>(File.ReadAllText(json_path));
                        study.name = study_name;
                        return study;
                    } catch (IOException)
                    {
                    } catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        public string GetInfo()
        {
            string info =
               "Study " + name + "\n" +
               "Number of completed sessions: " + number_of_completed_sessions + "\n" +
               "Configurations:\n";

            foreach (var configuration in configurations)
            {
                info +=
                     (configuration.name == null ? "User Data" : configuration.name) + ". Saving changes? " +
                     (configuration.save_changes ? "Yes" : "No") + "\n";
            }
            return info;
        }

        public void SaveToFile()
        {
            if (!Directory.Exists(StudyResultsFolder))
                Directory.CreateDirectory(StudyResultsFolder);
            File.WriteAllText(Path.Combine(StudyResultsFolder, "study.json"), JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        private void UpdateResultsJson()
        {
            var results = new Dictionary<string, List<eye_tracking_mouse.Statistics>>();

            for (int i = 0; i < number_of_completed_sessions; i++)
            {
                foreach (var configuration in configurations)
                {
                    string configuration_string = configuration.name ?? "User Data";
                    var statistics_before = eye_tracking_mouse.Statistics.LoadFromFile(Path.Combine(GetUserDataPathBeforeSession(i), configuration_string, "statistics.json"));
                    var statistics_after = eye_tracking_mouse.Statistics.LoadFromFile(Path.Combine(GetUserDataPathAfterSession(i), configuration_string, "statistics.json"));

                    if (!results.ContainsKey(configuration_string))
                        results.Add(configuration_string, new List<eye_tracking_mouse.Statistics>());

                    results[configuration_string].Add(
                        new eye_tracking_mouse.Statistics
                        {
                            calibrations = statistics_after.calibrations - statistics_before.calibrations,
                            clicks = statistics_after.clicks - statistics_before.clicks
                        });
                }
            }

            File.WriteAllText(Path.Combine(StudyResultsFolder, "results.json"), JsonConvert.SerializeObject(results));
        }

        private string GetSessionResultsPath(int session_index)
        {
            return Path.Combine(StudyResultsFolder, session_index.ToString());
        }

        private string GetUserDataPathBeforeSession(int session_index)
        {
            return Path.Combine(GetSessionResultsPath(session_index), "Before");
        }

        private string GetUserDataPathAfterSession(int session_index)
        {
            return Path.Combine(GetSessionResultsPath(session_index), "After");
        }

        private void TakeSnapshotBeforeSession(string configuration_name)
        {
            while (!Utils.TryCloseApplication()) ;
            string path_before = GetUserDataPathBeforeSession(number_of_completed_sessions);
            if (!Directory.Exists(path_before))
                Directory.CreateDirectory(path_before);

            Utils.CopyDir(
                eye_tracking_mouse.Helpers.UserDataFolder,
                Path.Combine(path_before, (configuration_name ?? "User Data")));
        }

        private void TakeSnapshotAfterSession(string configuration_name)
        {
            while (!Utils.TryCloseApplication()) ;
            string path_after = GetUserDataPathAfterSession(number_of_completed_sessions);
            if (!Directory.Exists(path_after))
                Directory.CreateDirectory(path_after);

            Utils.CopyDir(
                eye_tracking_mouse.Helpers.UserDataFolder,
                Path.Combine(path_after, (configuration_name ?? "User Data")));
        }
    }
}
