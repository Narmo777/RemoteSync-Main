using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Console.Out.WriteLineAsync("i am server \nplease enter your name");
            var name = Console.ReadLine();
            await Console.Out.WriteLineAsync("please enter technician name");
            var technician = Console.ReadLine();

            await Console.Out.WriteLineAsync("thank you for connecting!");
            var s = new Server(name, technician); // build a server 

            await s.Start();
        }
    }
}
