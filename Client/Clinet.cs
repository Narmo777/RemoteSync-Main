using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Protocol;
namespace Client
{
    public class Clinet
    {
        
        public Clinet() { }
        public static async Task Connect(string ip, int port)
        {
            var clinet = new TcpClient(ip, port); //creates a new tcp client
            var stream = clinet.GetStream();

            var baseMsg = new Packet(RequestType.Get, "");

            await stream.WriteAsync((byte[])baseMsg,0,baseMsg.DataSize+Packet.HeaderSize);
            await stream.FlushAsync();

            var resp = await Packet.FromNetworkStream(stream);

            foreach (var item in resp.GetContentAsString().Split('#').Select(x => x.Split('|')).Select(x => (int.Parse(x[0]), x[1])))
            {
                await Console.Out.WriteLineAsync(item.ToString());
            };
            stream.Close();
        }
    }
}
