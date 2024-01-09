using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Interaction logic for SignUp.xaml
    /// </summary>
    public partial class SignUp : Page
    {
        private string username = null;
        private string password = null;
        private string email = null;
        private string password2 = null;
        
        public SignUp()
        {
            InitializeComponent();
        }
        private void New_Error_Window(string error)
        {
            ErrorWindow errorWindow = new ErrorWindow();
            errorWindow.ErrorWin.Text = error;
            errorWindow.Show();
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            MongoClient dbClient = new MongoClient("mongodb+srv://Nimrod:NimrodBenHamo85@cluster0.nvpsjki.mongodb.net/");
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");
            bool verify = true;
            //need to check all info is diffrent from null and from other users
            //only then insert to db
            //check that password is equal to password2 
            if (this.password != this.password2)
            {
                verify = false;
                string error = "passwords does not match";
                New_Error_Window(error);
            }
            var existingUser = collection.Find(new BsonDocument("username", this.username)).FirstOrDefault();
            if (existingUser != null)
            {
                //username exists, new user can't be inserted
                verify = false;
                string error = "Username already exists. Choose a different username.";
                New_Error_Window(error);
            }
            if (!IsEmailValid(this.email))
            {
                verify = false;
                string error = "Invalid email address";
                New_Error_Window(error);
            }
            if (verify)
            {
                collection.InsertOne(new BsonDocument { { "username", this.username }, { "password", this.password }, { "email", this.email} });
                
                string error = "Document inserted successfully!";
                New_Error_Window(error);
                
                LogIn login = new LogIn();
                this.NavigationService.Navigate(login);
            }
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.username = Username.Text;        
        }
        private void UsernameTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(Username.Text == "Enter your Username")
                Username.Text = string.Empty;
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.email = Email.Text;               
        }
        private void EmailTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Email.Text == "example@email.com")
                Email.Text = string.Empty;
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

        private void Passwaord2TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.password2 = ReTypePassword.Text;
        }
        private void Password2TextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ReTypePassword.Text == "ReType your Password")
                ReTypePassword.Text = string.Empty;
        }

        private static bool IsEmailValid(string email)
        {
            var valid = true;

            try
            {
                var emailAddress = new MailAddress(email);
            }
            catch
            {
                valid = false;
            }

            return valid;
        }
    }
}
