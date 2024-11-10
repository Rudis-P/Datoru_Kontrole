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
    /// Interaction logic for SetMoneyDialog.xaml
    /// </summary>
    public partial class SetMoneyDialog : Window
    {
        public double MoneyMultiplier
        {
            get; private set;
        }

        public SetMoneyDialog(double moneyMultiplier)
        {
            InitializeComponent();
            MoneyTextBox.Text = Convert.ToString(moneyMultiplier);
        }

        private void ConfirmClick_Click(object sender, RoutedEventArgs e)
        {
            MoneyMultiplier = Convert.ToDouble(MoneyTextBox.Text);
            DialogResult = true;
        }

    }
}
