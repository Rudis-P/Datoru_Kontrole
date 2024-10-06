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

namespace PC_C_Client
{
    /// <summary>
    /// Interaction logic for Parole.xaml
    /// </summary>
    public partial class Parole : Window
    {
        public Parole()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {


            if (ParolesLogs2.Password == "31112")
            {
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                string message = "Parole Nav Pareiza";
                MessageBox.Show(message);


            }
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            var window = (Window)sender;
            window.Topmost = true;
        }

        private void Window_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var window = (Window)sender;
            window.Topmost = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           // ParolesLogs.Text = "1" + ParolesLogs.Text;
            ParolesLogs2.Password = "1" + ParolesLogs2.Password;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
          //  ParolesLogs.Text = "2" + ParolesLogs.Text;
            ParolesLogs2.Password = "2" + ParolesLogs2.Password;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
           // ParolesLogs.Text = "3" + ParolesLogs.Text;
            ParolesLogs2.Password = "3" + ParolesLogs2.Password;
        }
    }
}
