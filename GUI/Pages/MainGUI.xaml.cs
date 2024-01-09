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
        }
        private string id;
        private async void Get_Click(object sender, RoutedEventArgs e)
        {
            var baseMsg = new Packet(RequestType.Get, "");
            await Connect(Server.Server.IP, 300, baseMsg);
            
        }

        private async void Kill_Click(object sender, RoutedEventArgs e)
        {
            var baseMsg = new Packet(RequestType.Kill, this.id);
            await Connect(Server.Server.IP, 300, baseMsg);
        }

        private void Rsc_Click(object sender, RoutedEventArgs e)
        {

        }
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
                foreach (var item in resp.GetContentAsString().Split('#').Select(x => x.Split('|')).Select(x => (int.Parse(x[0]), x[1])))
                {
                    MainListBox.Items.Add(item);
                };
            }
            else if (baseMsg.RequestType == RequestType.Kill)
            {
                //clear all of the items in MainListBox 
                //refresh
            }
            stream.Close();
        }

        public async Task Refresh(NetworkStream stream) //func dosent work, code crashes
        {
            var refresh = new Packet(RequestType.Get, "");            

            await stream.WriteAsync((byte[])refresh, 0, refresh.DataSize + Packet.HeaderSize); //sends the messsage
            await stream.FlushAsync();

            var resp = await Packet.FromNetworkStream(stream); //wait for the server response
            foreach (var item in resp.GetContentAsString().Split('#').Select(x => x.Split('|')).Select(x => (int.Parse(x[0]), x[1])))
            {
                MainListBox.Items.Add(item);
            };
        }

        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProcces = MainListBox.SelectedItem.ToString();
            string[] procces = selectedProcces.Split(',', '(');
            var proccesId = procces[1];
            
            this.id = proccesId;
        }
    }
}
