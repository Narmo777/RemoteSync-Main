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
        public static int clientsNumber = 0;
        private static int clientIndex;
                
        public MainGUI(string name)
        {
            technicianUsername = name;
            InitializeComponent();
            InitTimer();
        }
        
        public async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshScreenFromServerNew(technicianUsername);
        }
        private async void Kill_Click(object sender, RoutedEventArgs e)
        {
            var selectedId = this.id;
            skipOneTime = true;
            //GetCurrentListBox().SelectedItem = null;
            var baseMsg = new Packet(RequestType.Kill, selectedId);
            await Connect(GetCurrentIP(), 300, baseMsg);            
        }        
        private void Rsc_Click(object sender, RoutedEventArgs e)
        {
            New_Error_Window("button currently not in use", "error");
        }
        private void File_Click(object sender, RoutedEventArgs e)
        {
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
            ErrorWindow errorWindow = new ErrorWindow();
            errorWindow.Title = title;
            errorWindow.ErrorWin.Text = error;
            errorWindow.Show();
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
        
        //old refresh
        //private async Task RefreshScreenFromServer(string username)
        //{
        //    var serverClientList = await MongoDBfunctions.GetAllClientsAsync(username);
        //    bool exist = false;

        //    if (serverClientList != null)
        //    {
        //        if (clientsNumber == 0)
        //        {
        //            //client list is empty, add all of the client from mongo
        //            foreach (var tuple in serverClientList)
        //            {
        //                currentClientList.Add(tuple);
        //                clientsNumber++;

        //                //add here the new tab for the client
        //                TabItem newTabItem = new TabItem
        //                {
        //                    Header = tuple.Item1.ToString()
        //                };
        //                ListBox listbox = new ListBox();
        //                listbox.Name = tuple.Item1.ToString() + "_listbox";
        //                listbox.Height = 600;

        //                newTabItem.Content = listbox;
        //                listbox.SelectionChanged += MainListBox_SelectionChanged; //enables the useage of the MainListBox_SelectionChanged function                        
        //                listbox.ItemTemplate = ListItemTemplate();

        //                CmpTabs.Items.Add(newTabItem);

        //            }
        //        }
        //        else
        //        {
        //            foreach (var tuple in serverClientList)
        //            {
        //                foreach (var client in currentClientList)
        //                {
        //                    if (client.Item2 == tuple.Item2)
        //                    {
        //                        exist = true;
        //                        break;
        //                    }
        //                }
        //                if (exist == false)
        //                {
        //                    //the current client is not inside the tabs, so we will add it
        //                    currentClientList.Add(tuple);
        //                    clientsNumber++;
                            
        //                    //add here the new tab for the client
        //                    TabItem newTabItem = new TabItem
        //                    {
        //                        Header = tuple.Item1.ToString()
        //                    };
        //                    ListBox listbox = new ListBox();
        //                    listbox.Name = tuple.Item1.ToString() + "_listbox";
        //                    listbox.Height = 600;

        //                    newTabItem.Content = listbox;
        //                    listbox.SelectionChanged += MainListBox_SelectionChanged;
        //                    listbox.ItemTemplate= ListItemTemplate();

        //                    CmpTabs.Items.Add(newTabItem);

        //                }
        //            }
        //        }
        //    }
        //    if (currentClientList != null)
        //    {
        //        foreach (var client in currentClientList.ToList())
        //        {
        //            var name = client.Item1;
        //            var ip = client.Item2;
        //            var id = client.Item3;

        //            ListBox clientListBox = new ListBox();

        //            try //try to update the process list for each client, if falied, client has disconnected
        //            {
        //                foreach (var item in ComputerTabs.Items)
        //                {
        //                    var tab = item as TabItem;
        //                    if (tab != null)
        //                    {
        //                        if (tab.Header != null && tab.Header.ToString() == name)
        //                        {
        //                            clientListBox = tab.Content as ListBox;
        //                        }
        //                    }
        //                }

        //                var newProcesses = await GetProcesscesFromServer(ip);
        //                Dispatcher.Invoke(() => UpdateProcessList(newProcesses, clientListBox));
        //            }
        //            catch (Exception e)
        //            {
        //                currentClientList.Remove(client);
        //                RemoveCurrentTab(name);
        //                clientsNumber--;
        //                await MongoDBfunctions.RemoveDisconnectedClientAsync(technicianUsername, ip);
        //            }
        //        }
        //    }
        //}
        //private ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();
        //private void UpdateProcessList(List<(int, string, string)> newProcesses, ListBox clientListBox)
        //{
        //    var ProcessesListItem = new List<ListItem>();
        //    // Convert newProcesses to ListItem objects
        //    foreach (var process in newProcesses)
        //    {
        //        ListItem current = new ListItem { Name = process.Item2, ID = process.Item1, CPU = process.Item3 };
        //        ProcessesListItem.Add(current);
        //    }

        //    ListBox currentListBox = clientListBox;

        //    // Remove duplicates from the ListBox
        //    foreach (var newItem in ProcessesListItem)
        //    {
        //        bool alreadyExists = false;
        //        foreach (var listBoxItem in currentListBox.Items)
        //        {
        //            var existingItem = listBoxItem as ListItem;
        //            if (existingItem != null && existingItem.ID == newItem.ID && existingItem.Name == newItem.Name)
        //            {
        //                alreadyExists = true;
        //                break;
        //            }
        //        }
        //        if (!alreadyExists)
        //        {
        //            // Add new item to ListBox only if it contains the search term
        //            if (search == "" || newItem.Name.Contains(search))
        //            {
        //                items.Add(newItem);
        //                currentListBox.Items.Add(newItem);
        //            }
        //        }
        //    }

        //    // Remove items that do not exist in newProcesses
        //    for (int i = currentListBox.Items.Count - 1; i >= 0; i--)
        //    {
        //        var item = currentListBox.Items[i] as ListItem;
        //        if (item != null && !ProcessesListItem.Any(p => p.ID == item.ID && p.Name == item.Name))
        //        {
        //            currentListBox.Items.RemoveAt(i);
        //        }
        //    }
        //    // Remove items that do not contain the search term
        //    if (search != "")
        //    {
        //        for (int i = currentListBox.Items.Count - 1; i >= 0; i--)
        //        {
        //            var item = currentListBox.Items[i] as ListItem;
        //            if (item != null && !item.Name.Contains(search))
        //            {
        //                currentListBox.Items.RemoveAt(i);
        //            }
        //        }
        //    }

        //}

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
                            //FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                            //textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
                            //hdt.VisualTree = textBlockFactory;
                            //treeView.ItemTemplate = hdt;
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
        }
        private void UpdateProcessListNew(List<(int, string, string)> newProcesses, TreeView processTreeView)
        {
            // Dictionary to store parent process items by name
            var parentDictionary = new Dictionary<string, ProcessItem>();

            foreach (var process in newProcesses)
            {
                var newItem = new ProcessItem { ID = process.Item1, Name = process.Item2, CPU = process.Item3 };

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
            timer.Interval = TimeSpan.FromMilliseconds(1000); // Set the interval to 1000 milliseconds = 1 second           
            
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
        private HierarchicalDataTemplate CreateTreeView()
        {
            // Create the TreeView
            TreeView processTreeView = new TreeView();

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

            // Add TextBlocks to StackPanel
            stackPanelFactory.AppendChild(nameTextBlock);
            stackPanelFactory.AppendChild(idTextBlock);
            stackPanelFactory.AppendChild(cpuTextBlock);

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

            innerStackPanelFactory.AppendChild(innerNameTextBlock);
            innerStackPanelFactory.AppendChild(innerIdTextBlock);
            innerStackPanelFactory.AppendChild(innerCpuTextBlock);

            innerDataTemplate.VisualTree = innerStackPanelFactory;

            // Set the ItemTemplate of the inner HierarchicalDataTemplate
            hierarchicalDataTemplate.ItemTemplate = innerDataTemplate;
            
            return hierarchicalDataTemplate;
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

        public override string ToString()
        {
            var childrenToString = string.Join("\n", Children.Select(c => c.ToString()));
            return $"ID: {ID}, Name: {Name}, CPU: {CPU} {childrenToString}";
        }
    }
}
