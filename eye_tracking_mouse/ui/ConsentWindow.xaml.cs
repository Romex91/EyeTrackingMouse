using System;
using System.Collections.Generic;
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

namespace eye_tracking_mouse
{
    /// <summary>
    /// Interaction logic for ConsentWindow.xaml
    /// </summary>
    public partial class ConsentWindow : Window
    {
        private Action on_consent_given;
        private Action on_consent_rejected;
        public ConsentWindow(Action on_consent_given, Action on_consent_rejected)
        {
            this.on_consent_given = on_consent_given;
            this.on_consent_rejected = on_consent_rejected;
            InitializeComponent();
            CheckboxCheckedChanged(null, null);
        }

        private void CheckboxCheckedChanged(object sender, RoutedEventArgs e)
        {
            Button_Continue.IsEnabled = Checkbox_AgreeToStore.IsChecked == true && Checkbox_AcceptLicense.IsChecked == true;
        }

        private void Button_Continue_Click(object sender, RoutedEventArgs e)
        {
            lock(Helpers.locker)
            {
                Options.Instance.user_consent_given = true;
                Options.Instance.SaveToFile(Options.Filepath);
                on_consent_given();
            }
            this.Close();
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            lock (Helpers.locker)
            {
                if (!Options.Instance.user_consent_given)
                    on_consent_rejected();
            }
        }
    }
}
