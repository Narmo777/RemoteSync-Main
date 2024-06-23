using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using Protocol;
using MongoDB.Bson;
using MongoDB.Driver;
using ConsoleApp2.MongoDB;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Net.NetworkInformation;

namespace Server
{

    public class Server
    {
        private TcpListener listener;
        public static string GetLocalIPAddress() => Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
        public string IP = GetLocalIPAddress();
        public const int PORT = 300;
        public Server(string name, string technician)
        {
            string IP = GetLocalIPAddress();

            MongoDBfunctionsServer.InsertNewClient(technician, name, IP);
        }

        public async Task Start()
        {
            var ip = IPAddress.Parse(IP);
            this.listener = new TcpListener(ip, PORT);

            listener.Start();
            var clientTasks = new List<Task>();

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                var task = ProcessRequest(client).ContinueWith(t =>
                {
                    if (t.Exception != null)
                        Console.WriteLine(t.Exception);
                }, TaskContinuationOptions.OnlyOnFaulted);

                clientTasks.Add(task);
            }

           // await Task.WhenAll(clientTasks);
        }
        private async Task ProcessRequest(TcpClient client)
        {
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
                    Packet responcePacket = new Packet(RequestType.Err, "Bad Request Try Again");
                    try
                    {
                        var packet = await Packet.FromNetworkStream(stream);
                        await Console.Out.WriteLineAsync($"got {packet.GetContentAsString()}");
                        responcePacket = HandleRequest(ref packet);

                    }
                    catch (Exception)
                    {

                    }
                    finally
                    {
                        await stream.WriteAsync((byte[])responcePacket, 0, responcePacket.DataSize + Packet.HeaderSize);
                        await Console.Out.WriteLineAsync($"wrote {responcePacket.GetContentAsString()}");

                        stream.Close();
                    }
                }


            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync($"Error with client - {e}");
            }
        }

        //all buttons 
        private Packet HandleRscRequest(ref Packet p)
        {
            var id = GetProcessIdFrom(ref p);
            //var process = Process.GetProcessById(id);

            //var processRsc = $"Mem:{process.WorkingSet64}";

            var processRsc = "";


            // Create a PerformanceCounter for CPU usage
            using (PerformanceCounter cpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName))
            {
                cpuCounter.NextValue(); // Call this once to get initial value

                // Get CPU usage for the specific process
                float cpuUsage = cpuCounter.NextValue();

                Console.WriteLine($"CPU Usage for Process ID {id}: {cpuUsage}%");
            }


            return new Packet(RequestType.Ok, processRsc);
        }
        private Packet HandleKillRequest(ref Packet p)
        {

            var id = int.Parse(p.GetContentAsString());
            int processIdToKill = id;
            string msg;
            
            try
            {
                var process = Process.GetProcessById(processIdToKill);
                var childProcesses = Process.GetProcessesByName(process.ProcessName);

                if(childProcesses.Length > 0)
                {
                    // Kill child processes
                    foreach (var child in childProcesses)
                    {
                        child.Kill();
                    }
                }
                else
                {
                    // Kill the main process
                    process.Kill();
                }

                msg = $"Process with ID {processIdToKill} has been killed.";
            }
            catch (ArgumentException)
            {
                msg = $"Process with ID {processIdToKill} not found.";
            }
            catch (Exception ex)
            {
                msg = $"An error occurred: {ex.Message}";
            }


            return new Packet(RequestType.Ok, msg);
        }
        private  Packet HandleGetRequest()
        {
            var processes = Process.GetProcesses();
            var parentProcesses = new List<Process>();
            var childProcesses = new List<Process>();

            // Classify processes into parent and child lists
            foreach (Process process in processes)
            {
                int parentPid = GetParentProcessId(process.Id);
                if (parentPid == 0 || parentPid == process.Id)
                {
                    parentProcesses.Add(process);
                }
                else
                {
                    childProcesses.Add(process);
                }
            }

            string totalDataBuilder = "";

            // Process parent processes
            foreach (Process process in parentProcesses)
            {
                string data = GetProcessData(process);
                if (totalDataBuilder.Length > 0)
                {
                    totalDataBuilder += '|';
                }
                totalDataBuilder += data;
            }

            // Process child processes
            foreach (Process process in childProcesses)
            {
                string data = GetProcessData(process);
                if (totalDataBuilder.Length > 0)
                {
                    totalDataBuilder += '|';
                }
                totalDataBuilder += data;
            }

            string totalData = totalDataBuilder.ToString();
            return new Packet(RequestType.Ok, totalData);
        }

        public Packet HandleRequest(ref Packet p)
        {
            switch (p.RequestType)
            {
                case RequestType.Get:
                    return HandleGetRequest();
                case RequestType.Kill:
                    return HandleKillRequest(ref p);
                case RequestType.Rsc:
                    return HandleRscRequest(ref p);
                default:
                    return new Packet(RequestType.Err, "Cant Process Request");
            }
        }
        private int GetProcessIdFrom(ref Packet p)
        {
            return BitConverter.ToInt32(p.Data, 0);
        }

        //helpers for get request
        private string GetProcessData1(Process process)
        {
            string data = "";
            try
            {
                // Capture the initial CPU usage time
                TimeSpan startCpuUsage = process.TotalProcessorTime;
                DateTime startTime = DateTime.Now;

                // Wait for a short period to capture CPU usage over time
                Thread.Sleep(50);

                // Capture the end CPU usage time
                TimeSpan endCpuUsage = process.TotalProcessorTime;
                DateTime endTime = DateTime.Now;

                // Calculate the CPU usage over the interval
                double cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                double intervalMs = (endTime - startTime).TotalMilliseconds;
                double cpuUsagePercentage = (cpuUsedMs / (Environment.ProcessorCount * intervalMs)) * 100;

                //network
                //float totalBandwidth = 30*1000*1000/8;
                //string NetworkUsagePercentage = GetNetworkUsagePercentage(process.Id, totalBandwidth, 10);

                long totalNetworkBandwidth = 3750000;

                // Initialize performance counters for the process
                PerformanceCounter bytesReceivedCounter = new PerformanceCounter("Process", "IO Read Bytes/sec", process.ProcessName);
                PerformanceCounter bytesSentCounter = new PerformanceCounter("Process", "IO Write Bytes/sec", process.ProcessName);

                // Get the current network usage
                float bytesReceivedPerSec = bytesReceivedCounter.NextValue();
                float bytesSentPerSec = bytesSentCounter.NextValue();

                // Calculate total network usage for the process
                float totalBytesPerSec = bytesReceivedPerSec + bytesSentPerSec;

                // Calculate network usage percentage
                float NetworkUsagePercentage = (totalBytesPerSec / totalNetworkBandwidth) * 100;

                //data = $"{process.Id}#{process.ProcessName}#{cpuUsagePercentage.ToString("F2")}%";
                data = $"{process.Id}#{process.ProcessName}#{cpuUsagePercentage.ToString("F2")}%#{NetworkUsagePercentage}%";
            }
            catch (Exception)
            {
                data = $"{process.Id}#{process.ProcessName}#999%#999%";
            }
            return data;

        }
        private string GetProcessData(Process process)
        {
            string data = "";
            try
            {
                // Get initial CPU time
                TimeSpan startCpuTime = process.TotalProcessorTime;
                DateTime startTime = DateTime.UtcNow;

                // Get initial network usage
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                long startBytesReceived = networkInterfaces.Sum(ni => ni.GetIPv4Statistics().BytesReceived);
                long startBytesSent = networkInterfaces.Sum(ni => ni.GetIPv4Statistics().BytesSent);

                // Wait for a second to calculate CPU usage over time
                Thread.Sleep(500);

                // Get final CPU time
                TimeSpan endCpuTime = process.TotalProcessorTime;
                DateTime endTime = DateTime.UtcNow;

                // Calculate CPU usage
                double cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
                double totalMsPassed = (endTime - startTime).TotalMilliseconds;
                double cpuUsagePercentage = (cpuUsedMs / totalMsPassed) / Environment.ProcessorCount * 100;

                // Get final network usage
                long endBytesReceived = networkInterfaces.Sum(ni => ni.GetIPv4Statistics().BytesReceived);
                long endBytesSent = networkInterfaces.Sum(ni => ni.GetIPv4Statistics().BytesSent);
                long networkUsagePercentage = (endBytesReceived + endBytesSent) - (startBytesReceived + startBytesSent);

                data = $"{process.Id}#{process.ProcessName}#{cpuUsagePercentage.ToString("F2")}%#{networkUsagePercentage}%";
            }
            catch (Exception)
            {
                data = $"{process.Id}#{process.ProcessName}#999%#999%";
            }
            return data;
        }
        public static string GetNetworkUsagePercentage(int processId, float totalBandwidth, int monitoringInterval)
        {
            using (PerformanceCounter pcSent = new PerformanceCounter("Process", "IO Write Bytes/sec", processId.ToString()))
            using (PerformanceCounter pcReceived = new PerformanceCounter("Process", "IO Read Bytes/sec", processId.ToString()))
            {
                float bytesSentStart = pcSent.NextValue();
                float bytesReceivedStart = pcReceived.NextValue();

                Thread.Sleep(monitoringInterval);

                float bytesSentEnd = pcSent.NextValue();
                float bytesReceivedEnd = pcReceived.NextValue();

                float bytesSent = bytesSentEnd - bytesSentStart;
                float bytesReceived = bytesReceivedEnd - bytesReceivedStart;
                float totalBytes = bytesSent + bytesReceived;

                float usagePercentage = (totalBytes / (totalBandwidth * (monitoringInterval / 1000.0f))) * 100;

                return usagePercentage.ToString();
            }
        }                
        
        public int GetParentProcessId(int processId)
        {
            int parentPid = 0;
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    // Allocate a handle for the process query
                    IntPtr hProcess = OpenProcess(ProcessAccessFlags.QueryInformation, false, processId);
                    if (hProcess != IntPtr.Zero)
                    {
                        try
                        {
                            PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                            int returnLength = 0;
                            int status = NtQueryInformationProcess(hProcess, 0, ref pbi, Marshal.SizeOf(pbi), ref returnLength);
                            if (status == 0)
                            {
                                parentPid = pbi.InheritedFromUniqueProcessId.ToInt32();
                            }
                        }
                        finally
                        {
                            CloseHandle(hProcess);
                        }
                    }
                }
            }
            catch
            {
                // If unable to get the parent process, return 0
                parentPid = 0;
            }
            return parentPid;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }
        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, ref int returnLength);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            QueryInformation = 0x00000400
        }

        //helpers for kill request
        private List<Process> GetChildProcesses(int parentPid)
        {
            var childProcesses = new List<Process>();
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                if (GetParentProcessId(process.Id) == parentPid)
                {
                    childProcesses.Add(process);
                }                
            }
            return childProcesses;
        }
    }
}
