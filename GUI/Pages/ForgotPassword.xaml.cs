using GUI.MongoDB;
using GUI.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
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
        private string username;
        private string email;
        private string password;
        public ForgotPassword()
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
        
        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.email = Email.Text;
        }
        private void EmailTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Email.Text == "temp@gmail.com")
                Email.Text = string.Empty;
        }
        
        private void NewPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.password = NewPassword.Text;
        }
        private void NewPasswordTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (NewPassword.Text == "new password")
                NewPassword.Text = string.Empty;
        }
        
        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            int codeToCheck = new Random().Next(1000, 10000);
            if (MongoDBfunctions.IsUsernameExists(username, email))
            {
                
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
                    smtpClient.Send(senderEmail, this.email, "Reset password code", $"Your code for reseting the password is {codeToCheck}");
                    error = "Email sent successfully!";
                }
                catch (SmtpException ex)
                {
                    error = "SMTP error occurred: " + ex.Message;
                }
                catch (InvalidOperationException ex)
                {
                    error = "Invalid operation: " + ex.Message;
                }
                catch (ArgumentException ex)
                {
                    error = "Argument error: " + ex.Message;
                }
                catch (Exception ex)
                {
                    error = "An error occurred: " + ex.Message;
                }

                Verify verifyWindow = new Verify(codeToCheck, this.username, this.password, this.email, "RESET");
                this.NavigationService.Navigate(verifyWindow);
            }
            else
            {
                string error = "Username or email do not match";
                New_Error_Window(error);
            }
        }
        private void New_Error_Window(string error)
        {
            ErrorWindow errorWindow = new ErrorWindow();
            errorWindow.ErrorWin.Text = error;
            errorWindow.Show();
        }

        private void BackArrow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LogIn logIn = new LogIn();
            this.NavigationService.Navigate(logIn);
        }
    }
}
