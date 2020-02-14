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

    public class Study
    {
        public int points_per_session;
        public int number_of_sessions;
        public int number_of_completed_sessions;
        public int size_of_circle;
        public string description;
        public List<Configuration> configurations = new List<Configuration>();
        public int study_index;

        public Study(
            int points_per_session,
            int number_of_sessions,
            int size_of_circle,
            string description,
            List<Configuration> configurations)
        {
            this.points_per_session = points_per_session;
            this.number_of_sessions = number_of_sessions;
            this.size_of_circle = size_of_circle;
            this.description = description;
            this.number_of_completed_sessions = 0;
            this.configurations = configurations;

            int largest_study_index = 0;
            if (Directory.Exists(StudiesFolder))
                foreach (var study_dir in Directory.GetDirectories(StudiesFolder))
                {
                    int study_index = 0;
                    if (int.TryParse(Path.GetFileName(study_dir), out study_index) && largest_study_index < study_index)
                        largest_study_index = study_index;
                }
            study_index = largest_study_index + 1;

            SaveToFile();
        }

        public bool IsFinished
        {
            get { return number_of_completed_sessions >= number_of_sessions; }
        }

        private void Finish()
        {
            var results = new Dictionary<string, List<eye_tracking_mouse.Statistics>>();

            for (int i = 0; i < number_of_sessions; i++)
            {
                foreach(var configuration in configurations)
                {
                    string configuration_string = configuration.name ?? "User Data";
                    var statistics_before = eye_tracking_mouse.Statistics.LoadFromFile(Path.Combine(GetPathBefore(i), configuration_string, "statistics.json"));
                    var statistics_after = eye_tracking_mouse.Statistics.LoadFromFile(Path.Combine(GetPathAfter(i), configuration_string, "statistics.json"));

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

            File.WriteAllText( Path.Combine(StudyResultsFolder, "results.json"), JsonConvert.SerializeObject(results));
            File.Move(UnfinishedStudyPath, Path.Combine(StudyResultsFolder, "study_settings.json"));

            MessageBox.Show("Will open explorer with results now.", "Study is over!", MessageBoxButton.OK);
            Process.Start(StudyResultsFolder);
        }

        public void Abort()
        {
            File.Delete(UnfinishedStudyPath);
        }

        public void StartSession()
        {
            var rand = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId));

            List<Tuple<int, int>> points = new List<Tuple<int, int>>();
            for (int i = 0; i < points_per_session; i++)
                points.Add(new Tuple<int, int>(rand.Next(0, int.MaxValue), rand.Next(0, int.MaxValue)));

            List<int> configurations_indices = new List<int>();
            for (int i = 0; i < configurations.Count; i++)
                configurations_indices.Add(i);

            configurations_indices.Shuffle(rand);


            foreach (var configuration_index in configurations_indices)
            {
                var session = new SessionWindow(points, size_of_circle);
                Utils.RunApp(configurations[configuration_index].name, configurations[configuration_index].save_changes, () =>
                {
                    TakeSnapshotBeforeSession(configurations[configuration_index].name);
                }, () => {
                    session.ShowDialog();
                    TakeSnapshotAfterSession(configurations[configuration_index].name);
                });
            }

            number_of_completed_sessions++;
            SaveToFile();

            if (number_of_completed_sessions >= number_of_sessions)
                Finish();
        }

        private static string StudiesFolder
        {
            get { return Path.Combine(eye_tracking_mouse.Helpers.AppFolder, "Studies"); }
        }

        private static string UnfinishedStudyPath
        {
            get { return Path.Combine(eye_tracking_mouse.Helpers.AppFolder, "unfinished_study.json"); }
        }

        private string StudyResultsFolder
        {
            get { return Path.Combine(StudiesFolder, study_index.ToString()); }
        }

        private string GetSessionResultsPath(int session_index)
        {
            return Path.Combine(StudyResultsFolder, session_index.ToString());
        }

        private string GetPathBefore(int session_index)
        {
            return Path.Combine(GetSessionResultsPath(session_index), "Before");
        }

        private string GetPathAfter(int session_index)
        {
            return Path.Combine(GetSessionResultsPath(session_index), "After");
        }

        private void TakeSnapshotBeforeSession(string configuration_name)
        {
            while (!Utils.TryCloseApplication());
            string path_before = GetPathBefore(number_of_completed_sessions);
            if (!Directory.Exists(path_before))
                Directory.CreateDirectory(path_before);

            Utils.CopyDir(
                eye_tracking_mouse.Helpers.UserDataFolder, 
                Path.Combine(path_before, (configuration_name ?? "User Data")));
        }

        private void TakeSnapshotAfterSession(string configuration_name)
        {
            while (!Utils.TryCloseApplication()) ;
            string path_after = GetPathAfter(number_of_completed_sessions);
            if (!Directory.Exists(path_after))
                Directory.CreateDirectory(path_after);

            Utils.CopyDir(
                eye_tracking_mouse.Helpers.UserDataFolder,
                Path.Combine(path_after, (configuration_name ?? "User Data")));
        }
        private void SaveToFile()
        {
            while (!Utils.TryCloseApplication());
            File.WriteAllText(UnfinishedStudyPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        static public Study LoadUnfinished()
        {
            if (File.Exists(UnfinishedStudyPath))
            {
                return JsonConvert.DeserializeObject<Study>(File.ReadAllText(UnfinishedStudyPath));
            }

            return null;
        }
    }
}
