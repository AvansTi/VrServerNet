using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VrServer
{
    public class Session
    {
        public string id { get; } = Guid.NewGuid().ToString();
        public DateTime beginTime { get; } = DateTime.Now;
        public DateTime lastPing { get; set; } = DateTime.Now;
        public List<Fps> fps { get; set; } = new List<Fps>();
        public HashSet<string> features { get; set; } = new HashSet<string>();
        public ClientInfo clientinfo { get; set; } = new ClientInfo();

        [JsonIgnore]
        public string tunnelKey { get; set; }
        [JsonIgnore]
        public Client client { get; private set; }
        [JsonIgnore]
        public List<Tunnel> tunnels = new List<Tunnel>();


        public Session(Client client)
        {
            this.client = client;
        }

    }

    public class Fps
    {
        public double time;
        public float fps;
    }
    public class ClientInfo
    {
        public string host;
        public string user;
        public string file;
        public string renderer;
    }
}
