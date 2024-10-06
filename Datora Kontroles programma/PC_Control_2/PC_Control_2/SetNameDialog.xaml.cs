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

namespace PC_Control_2
{
    /// <summary>
    /// Interaction logic for SetNameDialog.xaml
    /// </summary>
    public partial class SetNameDialog : Window
    {
        public string ClientName { get; private set; }

        public SetNameDialog(string currentName)
        {
            InitializeComponent();
            NameTextBox.Text = currentName;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ClientName = NameTextBox.Text;
            DialogResult = true;
        }
    }
}