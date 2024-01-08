using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using Protocol;
namespace Server
{

    public class Server
    {
        private TcpListener listener;
        public const string IP = "127.0.0.1";
        private int port;
        public Server(int port)
        {
            this.port = port;
        }

        public async Task Start()
        {
            var ip = IPAddress.Parse(IP);
            this.listener = new TcpListener(ip, port);

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
        private int GetProcessIdFrom(ref Packet p)
        {
            return BitConverter.ToInt32(p.Data, 0);
        }
        
        private Packet HandleRscRequest(ref Packet p)
        {
            var id=GetProcessIdFrom(ref p);
            var process=Process.GetProcessById(id);

            var processRsc = $"Mem:{process.WorkingSet64}";

            return new Packet(RequestType.Ok, processRsc);
        }
        private Packet HandleKillRequest(ref Packet p)
        {
            var id =GetProcessIdFrom(ref p);

            Process.GetProcessById(id).Kill();

            return new Packet(RequestType.Ok,"");
        }
        private Packet HandleGetRequest()
        {
            var process = Process.GetProcesses();

            var data = process.Select(p => $"{p.Id}|{p.ProcessName}").Aggregate((s1, s2) => $"{s1}#{s2}");


            return new Packet(RequestType.Ok,data);
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

    }
}
