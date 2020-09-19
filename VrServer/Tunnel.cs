using System;

namespace VrServer
{
    public class Tunnel
    {
        public string id = Guid.NewGuid().ToString();
        public Session session;
        public Client client;
    }
}