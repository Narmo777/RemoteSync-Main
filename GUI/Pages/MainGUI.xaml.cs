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
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using System.Windows.Threading;
using ConsoleApp2.MongoDB;
using GUI.MongoDB;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;

namespace RemoteSync
{
    /// <summary>
    /// Interaction logic for MainGUI.xaml
    /// </summary>
    public partial class MainGUI : Page
    {
        private string technicianUsername;
        public static List<Tuple<string, string,int>> currentClientList = new List<Tuple<string, string, int>>();
        public static int clientsNumber = 0;
        private static int clientIndex;
                
        public MainGUI(string name)
        {
            technicianUsername = name;
            InitializeComponent();
            InitTimer();
        }
        
        public void Comp_Click(object sender, RoutedEventArgs e)
        {
            TabItem newComputer = new TabItem
            {
                Header = "temp",
            };
            ComputerTabs.Items.Add(newComputer);
        }
        private async void Get_Click(object sender, RoutedEventArgs e)
        {
            var baseMsg = new Packet(RequestType.Get, "");
            await Connect(GetCurrentIP(), 300, baseMsg);
            
        }
        private async void Kill_Click(object sender, RoutedEventArgs e)
        {
            var selectedId = this.id;
            skipOneTime = true;
            GetCurrentListBox().SelectedItem = null;
            var baseMsg = new Packet(RequestType.Kill, selectedId);
            await Connect(GetCurrentIP(), 300, baseMsg);            
        }
        private void Rsc_Click(object sender, RoutedEventArgs e)
        {

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
                //var selectedProcces = GetCurrentListBox().SelectedItem.ToString();
                //string[] procces = selectedProcces.Split(',', '(');
                //var proccesId = procces[1];

                //this.id = proccesId;


                ListBox currentListBox = GetCurrentListBox();
                if (currentListBox.SelectedItem != null)
                {
                    // Cast the selected item to ListItem
                    var selectedProcess = currentListBox.SelectedItem as ListItem;

                    if (selectedProcess != null)
                    {
                        // Access the Part2 property
                        this.id = selectedProcess.ID.ToString();
                    }
                }
            }
        }
        
        //change index when going through tabs
        private void TabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tabHeader = "";
            if (ComputerTabs.SelectedItem is TabItem selectedTab)
            {
                // Retrieve the header of the selected TabItem
                tabHeader = selectedTab.Header?.ToString();
            }

            foreach (var client in currentClientList)
            {
                if (client.Item1 == tabHeader)
                {
                    clientIndex = client.Item3;
                }
            }
        }        
        
        //refresh and helpers
        public static async Task<Packet> SendRequest(Packet p, string IP)
        {
            using (var clinet = new TcpClient(IP, Server.Server.PORT))
            {
                using (var stream = clinet.GetStream())
                {

                    await stream.WriteAsync((byte[])p, 0, p.DataSize + Packet.HeaderSize);
                    await stream.FlushAsync();

                    return await Packet.FromNetworkStream(stream);
                }
            }
        }
        public async Task<List<(int, string, string)>> GetProcesscesFromServer(string ip) 
        {
            var requset = await SendRequest(new Packet(RequestType.Get, ""), ip);
            var content = requset.GetContentAsString();

            var result = new List<(int, string, string)>();

            var parts = content.Split('|');
            foreach (var part in parts)
            {
                var components = part.Split('#');
                if (components.Length == 3)
                {
                    if (int.TryParse(components[0], out int number))
                    {
                        result.Add((number, components[1], components[2]));
                    }
                }
            }

            return result;
        } 
        private async Task RefreshScreenFromServer(string username)
        {
            var serverClientList = await MongoDBfunctions.GetAllClientsAsync(username);
            bool exist = false;

            if (serverClientList != null)
            {
                if (clientsNumber == 0)
                {
                    //client list is empty, add all of the client from mongo
                    foreach (var tuple in serverClientList)
                    {
                        currentClientList.Add(tuple);
                        clientsNumber++;

                        //add here the new tab for the client
                        TabItem newTabItem = new TabItem
                        {
                            Header = tuple.Item1.ToString()
                        };
                        ListBox listbox = new ListBox();
                        listbox.Name = tuple.Item1.ToString() + "_listbox";
                        listbox.Height = 600;

                        newTabItem.Content = listbox;
                        listbox.SelectionChanged += MainListBox_SelectionChanged; //enables the useage of the MainListBox_SelectionChanged function                        
                        listbox.ItemTemplate = ListItemTemplate();

                        CmpTabs.Items.Add(newTabItem);

                    }
                }
                else
                {
                    foreach (var tuple in serverClientList)
                    {
                        foreach (var client in currentClientList)
                        {
                            if (client.Item2 == tuple.Item2)
                            {
                                exist = true;
                                break;
                            }
                        }
                        if (exist == false)
                        {
                            //the current client is not inside the tabs, so we will add it
                            currentClientList.Add(tuple);
                            clientsNumber++;
                            
                            //add here the new tab for the client
                            TabItem newTabItem = new TabItem
                            {
                                Header = tuple.Item1.ToString()
                            };
                            ListBox listbox = new ListBox();
                            listbox.Name = tuple.Item1.ToString() + "_listbox";
                            listbox.Height = 600;

                            newTabItem.Content = listbox;
                            listbox.SelectionChanged += MainListBox_SelectionChanged;
                            listbox.ItemTemplate= ListItemTemplate();

                            CmpTabs.Items.Add(newTabItem);

                        }
                    }
                }
            }
            if (currentClientList != null)
            {
                foreach (var client in currentClientList.ToList())
                {
                    var name = client.Item1;
                    var ip = client.Item2;
                    var id = client.Item3;

                    ListBox clientListBox = new ListBox();

                    try //try to update the process list for each client, if falied, client has disconnected
                    {
                        foreach (var item in ComputerTabs.Items)
                        {
                            var tab = item as TabItem;
                            if (tab != null)
                            {
                                if (tab.Header != null && tab.Header.ToString() == name)
                                {
                                    clientListBox = tab.Content as ListBox;
                                }
                            }
                        }

                        var newProcesses = await GetProcesscesFromServer(ip);
                        Dispatcher.Invoke(() => UpdateProcessList(newProcesses, clientListBox));
                    }
                    catch (Exception e)
                    {
                        currentClientList.Remove(client);
                        RemoveCurrentTab(name);
                        clientsNumber--;
                        await MongoDBfunctions.RemoveDisconnectedClientAsync(technicianUsername, ip);
                    }
                }
            }
        }
        private ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();
        private void UpdateProcessList(List<(int, string, string)> newProcesses, ListBox clientListBox)
        {
            var ProcessesListItem = new List<ListItem>();
            // Convert newProcesses to ListItem objects
            foreach (var process in newProcesses)
            {
                ListItem current = new ListItem { Name = process.Item2, ID = process.Item1, CPU = process.Item3 };
                ProcessesListItem.Add(current);
            }

            ListBox currentListBox = clientListBox;

            // Remove duplicates from the ListBox
            foreach (var newItem in ProcessesListItem)
            {
                bool alreadyExists = false;
                foreach (var listBoxItem in currentListBox.Items)
                {
                    var existingItem = listBoxItem as ListItem;
                    if (existingItem != null && existingItem.ID == newItem.ID && existingItem.Name == newItem.Name)
                    {
                        alreadyExists = true;
                        break;
                    }
                }
                if (!alreadyExists)
                {
                    // Add new item to ListBox only if it contains the search term
                    if (search == "" || newItem.Name.Contains(search))
                    {
                        items.Add(newItem);
                        currentListBox.Items.Add(newItem);
                    }
                }
            }

            // Remove items that do not exist in newProcesses
            for (int i = currentListBox.Items.Count - 1; i >= 0; i--)
            {
                var item = currentListBox.Items[i] as ListItem;
                if (item != null && !ProcessesListItem.Any(p => p.ID == item.ID && p.Name == item.Name))
                {
                    currentListBox.Items.RemoveAt(i);
                }
            }
            // Remove items that do not contain the search term
            if (search != "")
            {
                for (int i = currentListBox.Items.Count - 1; i >= 0; i--)
                {
                    var item = currentListBox.Items[i] as ListItem;
                    if (item != null && !item.Name.Contains(search))
                    {
                        currentListBox.Items.RemoveAt(i);
                    }
                }
            }

        }


        private async Task RefreshScreenFromServerNew(string username)
        {
            var serverClientList = await MongoDBfunctions.GetAllClientsAsync(username);
            bool exist = false;

            if (serverClientList != null)
            {
                if (clientsNumber == 0)
                {
                    //client list is empty, add all of the client from mongo
                    foreach (var tuple in serverClientList)
                    {
                        currentClientList.Add(tuple);
                        clientsNumber++;

                        // Add here the new tab for the client
                        TabItem newTabItem = new TabItem
                        {
                            Header = tuple.Item1.ToString()
                        };

                        // Replace ListBox with ListView
                        ListView listView = new ListView
                        {
                            Name = tuple.Item1.ToString() + "_listview",
                            Height = 600
                        };

                        // Set the GridView as the View for the ListView
                        listView.View = ListViewGrid();

                        newTabItem.Content = listView;
                        listView.SelectionChanged += MainListBox_SelectionChanged; // Enables the usage of the MainListBox_SelectionChanged function                        
                        listView.ItemTemplate = ListViewDataTemplate();

                        CmpTabs.Items.Add(newTabItem);
                    }
                }
                else
                {
                    foreach (var tuple in serverClientList)
                    {
                        foreach (var client in currentClientList)
                        {
                            if (client.Item2 == tuple.Item2)
                            {
                                exist = true;
                                break;
                            }
                        }
                        if (exist == false)
                        {
                            //the current client is not inside the tabs, so we will add it
                            currentClientList.Add(tuple);
                            clientsNumber++;

                            // Add here the new tab for the client
                            TabItem newTabItem = new TabItem
                            {
                                Header = tuple.Item1.ToString()
                            };

                            // Replace ListBox with ListView
                            ListView listView = new ListView
                            {
                                Name = tuple.Item1.ToString() + "_listview",
                                Height = 600
                            };

                            // Set the GridView as the View for the ListView
                            listView.View = ListViewGrid();

                            newTabItem.Content = listView;
                            listView.SelectionChanged += MainListBox_SelectionChanged; // Enables the usage of the MainListBox_SelectionChanged function                        
                            listView.ItemTemplate = ListViewDataTemplate();

                            CmpTabs.Items.Add(newTabItem);
                        }
                    }
                }
            }
            if (currentClientList != null)
            {
                foreach (var client in currentClientList.ToList())
                {
                    var name = client.Item1;
                    var ip = client.Item2;
                    var id = client.Item3;

                    ListView clientListView = new ListView();

                    try //try to update the process list for each client, if falied, client has disconnected
                    {
                        foreach (var item in ComputerTabs.Items)
                        {
                            var tab = item as TabItem;
                            if (tab != null)
                            {
                                if (tab.Header != null && tab.Header.ToString() == name)
                                {
                                    clientListView = tab.Content as ListView;
                                }
                            }
                        }

                        var newProcesses = await GetProcesscesFromServer(ip);
                        Dispatcher.Invoke(() => UpdateProcessListNew(newProcesses, clientListView));
                    }
                    catch (Exception e)
                    {
                        currentClientList.Remove(client);
                        RemoveCurrentTab(name);
                        clientsNumber--;
                        await MongoDBfunctions.RemoveDisconnectedClientAsync(technicianUsername, ip);
                    }
                }
            }
        }
        private void UpdateProcessListNew(List<(int, string, string)> newProcesses, ListView clientListView)
        {
            var ProcessesListItem = new List<ProcessItem>();

            // Convert newProcesses to ProcessItem objects
            foreach (var process in newProcesses)
            {
                ProcessItem current = new ProcessItem { ID = process.Item1, Name = process.Item2, CPU = process.Item3 };
                ProcessesListItem.Add(current);
            }

            // Dictionary to store unique process items by ID
            var processDictionary = new Dictionary<int, ProcessItem>();

            // Convert newProcesses to ProcessItem objects and keep only the last occurrence for each ID
            foreach (var process in newProcesses)
            {
                var current = new ProcessItem { ID = process.Item1, Name = process.Item2, CPU = process.Item3 };

                // Replace existing item if there is a duplicate ID
                processDictionary[current.ID] = current;
            }

            ListView currentListView = clientListView;

            // Clear the ListView before updating with unique process items
            currentListView.Items.Clear();

            // Update ListView with unique process items
            foreach (var processItem in processDictionary.Values)
            {
                currentListView.Items.Add(processItem);
            }

            // Remove items that do not exist in newProcesses
            for (int i = currentListView.Items.Count - 1; i >= 0; i--)
            {
                var item = currentListView.Items[i] as ProcessItem;
                if (item != null && !processDictionary.Any(p => p.Value.ID == item.ID))
                {
                    currentListView.Items.RemoveAt(i);
                }
            }
        }


        //refresh the clients
        public static TabControl CmpTabs;
        private void ComputerTabs_Loaded(object sender, RoutedEventArgs e)
        {
            CmpTabs = (sender as TabControl);
        }

        //setting timer for refresh
        private DispatcherTimer timer;
        public void InitTimer()
        {
            // Initialize the timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(2000); // Set the interval to 1000 milliseconds = 1 second           
            
            timer.Tick += async (sender, e) => await RefreshScreenFromServerNew(technicianUsername);

            // Start the timer
            timer.Start();
        }

        public static string GetCurrentIP()
        {
            string currentClientIP = "";
            foreach (var client in currentClientList)
            {
                if (client.Item3 == clientIndex)
                    currentClientIP = client.Item2;
            }

            return currentClientIP;
        }
        public ListBox GetCurrentListBox()
        {
            ListBox currentlistbox = new ListBox();
            string targetHeader = "";
            foreach (var client in currentClientList)
            {
                if (client.Item3 == clientIndex)
                    targetHeader = client.Item1;
            }
            foreach (var item in ComputerTabs.Items)
            {
                if (item is TabItem tabItem && tabItem.Header != null && tabItem.Header.ToString() == targetHeader)
                {
                    currentlistbox = tabItem.Content as ListBox;
                }
            }

            return currentlistbox;
        }
        public void RemoveCurrentTab(string header)
        {
            TabItem tabToRemove = null;

            // Find the TabItem to remove based on the specified header
            foreach (var item in ComputerTabs.Items)
            {
                if (item is TabItem tabItem && tabItem.Header != null && tabItem.Header.ToString() == header)
                {
                    tabToRemove = tabItem;
                    break; // Found the matching TabItem, exit the loop
                }
            }

            // Remove the TabItem outside of the loop to avoid modifying the collection during iteration
            if (tabToRemove != null)
            {
                ComputerTabs.Items.Remove(tabToRemove);
            }
        }

        public DataTemplate ListItemTemplate()
        {
            DataTemplate dataTemplate = new DataTemplate(typeof(ListItem));

            // Create a StackPanel as the root of the template
            FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            // Create the first TextBlock and bind it to Part1
            FrameworkElementFactory textBlock1Factory = new FrameworkElementFactory(typeof(TextBlock));
            textBlock1Factory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            textBlock1Factory.SetValue(TextBlock.MarginProperty, new Thickness(5));
            textBlock1Factory.SetValue(TextBlock.WidthProperty, 200.0);

            // Create the second TextBlock and bind it to Part2
            FrameworkElementFactory textBlock2Factory = new FrameworkElementFactory(typeof(TextBlock));
            textBlock2Factory.SetBinding(TextBlock.TextProperty, new Binding("ID"));
            textBlock2Factory.SetValue(TextBlock.MarginProperty, new Thickness(5));
            textBlock2Factory.SetValue(TextBlock.WidthProperty, 200.0);

            // Create the thrid TextBlock and bind it to Part3
            FrameworkElementFactory textBlock3Factory = new FrameworkElementFactory(typeof(TextBlock));
            textBlock3Factory.SetBinding(TextBlock.TextProperty, new Binding("CPU"));
            textBlock3Factory.SetValue(TextBlock.MarginProperty, new Thickness(5));
            textBlock3Factory.SetValue(TextBlock.WidthProperty, 200.0);

            // Add the TextBlocks to the StackPanel
            stackPanelFactory.AppendChild(textBlock1Factory);
            stackPanelFactory.AppendChild(textBlock2Factory);
            stackPanelFactory.AppendChild(textBlock3Factory);

            // Set the VisualTree of the DataTemplate
            dataTemplate.VisualTree = stackPanelFactory;

            // return DataTemplate
            return dataTemplate;
        }
        public DataTemplate ListViewDataTemplate()
        {
            DataTemplate dataTemplate = new DataTemplate();
            FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            FrameworkElementFactory nameTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            nameTextBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
            nameTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            nameTextBlock.SetValue(TextBlock.WidthProperty, 100.0);
            stackPanelFactory.AppendChild(nameTextBlock);

            FrameworkElementFactory idTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            idTextBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("ID"));
            idTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            idTextBlock.SetValue(TextBlock.WidthProperty, 50.0);
            stackPanelFactory.AppendChild(idTextBlock);

            FrameworkElementFactory cpuTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            cpuTextBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("CPU"));
            cpuTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            cpuTextBlock.SetValue(TextBlock.WidthProperty, 100.0);
            stackPanelFactory.AppendChild(cpuTextBlock);

            dataTemplate.VisualTree = stackPanelFactory;
            return dataTemplate;
        }
        public GridView ListViewGrid()
        {
            // Create and configure the GridView
            GridView gridView = new GridView();
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Name",
                DisplayMemberBinding = new System.Windows.Data.Binding("Name")
            }); //Name 
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Id",
                DisplayMemberBinding = new System.Windows.Data.Binding("Id")
            }); //Id
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "CPU",
                DisplayMemberBinding = new System.Windows.Data.Binding("CPU")
            }); //CPU

            return gridView;
        }
    }
    public class ListItem
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public string CPU { get; set; }
    }
    public class ProcessItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string CPU { get; set; }
        public ObservableCollection<ProcessItem> Children { get; set; } // To store child items

        public ProcessItem()
        {
            Children = new ObservableCollection<ProcessItem>();
        }
    }
}
