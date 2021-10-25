#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Lidgren.Network;
using System.Xml.Serialization;

namespace ReDream.Shared
{
    public class ReGame
    {
        protected List<GameObject> gameObjects = new();
        protected NetServer? server;
        public string Action = "";
        
        public void Host(int port)
        {
            var config = new NetPeerConfiguration("ReDream")
            {
                Port = port
            };
            server = new NetServer(config);
            server.Start();
        }

        public void ReceiveMessages()
        {
            if (server == null) return;
            NetIncomingMessage message;
            while ((message = server.ReadMessage()) != null)
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        // handle custom messages
                        switch (message.ReadString())
                        {
                            case "move":
                                var action = message.ReadString();
                                this.Action = action;
                                break;
                            case "msg":
                                var serObject = message.ReadString();
                                var serializer = new XmlSerializer(typeof(GameObject));
                                var stream = new StringReader(serObject);
                                var obj = (GameObject?)serializer.Deserialize(stream);
                                if (obj == null) return;
                                obj.Messages.Add(message.ReadString());
                                break;
                            case "status":
                                var ser= message.ReadString();
                                var seri = new XmlSerializer(typeof(GameObject));
                                var stream1 = new StringReader(ser);
                                var obj1 = (GameObject?)seri.Deserialize(stream1);
                                var dataSer = message.ReadString();
                                var dataSerializer = new XmlSerializer(typeof(Dictionary<string, object>));

                                if (obj1 == null) return;
                                var dataStream = new StringReader(dataSer);
                                var dataDes = (Dictionary<string, object>?)dataSerializer.Deserialize(dataStream);
                                if (dataDes == null) return;
                                obj1.Data = dataDes;
                                break;
                            case "download":
                                try
                                {
                                    var texName = message.ReadString();
                                    var textureData = File.ReadAllBytes(Path.Combine(ScriptWorker.ClientPath, texName));
                                    SendBinaryFile(textureData, texName, message.SenderConnection);
                                }
                                catch { }
                                break;
                            case "code":
                                foreach (var file in Directory.GetFiles(ScriptWorker.ClientPath))
                                {
                                    var splitted = Path.GetFullPath(file).Split("/");
                                    if (splitted.Length < 2)
                                    {
                                        splitted = Path.GetFullPath(file).Split("\\");
                                    }
                                    var filename = splitted[splitted.Length - 1];
                                    if (filename.Split(".").Last() != "cs") continue;
                                    SendFile(File.ReadAllText(file), filename, message.SenderConnection);
                                }
                                var msg = server.CreateMessage();
                                msg.Write("reload");
                                server.SendMessage(msg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                break;
                        }
                        
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        // handle connection status messages
                        switch (message.SenderConnection.Status)
                        {
                            /* .. */
                        }
                        break;

                    case NetIncomingMessageType.DebugMessage:
                        // handle debug messages
                        // (only received when compiled in DEBUG mode)
                        Console.WriteLine(message.ReadString());
                        break;

                    /* .. */
                    default:
                        Console.WriteLine("unhandled message with type: "
                            + message.MessageType);
                        break;
                }
            }
        }

        protected void SendBinaryFile(byte[] data, string name, NetConnection sender)
        {
            if (server == null || sender.Status != NetConnectionStatus.Connected) return;
            var msg = server.CreateMessage();
            msg.Write("texture");
            msg.Write(name);
            msg.Write(data.Length);
            msg.Write(data);
            server.SendMessage(msg, sender, NetDeliveryMethod.ReliableOrdered);
        }
        
        protected void SendFile(string data, string name, NetConnection sender)
        {
            if (server == null) return;
            var msg = server.CreateMessage();
            msg.Write("upload");
            msg.Write(name);
            msg.Write(data);
            server.SendMessage(msg, sender, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMessage(GameObject obj, string msg)
        {
            if (server == null) return;
            var message = server.CreateMessage();
            message.Write("msg");
            var formatter = new XmlSerializer(obj.GetType());
            var ms = new StringWriter();
            formatter.Serialize(ms, obj);
            message.Write(ms.ToString());
        }

        public void SendStatus(GameObject obj)
        {
            
        }

        public void AddNew(string tex, int x, int y, string name)
        {
            AddNew(new GameObject());
        }

        public void AddNew(GameObject obj)
        {
            gameObjects.Add(obj);
            ReTools.Log("GameObject added");
        }

        public GameObject[] GetObjectsByName(string name)
        {
            var found = new List<GameObject>();
            foreach (var obj in gameObjects)
            {
                if (obj.Name == name)
                {
                    found.Add(obj);
                }
            }
            return found.ToArray();
        }

        public void DrawObject(GameObject obj)
        {
            if (server == null) return;
            var message = server.CreateMessage();
            message.Write("redraw");
            message.Write(obj.Texture);
            message.Write(obj.X);
            message.Write(obj.Y);

            BroadcastMsg(message);
        }

        protected void BroadcastMsg(NetOutgoingMessage msg)
        {
            if (server == null) return;
            foreach (var conneciton in server.Connections)
            {
                server.SendMessage(msg, conneciton, NetDeliveryMethod.ReliableOrdered);
            }
        }
    }

    public class GameObject
    {
        public string Texture = "error.png";
        public int X = 0;
        public int Y = 0;
        public string Name = "error";
        public Dictionary<string, object> Data = new();
        public List<string> Messages = new();

        protected int LastX = 0;
        protected int LastY = 0;

        public GameObject()
        {
        }

        public override string ToString()
        {
            return X.ToString() + ":" + Y.ToString() + "  "  + Name;
        }

        public object GetData(string name)
        {
            return Data[name];
        }

        public void SetData(string name, object obj)
        {
            Data[name] = obj;
        }

        public virtual void Update(ReGame game)
        {
            if (LastX != X || LastY != Y)
            {
                game.DrawObject(this);
                LastX = X;
                LastY = Y;
            }
        }

        public virtual void Start(ReGame game)
        {

        }
    }
}
