using GUI.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using RemoteSync;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Packaging;
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

namespace GUI.Pages
{
    /// <summary>
    /// Interaction logic for Verify.xaml
    /// </summary>
    public partial class Verify : Page
    {
        private string codeEntered;
        private int codeToCheck;

        private string username;
        private string password;
        private string email;
        private string func;

        public Verify(int codeToCheck, string username, string password, string email, string func)
        {
            this.codeToCheck = codeToCheck;
            this.username = username;
            this.password = password;
            this.email = email;
            this.func = func;
            InitializeComponent();
        }

        private void Code_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.codeEntered = Code.Text;
        }

        private void Code_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(Code.Text == "your code")
                Code.Text = string.Empty;
        }

        private void New_Error_Window(string error)
        {
            ErrorWindow errorWindow = new ErrorWindow();
            errorWindow.ErrorWin.Text = error;
            errorWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            errorWindow.Show();
        }

        private void Enter_Click(object sender, RoutedEventArgs e)
        {
            if(this.func == "NEW")
            {
                if (int.Parse(codeEntered) == codeToCheck)
                {
                    var collection = MongoDBfunctions.GetUserInfoCollection();

                    MongoDBfunctions.InsertUser(this.username, this.password, this.email, collection);

                    string error = "Welcome To RemoteSync!\nPlease Login again.";
                    New_Error_Window(error);

                    LogIn login = new LogIn();
                    this.NavigationService.Navigate(login);
                }
                else
                {
                    string error = "code dosent match";
                    New_Error_Window(error);
                }
            }
            if(this.func == "RESET")
            {
                if (int.Parse(codeEntered) == codeToCheck)
                {
                    var collection = MongoDBfunctions.GetUserInfoCollection();
                    MongoDBfunctions.ChangePassword(username, password, collection);

                    string error = "Password chenged!\nPlease Login again.";
                    New_Error_Window(error);

                    LogIn login = new LogIn();
                    this.NavigationService.Navigate(login);
                }
                else
                {
                    string error = "code dosent match";
                    New_Error_Window(error);
                }
            }

        }
    }
}
