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

        public Verify(int codeToCheck, string username, string password, string email)
        {
            this.codeToCheck = codeToCheck;
            this.username = username;
            this.password = password;
            this.email = email;
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
            errorWindow.Show();
        }

        private void Enter_Click(object sender, RoutedEventArgs e)
        {
            if(int.Parse(codeEntered) == codeToCheck)
            {
                MongoClient dbClient = new MongoClient("mongodb+srv://Nimrod:NimrodBenHamo85@cluster0.nvpsjki.mongodb.net/");
                var db = dbClient.GetDatabase("LoginSystem");
                var collection = db.GetCollection<BsonDocument>("UserInfo");

                collection.InsertOne(new BsonDocument { { "username", this.username }, { "password", this.password }, { "email", this.email } });

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
    }
}
