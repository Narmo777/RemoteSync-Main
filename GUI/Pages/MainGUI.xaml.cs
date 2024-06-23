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
using System.IO;

namespace RemoteSync
{
    /// <summary>
    /// Interaction logic for MainGUI.xaml
    /// </summary>
    public partial class MainGUI : Page
    {
        private string technicianUsername;
        public static List<Tuple<string, string,int>> currentClientList = new List<Tuple<string, string, int>>();
        public static int clientsNumber = 0;    //counts how many clients are connected
        private static int clientIndex;         //variable that changes when going to another tab 
        private string search = "";             //variable that changes according to search box
        private string id= null;                      //variable that changes when a process is clicked in the listbox
        private bool skipOneTime = false;       

        public MainGUI(string name)
        {
            technicianUsername = name;
            InitializeComponent();
            InitTimer();
        }
        

        public async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            /// <summary>
            /// Event handler for the Refresh button click event. 
            /// Refreshes the screen from the server asynchronously using the technician's username.
            /// </summary>

            await RefreshScreenFromServerNew(technicianUsername);
        }
        private async void Kill_Click(object sender, RoutedEventArgs e)
        {
            /// <summary>
            /// Event handler for the Kill button click event. 
            /// Sends a kill request packet to the server asynchronously with the currently selected ID.
            /// </summary>

            if (id != null)
            {
                var selectedId = this.id;
                skipOneTime = true;
                var baseMsg = new Packet(RequestType.Kill, selectedId);
                //await Connect(GetCurrentIP(), 300, baseMsg);
                await SendRequest(baseMsg, GetCurrentIP()); 
            }
            else
            {
                New_Error_Window("please select an app", "error");
            }
        }        
        private void Rsc_Click(object sender, RoutedEventArgs e)
        {
            int totalMemory = 0;
            double totalCPU = 0;

            foreach (var item in itemsDictionary.Values)
            {
                double currentCPU = double.Parse(item.CPU.Substring(0, item.CPU.Length - 1));
                int currentMemory = int.Parse(item.Memory.Substring(0, item.Memory.Length - 5));

                totalCPU += currentCPU;
                totalMemory += currentMemory;
                //if (currentCPU != 999)
                //{
                //    int currentMemory = int.Parse(item.Memory.Substring(0, item.Memory.Length - 5));

                //    totalCPU += currentCPU;
                //    totalMemory += currentMemory;
                //}
            }

            string ret = $"total CPU usage: {totalCPU}%\ntotal Memory usage: {totalMemory} Mb/s";
            New_Error_Window(ret, "resource");
        }
        private void File_Click(object sender, RoutedEventArgs e)
        {
            /// <summary>
            /// Event handler for the File button click event. 
            /// Generates a text file on the desktop with the current time as its name, containing values from a dictionary.
            /// </summary>

            // Get the current time
            DateTime now = DateTime.Now;

            // Format the current time as a string for the file name
            string fileName = now.ToString("HH-mm-ss") + ".txt";

            // Get the path to the desktop
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // Combine the desktop path with the file name
            string filePath = System.IO.Path.Combine(desktopPath, fileName);

            var sb = new StringBuilder();
            foreach (var item in itemsDictionary.Values)
            {
                sb.Append(item.ToString());
                sb.Append('\n');
            }

            // Define the content to write into the file
            string content = sb.ToString();

            // Create and write to the file
            File.WriteAllText(filePath, content);

            New_Error_Window("file generated", "file");
        }

        private void New_Error_Window(string error, string title)
        {
            /// <summary>
            /// Displays a new error window with a specified error message and title.
            /// </summary>

            ErrorWindow errorWindow = new ErrorWindow();
            errorWindow.Title = title;
            errorWindow.ErrorWin.Text = error;
            errorWindow.Show();
        }

        //search box
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
        public async Task<List<(int, string, string, string, string)>> GetProcesscesFromServer(string ip) 
        {
            var requset = await SendRequest(new Packet(RequestType.Get, ""), ip);
            var content = requset.GetContentAsString();

            var result = new List<(int, string, string, string, string)>();

            var parts = content.Split('|');
            foreach (var part in parts)
            {
                var components = part.Split('#');
                if (components.Length == 5)
                {
                    if (int.TryParse(components[0], out int number))
                    {
                        result.Add((number, components[1], components[2], components[3], components[4]));
                    }
                }
            }

            return result;
        }                 

        private Dictionary<string, ProcessItem> itemsDictionary = new Dictionary<string, ProcessItem>();
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Get the TreeView
            TreeView treeView = sender as TreeView;

            if (treeView != null)
            {
                // Get the selected item
                var selectedItem = treeView.SelectedItem as ProcessItem;
                this.id = selectedItem.ID.ToString();
            }
            Kill.IsEnabled = true;

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

                        // Create a new TreeView
                        TreeView treeView = new TreeView
                        {
                            Name = tuple.Item1.ToString() + "_treeview",
                            Height = 600
                        };

                        treeView.ItemTemplate = CreateTreeView();
                        treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
                        // Set the TreeView as the content of the TabItem
                        newTabItem.Content = treeView;

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

                            // Create a new TreeView
                            TreeView treeView = new TreeView
                            {
                                Name = tuple.Item1.ToString() + "_treeview",
                                Height = 600
                            };

                            // Define the HierarchicalDataTemplate
                            HierarchicalDataTemplate hdt = new HierarchicalDataTemplate
                            {
                                ItemsSource = new Binding("Children")
                            };

                            treeView.ItemTemplate = CreateTreeView();
                            // Set the TreeView as the content of the TabItem
                            newTabItem.Content = treeView;

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

                    TreeView clientTreeView = new TreeView();

                    try //try to update the process list for each client, if falied, client has disconnected
                    {
                        foreach (var item in ComputerTabs.Items)
                        {
                            var tab = item as TabItem;
                            if (tab != null)
                            {
                                if (tab.Header != null && tab.Header.ToString() == name)
                                {
                                    clientTreeView = tab.Content as TreeView;
                                }
                            }
                        }

                        var newProcesses = await GetProcesscesFromServer(ip);
                        Dispatcher.Invoke(() => UpdateProcessListNew(newProcesses, clientTreeView));
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
            if(serverClientList != null && itemsDictionary.Values.Count > 0)
            {
                Refresh.IsEnabled = true;
                Rsc.IsEnabled = true;
                GetFile.IsEnabled = true;

                LoadingGIF.Stop();
                LoadingGIF.Visibility = Visibility.Collapsed;
            }
        }
        private void UpdateProcessListNew(List<(int, string, string, string, string)> newProcesses, TreeView processTreeView)
        {
            // Dictionary to store parent process items by name
            var parentDictionary = new Dictionary<string, ProcessItem>();

            foreach (var process in newProcesses)
            {
                var newItem = new ProcessItem { ID = process.Item1, Name = process.Item2, CPU = process.Item3, Memory = process.Item4, PeakMemory = process.Item5 };

                if (parentDictionary.ContainsKey(newItem.Name))
                {
                    // Add as a child to the existing parent with the same name
                    parentDictionary[newItem.Name].Children.Add(newItem);
                }
                else
                {
                    // Create a new parent item
                    parentDictionary[newItem.Name] = newItem;
                }
            }

            // Get the current items in the TreeView
            var currentItems = processTreeView.Items.Cast<ProcessItem>().ToList();

            // Remove items that do not exist in newProcesses
            foreach (var item in currentItems)
            {
                if (!parentDictionary.ContainsKey(item.Name))
                {
                    processTreeView.Items.Remove(item);
                }
                if (!item.Name.Contains(search))
                {
                    processTreeView.Items.Remove(item);
                }
            }

            // Update or add new items
            foreach (var newItem in parentDictionary.Values)
            {
                var existingItem = processTreeView.Items.Cast<ProcessItem>().FirstOrDefault(i => i.Name == newItem.Name);
                if (existingItem != null)
                {
                    // Update existing item
                    existingItem.ID = newItem.ID;
                    existingItem.CPU = newItem.CPU;

                    // Update children
                    foreach (var child in newItem.Children)
                    {
                        if (!existingItem.Children.Any(c => c.ID == child.ID))
                        {
                            existingItem.Children.Add(child);
                        }
                    }
                }                
                else
                {
                    // Add new item
                    processTreeView.Items.Add(newItem);
                }
            }
            // Additional loop to remove items that don't contain the search string
            currentItems = processTreeView.Items.Cast<ProcessItem>().ToList();
            foreach (var item in currentItems)
            {
                if (!item.Name.ToLower().Contains(search))
                {
                    processTreeView.Items.Remove(item);
                }
            }
            itemsDictionary = parentDictionary;
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
        public TreeView GetCurrentTreeView()
        {
            if(currentClientList.Count > 0)
            {
                var header = "";
                var returnTreeView = new TreeView();

                foreach (var client in currentClientList)
                {
                    if (client.Item3 == clientIndex)
                    {
                        header = client.Item1;
                    }
                }

                foreach(TabItem tab in CmpTabs.Items)
                {                    
                    if(tab.Header.ToString() == header)
                    {
                        returnTreeView = tab.Content as TreeView;
                    }
                }

                return returnTreeView;
            }
            return null;
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

        private HierarchicalDataTemplate CreateTreeView()
        {
            // Define the HierarchicalDataTemplate for the TreeView items
            HierarchicalDataTemplate hierarchicalDataTemplate = new HierarchicalDataTemplate();
            hierarchicalDataTemplate.ItemsSource = new Binding("Children");

            // Create the StackPanel for the template
            FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            // Create and bind TextBlocks for Name, ID, and CPU
            FrameworkElementFactory nameTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            nameTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            nameTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            nameTextBlock.SetValue(TextBlock.WidthProperty, 200.0);

            FrameworkElementFactory idTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            idTextBlock.SetBinding(TextBlock.TextProperty, new Binding("ID"));
            idTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            idTextBlock.SetValue(TextBlock.WidthProperty, 100.0);

            FrameworkElementFactory cpuTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            cpuTextBlock.SetBinding(TextBlock.TextProperty, new Binding("CPU"));
            cpuTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            cpuTextBlock.SetValue(TextBlock.WidthProperty, 100.0);

            FrameworkElementFactory memoryTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            memoryTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Memory"));
            memoryTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            memoryTextBlock.SetValue(TextBlock.WidthProperty, 100.0);

            FrameworkElementFactory peakMemoryTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            peakMemoryTextBlock.SetBinding(TextBlock.TextProperty, new Binding("PeakMemory"));
            peakMemoryTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            peakMemoryTextBlock.SetValue(TextBlock.WidthProperty, 100.0);

            // Add TextBlocks to StackPanel
            stackPanelFactory.AppendChild(nameTextBlock);
            stackPanelFactory.AppendChild(idTextBlock);
            stackPanelFactory.AppendChild(cpuTextBlock);
            stackPanelFactory.AppendChild(memoryTextBlock);
            stackPanelFactory.AppendChild(peakMemoryTextBlock);

            // Set the VisualTree of the HierarchicalDataTemplate
            hierarchicalDataTemplate.VisualTree = stackPanelFactory;

            // Define the inner DataTemplate for child items
            DataTemplate innerDataTemplate = new DataTemplate();
            FrameworkElementFactory innerStackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            innerStackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            FrameworkElementFactory innerNameTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            innerNameTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            innerNameTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            innerNameTextBlock.SetValue(TextBlock.WidthProperty, 200.0);

            FrameworkElementFactory innerIdTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            innerIdTextBlock.SetBinding(TextBlock.TextProperty, new Binding("ID"));
            innerIdTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            innerIdTextBlock.SetValue(TextBlock.WidthProperty, 100.0);

            FrameworkElementFactory innerCpuTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            innerCpuTextBlock.SetBinding(TextBlock.TextProperty, new Binding("CPU"));
            innerCpuTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            innerCpuTextBlock.SetValue(TextBlock.WidthProperty, 100.0);

            FrameworkElementFactory innerMemoryTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            innerMemoryTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Memory"));
            innerMemoryTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            innerMemoryTextBlock.SetValue(TextBlock.WidthProperty, 100.0);

            FrameworkElementFactory innerPeakMemoryTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            innerPeakMemoryTextBlock.SetBinding(TextBlock.TextProperty, new Binding("PeakMemory"));
            innerPeakMemoryTextBlock.SetValue(TextBlock.MarginProperty, new Thickness(5));
            innerPeakMemoryTextBlock.SetValue(TextBlock.WidthProperty, 100.0);

            innerStackPanelFactory.AppendChild(innerNameTextBlock);
            innerStackPanelFactory.AppendChild(innerIdTextBlock);
            innerStackPanelFactory.AppendChild(innerCpuTextBlock);
            innerStackPanelFactory.AppendChild(innerMemoryTextBlock);
            innerStackPanelFactory.AppendChild(innerPeakMemoryTextBlock);

            innerDataTemplate.VisualTree = innerStackPanelFactory;

            // Set the ItemTemplate of the inner HierarchicalDataTemplate
            hierarchicalDataTemplate.ItemTemplate = innerDataTemplate;
            
            return hierarchicalDataTemplate;
        }
        private void AddHeader(TreeView tree)
        {
            // Create the header item
            TreeViewItem headerItem = new TreeViewItem();
            headerItem.IsExpanded = true;
            headerItem.IsEnabled = false; // Make the header item non-interactive

            // Create the StackPanel for the header
            StackPanel headerStackPanel = new StackPanel();
            headerStackPanel.Orientation = Orientation.Horizontal;

            // Create the header TextBlocks
            TextBlock headerNameTextBlock = new TextBlock();
            headerNameTextBlock.Text = "Name";
            headerNameTextBlock.Margin = new Thickness(5);
            headerNameTextBlock.Width = 200.0;

            TextBlock headerIdTextBlock = new TextBlock();
            headerIdTextBlock.Text = "ID";
            headerIdTextBlock.Margin = new Thickness(5);
            headerIdTextBlock.Width = 100.0;

            TextBlock headerCpuTextBlock = new TextBlock();
            headerCpuTextBlock.Text = "CPU";
            headerCpuTextBlock.Margin = new Thickness(5);
            headerCpuTextBlock.Width = 100.0;

            // Add header TextBlocks to header StackPanel
            headerStackPanel.Children.Add(headerNameTextBlock);
            headerStackPanel.Children.Add(headerIdTextBlock);
            headerStackPanel.Children.Add(headerCpuTextBlock);

            // Add the header StackPanel to the header TreeViewItem
            headerItem.Header = headerStackPanel;

            // Insert the header item as the first item in the TreeView
            tree.Items.Insert(0, headerItem);
        }

        private void MediaEnded_GIF(object sender, RoutedEventArgs e)
        {
            LoadingGIF.Position = new TimeSpan(0, 0, 1);
            LoadingGIF.Play();
        }
    }
    public class ProcessItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string CPU { get; set; }
        public string Memory { get; set; }
        public string PeakMemory { get; set; }
        public ObservableCollection<ProcessItem> Children { get; set; } // To store child items

        public ProcessItem()
        {
            Children = new ObservableCollection<ProcessItem>();
        }

        public override string ToString()
        {
            var childrenToString = string.Join("\n", Children.Select(c => c.ToString()));
            return $"ID: {ID}, Name: {Name}, CPU: {CPU} , Memory {Memory}, Peak Memory {PeakMemory}";
        }
    }
}
