using GUI.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
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
    /// Interaction logic for LogIn.xaml
    /// </summary>
    public partial class LogIn : Page
    {
        private string username = null;
        private string password = null;

        public LogIn()
        {
            InitializeComponent();
        }

        private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.username = Username.Text;
        }
        private void UsernameTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Username.Text == "Enter your Username")
                Username.Text = string.Empty;
        }

        private void PasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.password = Password.Text;
        }
        private void PasswordTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Password.Text == "Enter your Password")
                Password.Text = string.Empty;
        }

        private void New_Error_Window(string error)
        {
            ErrorWindow errorWindow = new ErrorWindow();
            errorWindow.ErrorWin.Text = error;
            errorWindow.Show();
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ForgotPassword forgot = new ForgotPassword();
            this.NavigationService.Navigate(forgot);
        }

        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            var collection = MongoDBfunctions.GetUserInfoCollection();

            bool existingUser = MongoDBfunctions.IsUserSignedUp(this.username, this.password, collection);
            if (existingUser)
            {
                //connect to server
                //move to main page

                //string error = "Good! \nthis will move you to the main page";
                //New_Error_Window(error);

                MainGUI main = new MainGUI();
                this.NavigationService.Navigate(main);
            }
            else
            {
                string error = "Username or Password do not match";
                New_Error_Window(error);
            }
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            SignUp signup = new SignUp();
            this.NavigationService.Navigate(signup);
        }
    }
}