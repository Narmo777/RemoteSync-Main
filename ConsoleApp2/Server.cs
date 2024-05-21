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

namespace Server
{

    public class Server
    {
        private TcpListener listener;
        public static string GetLocalIPAddress() => Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
        //public const string IP = "127.0.0.1";
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
                    finally {
                        await stream.WriteAsync((byte[])responcePacket,0, responcePacket.DataSize + Packet.HeaderSize);
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
            
            //var id = GetProcessIdFrom(ref p);
            //Process process = Process.GetProcessById(id);
            //process.Kill();
            //Process.GetProcessById(id).Kill();

            var id = int.Parse(p.GetContentAsString());
            int processIdToKill = id;
            string msg;
            
            try
            {
                // Get the process by its ID
                Process processToKill = Process.GetProcessById(processIdToKill);

                // Kill the process
                processToKill.Kill();
                
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
        private Packet HandleGetRequest()
        {
            var process = Process.GetProcesses();

            var data = process.Select(p => $"{p.Id}|{p.ProcessName}").Aggregate((s1, s2) => $"{s1}#{s2}");


            return new Packet(RequestType.Ok,data);
        }
        private Packet HandleGetRequestNew()
        {
            var processes = Process.GetProcesses();
            StringBuilder totalDataBuilder = new StringBuilder();

            foreach (Process process in processes)
            {
                string data = "";
                if(totalDataBuilder.Length > 0)
                {
                    totalDataBuilder.Append("|");
                }

                try
                {
                    // Capture the initial CPU usage time
                    TimeSpan startCpuUsage = process.TotalProcessorTime;
                    DateTime startTime = DateTime.Now;

                    // Wait for a short period to capture CPU usage over time
                    Thread.Sleep(100);

                    // Capture the end CPU usage time
                    TimeSpan endCpuUsage = process.TotalProcessorTime;
                    DateTime endTime = DateTime.Now;

                    // Calculate the CPU usage over the interval
                    double cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                    double intervalMs = (endTime - startTime).TotalMilliseconds;
                    double cpuUsagePercentage = (cpuUsedMs / (Environment.ProcessorCount * intervalMs)) * 100;

                    data = $"{process.Id}#{process.ProcessName}#{cpuUsagePercentage}";
                }
                catch (Exception ex)
                {
                    data = $"{process.Id}#{process.ProcessName}#{000}";
                }
                finally
                {
                    totalDataBuilder.Append(data);
                }
            }

            string TotalData = totalDataBuilder.ToString();
            return new Packet(RequestType.Ok, TotalData);
        }
        
        public Packet HandleRequest(ref Packet p)
        {
            switch (p.RequestType)
            {
                case RequestType.Get:
                    return HandleGetRequestNew();
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
    }
}
