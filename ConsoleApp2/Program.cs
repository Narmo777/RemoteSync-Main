using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Console.Out.WriteLineAsync("i am server ");

            var s = new Server(300); // set server port to 300 

            await s.Start();
        }
    }
}
