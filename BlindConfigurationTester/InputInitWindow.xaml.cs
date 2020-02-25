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
    /// Interaction logic for InputInitWindow.xaml
    /// </summary>
    public partial class InputInitWindow : Window
    {
        public InputInitWindow()
        {
            InitializeComponent();

            Debug.Assert(!input.IsLoaded);
            input.KeyboardFilterMode = Interceptor.KeyboardFilterMode.All;
            input.OnKeyPressed += Input_OnKeyPressed;
            input.Load();
        }

        private void Input_OnKeyPressed(object sender, Interceptor.KeyPressedEventArgs e)
        {
            input.OnKeyPressed -= Input_OnKeyPressed;
            Dispatcher.BeginInvoke((Action)(() => { Close(); }));
        }

        public static Interceptor.Input input = new Interceptor.Input();

    }
}
