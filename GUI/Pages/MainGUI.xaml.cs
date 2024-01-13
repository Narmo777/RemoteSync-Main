using Protocol;
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
using Client;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using System.Windows.Threading;

namespace RemoteSync
{
    /// <summary>
    /// Interaction logic for MainGUI.xaml
    /// </summary>
    public partial class MainGUI : Page
    {
        public MainGUI()
        {
            InitializeComponent();
            InitTimer();
        }

        private async void Get_Click(object sender, RoutedEventArgs e)
        {
            var baseMsg = new Packet(RequestType.Get, "");
            await Connect(Server.Server.IP, 300, baseMsg);
            
        }
        private async void Kill_Click(object sender, RoutedEventArgs e)
        {
            var selectedId = this.id;
            skipOneTime = true;
            MainListBox.SelectedItem = null;
            var baseMsg = new Packet(RequestType.Kill, selectedId);
            await Connect(Server.Server.IP, 300, baseMsg);            
        }

        private void Rsc_Click(object sender, RoutedEventArgs e)
        {

        }

        //connect the gui to the server
        public async Task Connect(string ip, int port, Packet p)
        {
            var clinet = new TcpClient(ip, port); //creates a new tcp client
            var stream = clinet.GetStream();

            var baseMsg = p;

            await stream.WriteAsync((byte[])baseMsg, 0, baseMsg.DataSize + Packet.HeaderSize); //sends the messsage
            await stream.FlushAsync();

            var resp = await Packet.FromNetworkStream(stream); //wait for the server response
            if (baseMsg.RequestType == RequestType.Get)
            {
                //RefreshProcesses();
                //foreach (var item in resp.GetContentAsString().Split('#').Select(x => x.Split('|')).Select(x => (int.Parse(x[0]), x[1])))
                //{
                //    MainListBox.Items.Add(item);
                //};

                //InitTimer();
            }
            else if (baseMsg.RequestType == RequestType.Kill)
            {
                //RefreshProcesses();
            }
            stream.Close();
        }


        //if process is clicked, his id will be saved to this.id
        private string id;
        private bool skipOneTime = false;
        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(skipOneTime)
            {
                skipOneTime = false;
            }
            else
            {
                var selectedProcces = MainListBox.SelectedItem.ToString();
                string[] procces = selectedProcces.Split(',', '(');
                var proccesId = procces[1];

                this.id = proccesId;
            }
        }
        
        //refresh and helpers
        private void RefreshProcesses()
        {
            // Get the new list of processes
            var newProcesses = Process.GetProcesses()
                                       .Select(p => $"{p.Id}|{p.ProcessName}")
                                       .ToList();

            // Add missing items
            foreach (var newItem in newProcesses)
            {
                if (!MainListBox.Items.Cast<(int, string)>().Any(existingItem => AreEqualProcesses(newItem, existingItem)))
                {
                    MainListBox.Items.Add(ParseProcessInfo(newItem));
                }
            }

            // Remove items that no longer exist
            foreach (var existingItem in MainListBox.Items.Cast<(int, string)>().ToList())
            {
                var existingProcess = $"{existingItem.Item1}|{existingItem.Item2}";
                if (!newProcesses.Contains(existingProcess))
                {
                    MainListBox.Items.Remove(existingItem);
                }
                if (existingItem.ToString().Contains("GUI")) //remove my app from the listbox
                {
                    MainListBox.Items.Remove(existingItem);
                }
                if (search != "")
                {
                    if (!existingItem.ToString().Contains(search))
                    {
                        MainListBox.Items.Remove(existingItem);
                    }
                }
            }
        }        
        private bool AreEqualProcesses(string processString1, (int, string) processInfo2) // Helper method to compare process information
        {
            var processInfo1 = ParseProcessInfo(processString1);
            return processInfo1.Item1 == processInfo2.Item1 && processInfo1.Item2 == processInfo2.Item2;
        }        
        private (int, string) ParseProcessInfo(string processString) // Helper method to parse process information from the string
        {
            var parts = processString.Split('|');
            return (int.Parse(parts[0]), parts[1]);
        }

        //setting timer for refresh
        private DispatcherTimer timer;
        public void InitTimer()
        {
            // Initialize the timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000); // Set the interval to 1000 milliseconds = 1 second
            timer.Tick += Timer_Tick;

            // Start the timer
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Code to be executed on each timer tick
            // This will be called every 1000 milliseconds (1 second) in this example
            RefreshProcesses();
        }

        //search box
        private string search = "";
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Search.Text != "Search")
                this.search = Search.Text;
        }
        private void SearchTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Search.Text == "Search")
                Search.Text = string.Empty;
        }
    }
}
