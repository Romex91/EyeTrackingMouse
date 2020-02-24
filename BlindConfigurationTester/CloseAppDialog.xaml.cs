using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BlindConfigurationTester
{
    /// <summary>
    /// Interaction logic for CloseAppDialog.xaml
    /// </summary>
    public partial class CloseAppDialog : Window
    {
        
        public CloseAppDialog()
        {
            InitializeComponent();

            Task.Factory.StartNew(WaitApplicationExit);
        }

        public void WaitApplicationExit()
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                if (process.ProcessName == eye_tracking_mouse.Helpers.application_name)
                {
                    process.WaitForExit();
                    Task.Factory.StartNew(WaitApplicationExit);
                    return;
                }
            }

            Dispatcher.BeginInvoke((Action)(() => {
                if (IsLoaded)
                {
                    DialogResult = true;
                    Close();
                }
            }));
        }
    }
}
