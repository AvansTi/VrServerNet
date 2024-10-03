using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

namespace VrServer
{
    public class Client
    {
        private TcpClient client;
        private Program program;
        private NetworkStream stream;
        private byte[] buffer = new byte[1024];
        private byte[] totalBuffer = new byte[0];
        private Session session = null;

        private List<Tunnel> tunnels = new List<Tunnel>();

        public Client(TcpClient client, Program program)
        {
            this.client = client;
            this.program = program;
            Console.WriteLine($"Got a connection from {client.Client.RemoteEndPoint.ToString()}");

            this.stream = client.GetStream();
            try
            {
            	stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnRead), null);
			} catch(Exception)
			{
				OnDisconnect();
			}
        }

        private void OnRead(IAsyncResult ar)
        {
            try
            {
                int rc = stream.EndRead(ar);
                totalBuffer = concat(totalBuffer, buffer, rc);
                if(rc <= 0)
                {
                    OnDisconnect();
                    return;
                }
            }
            catch (Exception)
            {
                OnDisconnect();
                return;
            }
            while (totalBuffer.Length >= 4)
            {
                int packetSize = BitConverter.ToInt32(totalBuffer, 0);
                if(packetSize < 0)
                {
                	OnDisconnect();
                	return;
                }
                if (totalBuffer.Length >= packetSize + 4)
                {
                    string data = Encoding.UTF8.GetString(totalBuffer, 4, packetSize);
					try {
	                    dynamic json = JsonConvert.DeserializeObject(data);
   	                 	Console.WriteLine("Got a packet " + json.id);

                    	string id = json.id;
                    	if (id == "session/start")
                       	 	SessionStart(json.data);
                    	else if (id == "session/report")
                        	SessionReport(json.data);
                    else if (id == "session/enable")
                        SessionEnable(json.data, json);
                    else if (id == "session/list")
                        SessionList(json.data);
                    else if (id == "tunnel/create")
                        TunnelCreate(json.data);
                    else if (id == "tunnel/send")
                        TunnelSend(json.data);
                        }catch(Exception ex)
                        {
                        	Console.WriteLine(ex);
    	                    OnDisconnect();
	                        return;
                        }
                    totalBuffer = totalBuffer.SubArray(4 + packetSize, totalBuffer.Length - packetSize - 4);
                }
                else
                    break;
            }
            try
            {
                stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
            }
            catch (Exception) { OnDisconnect(); }
        }

        public void Send(string id, dynamic data)
        {
            var command = new { id = id, data = data };
            byte[] d = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command));
            try
            {
                stream.Write(BitConverter.GetBytes(d.Length), 0, 4);
                stream.Write(d, 0, d.Length);
            }catch(Exception)
            {
                OnDisconnect();
                return;
            }
        }


        private void TunnelSend(dynamic json)
        {
            foreach(Tunnel tunnel in tunnels)
            {
                if(tunnel.id == (string)json.dest)
                {
                    tunnel.session.client.Send("tunnel/send", new { id = tunnel.id, data = json.data });
                    return;
                }
            }
            if(session == null)
            {
            	Console.WriteLine("Could not find proper tunnel?");
            }
            if(session != null && session.tunnels != null)
                foreach (Tunnel tunnel in session.tunnels)
                    if (tunnel.session == this.session)
                        tunnel.client.Send("tunnel/send", new { id = tunnel.id, data = json.data });

        }
        //{
        //  "session": "e6b02d9d-594c-40bc-b1e7-57a95ae8ff65",
        //  "key": "muffins"
        //}
        private void TunnelCreate(dynamic json)
        {
            if(json.session == null)
            {
                Send("tunnel/create", new { status = "error" });
                return;
            }

            var session = program.clients.FirstOrDefault(c => c.session != null && c.session.id == (string)json.session)?.session;
            if(session == null)
            {
                Send("tunnel/create", new { status = "error" });
                return;
            }
            if(!session.features.Contains("tunnel"))
            {
                Send("tunnel/create", new { status = "error", msg = "does not support tunnel" });
                return;
            }
            if (session.tunnelKey != "" && (json.key == null || session.tunnelKey != (string)json.key))
            {
                Send("tunnel/create", new { status = "error", msg = "wrong tunnel key" });
                return;
            }

            Tunnel tunnel = new Tunnel();
            tunnel.session = session;
            tunnel.client = this;

            this.tunnels.Add(tunnel);
            session.tunnels.Add(tunnel);
            Send("tunnel/create", new { status = "ok", id = tunnel.id });

            session.client.Send("tunnel/connect", new { id = tunnel.id });

        }

        private void SessionList(dynamic json)
        {
            List<Session> sessions = program.clients.Where(c => c?.session != null).Select(c => c.session).ToList();
            Send("session/list", sessions);
        }

        private void SessionEnable(dynamic json, dynamic allPacket)
        {
            if (session == null)
                return;
            foreach(string feature in json)
                session.features.Add(feature);
			if(allPacket.key != null)
			{
				Console.WriteLine($"Using key {allPacket.key.ToObject<string>()}");
				session.tunnelKey = allPacket.key.ToObject<string>();
			}
        }

        private void SessionReport(dynamic json)
        {
            if (session == null)
                return;
            session.fps.Add(new Fps()
            {
                time = (DateTime.Now - session.beginTime).TotalSeconds,
                fps = json.fps
            });

        }

        private void SessionStart(dynamic json)
        {
            if (session == null)
            {
            	Console.WriteLine(json);
                session = new Session(this);
                session.tunnelKey = "";
                session.tunnelKey = "";
                session.clientinfo.host = json.host;
                session.clientinfo.file = json.file;
                session.clientinfo.user = json.user;
                session.clientinfo.renderer = json.renderer;
                Send("session/start", "ok");
            }
            
        }

        private void OnDisconnect()
        {
            if(session != null && session.clientinfo != null && session.clientinfo.user != null)
            	Console.Write(session.clientinfo + " ");
            Console.WriteLine($"Disconnected");
            try { stream.Close(); client.Close(); } catch (Exception) { };
            if(session != null)
            {

            }
            foreach (var tunnel in tunnels)
                tunnel.session.tunnels.Remove(tunnel);
            program.clients.Remove(this);
        }


        private static byte[] concat(byte[] b1, byte[] b2, int count)
        {
            byte[] r = new byte[b1.Length + count];
            System.Buffer.BlockCopy(b1, 0, r, 0, b1.Length);
            System.Buffer.BlockCopy(b2, 0, r, b1.Length, count);
            return r;
        }

    }
}