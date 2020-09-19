using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VrServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program(args);
        }

        TcpListener listener;
        public List<Client> clients { get; set; } = new List<Client>();

        public Program(string[] args)
        {
            listener = new TcpListener(IPAddress.Any, 6666);
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(OnConnect), null);
            Console.WriteLine("Listening on port 6666");
            CreateWebHostBuilder(args).Build().Run();
        }

        private void OnConnect(IAsyncResult ar)
        {
            TcpClient client = listener.EndAcceptTcpClient(ar);
            clients.Add(new Client(client, this));
            listener.BeginAcceptTcpClient(new AsyncCallback(OnConnect), null);
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
