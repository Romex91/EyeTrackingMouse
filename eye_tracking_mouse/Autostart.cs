using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;
namespace eye_tracking_mouse
{
    class Autostart
    {
        public static bool IsEnabled { get {
                return TaskService.Instance.RootFolder.AllTasks.Any((task) => { return task.Name == Helpers.application_name; });
            }
        }

        public static void Enable()
        {
            if (IsEnabled)
                return;
            TaskDefinition td = TaskService.Instance.NewTask();

            td.Actions.Add(Path.Combine(Helpers.GetAppFolder(), Helpers.application_name + ".exe"));
            td.Triggers.Add(new LogonTrigger());
            td.Principal.RunLevel = TaskRunLevel.Highest;

            // Register the task in the root folder of the local machine
            TaskService.Instance.RootFolder.RegisterTaskDefinition(Helpers.application_name, td);
        }

        public static void Disable()
        {
            if (!IsEnabled)
                return;

            TaskService.Instance.RootFolder.DeleteTask(Helpers.application_name);
        }
    }
}
