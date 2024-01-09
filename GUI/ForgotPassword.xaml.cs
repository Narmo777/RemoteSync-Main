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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RemoteSync
{
    /// <summary>
    /// Interaction logic for ForgotPassword.xaml
    /// </summary>
    public partial class ForgotPassword : Page
    {
        public ForgotPassword()
        {
            InitializeComponent();
        }

        private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void PasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void LogIn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void EmailTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Email.Text == "temp@gmail.com")
                Email.Text = string.Empty;
        }
        private void UsernameTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Username.Text == "Enter your Username")
                Username.Text = string.Empty;
        }
    }
}
