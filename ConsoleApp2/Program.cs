using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using ConsoleApp2.MongoDB;
using System.Net.NetworkInformation;
namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Checking for internet connection...");

            if (IsInternetAvailable())
            {
                Console.WriteLine("Internet connection is available.");

                await Console.Out.WriteLineAsync("\n\n\nplease enter your name");
                var name = Console.ReadLine();
                await Console.Out.WriteLineAsync("please enter technician name");
                var technician = Console.ReadLine();
                while (!MongoDBfunctionsServer.IsTechnicianExists(technician))
                {
                    //technician doesnt exists
                    await Console.Out.WriteLineAsync("technician name doesnt exists");
                    await Console.Out.WriteLineAsync("please enter technician name");
                    technician = Console.ReadLine();
                }


                await Console.Out.WriteLineAsync("thank you for connecting!");
                var s = new Server(name, technician); // build a server 

                await s.Start();
            }
            else
            {
                Console.WriteLine("No internet connection.");
            }
        }
        public static bool IsInternetAvailable()
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send("8.8.8.8", 3000); // Google's public DNS server
                    return reply.Status == IPStatus.Success;
                }
            }
            catch (PingException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
        }
    }
}
