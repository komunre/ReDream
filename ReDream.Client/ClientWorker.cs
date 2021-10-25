using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReDream.Shared;
using Raylib_cs;
using Lidgren.Network;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Runtime.Loader;
using System.Numerics;

namespace ReDream.Client
{
    public class ClientWorker 
    {
        private ScriptWorker worker = new();
        private List<string> ReceivedFiles = new();
        private Dictionary<string, Texture2D> _textures = new();
        private List<string> _requested = new();
        private Dictionary<string, Vector2> _drawingTextures = new();

        public ClientWorker()
        {
            worker.AddType(typeof(ClientWorker).GetTypeInfo().Assembly.Location);
            worker.Initialize("client_cs/", "client_cs/", true);
            worker.eng.SetValue("getInput", new Func<int>(GetInput));
            worker.eng.SetValue("sendInput", new Action<string>(SendInput));
            if (!Directory.Exists(ScriptWorker.ClientPath))
            {
                Directory.CreateDirectory(ScriptWorker.ClientPath);
            }
            else
            {
                var files = Directory.GetFiles(ScriptWorker.ClientPath);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            Raylib.InitWindow(800, 600, "ReDream Client");
            Raylib.SetTargetFPS(60);
        }

        public void Update()
        {
            worker.Update();
            if (ReceivedFiles.Count == 0)
            {
                Thread.Sleep(2000);
                RequestCode();
            }
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(255, 255, 255, 255));

            foreach (var tex in _drawingTextures)
            {
                Raylib.DrawTexture(_textures[tex.Key], (int)tex.Value.X, (int)tex.Value.Y, Color.WHITE);
            }
            Raylib.EndDrawing();
        }

        protected static NetClient client;

        public void Connect(string url, int port)
        {
            var config = new NetPeerConfiguration("ReDream");
            client = new NetClient(config);
            client.Start();
            while (client.Connections.Count == 0)
            {
                client.Connect(url, port);
                ReTools.Log("Reconnecting soon...");
                Thread.Sleep(200);
            }
            RequestCode();
        }

        public static void SendInput(string action)
        {
            var msg = client.CreateMessage();
            msg.Write("move");
            msg.Write(action);
            client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }

        protected void DownloadTexture(string tex)
        {
            var msg = client.CreateMessage();
            msg.Write("download");
            msg.Write(tex);
            client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }

        protected void RequestCode()
        {
            var msg = client.CreateMessage();
            msg.Write("code");
            client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }

        public void ReceiveMessages()
        {
            if (client == null) return;
            NetIncomingMessage message;
            while ((message = client.ReadMessage()) != null)
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        // handle custom messages
                        switch (message.ReadString())
                        {
                            case "redraw":
                                var tex = message.ReadString();
                                var x = message.ReadInt32();
                                var y = message.ReadInt32();

                                if (ReceivedFiles.Contains(tex))
                                {
                                    if (!worker.Compiled) return;
                                    if (!_textures.ContainsKey(tex)) return;
                                    if (_drawingTextures.ContainsKey(tex))
                                    {
                                        _drawingTextures[tex] = new Vector2(x, y);
                                    }
                                    else
                                    {
                                        _drawingTextures.Add(tex, new Vector2(x, y));
                                    }
                                }
                                else if (!_requested.Contains(tex))
                                {
                                    DownloadTexture(tex);
                                }
                                break;
                            case "texture":
                                var downTex = message.ReadString();
                                var len = message.ReadInt32();
                                var bindata = message.ReadBytes(len);
                                if (ReceivedFiles.Contains(downTex)) return;
                                File.WriteAllBytes(Path.Combine(ScriptWorker.ClientPath, downTex), bindata);
                                ReceivedFiles.Add(downTex);
                                _textures.Add(downTex, Raylib.LoadTexture(Path.Combine(ScriptWorker.ClientPath, downTex)));
                                break;
                            case "upload":
                                var downFile = message.ReadString();
                                var data = message.ReadString();
                                if (ReceivedFiles.Contains(downFile)) return;
                                File.WriteAllText(Path.Combine(ScriptWorker.ClientPath, downFile), data);
                                ReceivedFiles.Add(downFile);
                                break;
                            case "reload":
                                var reloadThread = new Thread(worker.ReloadCode);
                                reloadThread.Start();
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
                        ReTools.Log(message.ReadString());
                        break;
                }
            }
        }

        public static int GetInput()
        {
            return Raylib.GetKeyPressed();
        }
    }
}
