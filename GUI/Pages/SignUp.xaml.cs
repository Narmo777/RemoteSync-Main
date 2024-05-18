using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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
using System.Net.Mime;
using GUI.Pages;
using System.Collections;
using GUI.MongoDB;

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
            //MongoClient dbClient = new MongoClient("mongodb+srv://Nimrod:NimrodBenHamo85@cluster0.nvpsjki.mongodb.net/");
            //var db = dbClient.GetDatabase("LoginSystem");
            //var collection = db.GetCollection<BsonDocument>("UserInfo");
            bool verify = true;
            bool existingUser = MongoDBfunctions.IsUsernameExists(this.username);
            //need to check all info is diffrent from null and from other users
            //only then insert to db
            //check that password is equal to password2 
            if (this.password != this.password2)
            {
                verify = false;
                string error = "passwords does not match";
                New_Error_Window(error);
            }
            //var existingUser = collection.Find(new BsonDocument("username", this.username)).FirstOrDefault();

            else if (existingUser)
            {
                //username exists, new user can't be inserted
                verify = false;
                string error = "Username already exists. Choose a different username.";
                New_Error_Window(error);
            }
            else if (!IsUsernameValid(this.username))
            {
                verify = false;
                string error = "Username cannot contain spaces";
                New_Error_Window(error);
            }
            else if (!IsEmailValid(this.email))
            {
                verify = false;
                string error = "Invalid email address";
                New_Error_Window(error);
            }
            int codeToCheck = new Random().Next(1000, 10000);
            if (IsEmailValid(this.email) && verify)
            {
                //if email is valid, send a code and validate


                // Sender's email address and password
                string senderEmail = "remotesyncvalidate@gmail.com";
                string senderPassword = "aidb setv vykp bbik";
                string error = "";
                try
                {
                    var smtpClient = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential(senderEmail, senderPassword),
                        EnableSsl = true,
                    };
                    smtpClient.Send(senderEmail, this.email, "Validation code", $"Your validation code is {codeToCheck}");
                    error = "Email sent successfully!";
                }
                catch (SmtpException ex)
                {
                    error = "SMTP error occurred: " + ex.Message;
                    verify = false;
                }
                catch (InvalidOperationException ex)
                {
                    error = "Invalid operation: " + ex.Message;
                    verify = false;
                }
                catch (ArgumentException ex)
                {
                    error = "Argument error: " + ex.Message;
                    verify = false;
                }
                catch (Exception ex)
                {
                    error = "An error occurred: " + ex.Message;
                    verify = false;
                }
                //New_Error_Window(error);
            }

            if (verify)
            {
                Verify verifyWindow = new Verify(codeToCheck, this.username, this.password, this.email, "NEW");
                this.NavigationService.Navigate(verifyWindow);
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
        private static bool IsUsernameValid(string username)
        {
            // Check for spaces
            if (username.Contains(" "))
            {
                return false;
            }

            //// Check for numbers
            //if (Regex.IsMatch(username, @"\d"))
            //{
            //    return false;
            //}

            //// Check for at least one capital letter
            //if (!Regex.IsMatch(username, @"[A-Z]"))
            //{
            //    return false;
            //}

            // If all conditions pass, return true
            return true;
        }

        private void BackArrow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LogIn logIn = new LogIn();
            this.NavigationService.Navigate(logIn);
        }
    }
}
