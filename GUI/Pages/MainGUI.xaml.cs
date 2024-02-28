﻿using Protocol;
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
        private void Comp_Click(object sender, RoutedEventArgs e)
        {
            TabItem newComputer = new TabItem
            {
                Header = "computer 3"
            };
            ComputerTabs.Items.Add(newComputer);
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
        public static async Task<Packet> SendRequest(Packet p)
        {
            using (var clinet = new TcpClient(Server.Server.IP, Server.Server.PORT))
            {
                using (var stream = clinet.GetStream())
                {

                    await stream.WriteAsync((byte[])p, 0, p.DataSize + Packet.HeaderSize);
                    await stream.FlushAsync();

                    return await Packet.FromNetworkStream(stream);
                }
            }
        }
        public async Task<List<(int, string)>> GetProcesscesFromServer() => (await SendRequest(new Packet(RequestType.Get, ""))).GetContentAsString().Split('#').Select(x => x.Split('|')).Select(x => (int.Parse(x[0]), x[1])).ToList();
        private async Task RefreshScreenFromServer()
        {
            var newProcesses = await GetProcesscesFromServer();
            Dispatcher.Invoke(() => UpdateProcessList(newProcesses));
        }
        private void UpdateProcessList(List<(int, string)> newProcesses)
        {
            foreach (var newItem in newProcesses)
            {
                if (!MainListBox.Items.Cast<(int, string)>().Any(existingItem => existingItem.Item1 == newItem.Item1))
                {
                    MainListBox.Items.Add(newItem);
                }
            }

            // Remove items that no longer exist
            foreach (var existingItem in MainListBox.Items.Cast<(int, string)>().ToList())
            {
                if (!newProcesses.Contains(existingItem))
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

        //setting timer for refresh
        private DispatcherTimer timer;
        public void InitTimer()
        {
            // Initialize the timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000); // Set the interval to 1000 milliseconds = 1 second
            timer.Tick += async (sender, e) => await RefreshScreenFromServer();

            // Start the timer
            timer.Start();
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
