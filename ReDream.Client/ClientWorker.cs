using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReDream.Shared;
using Raylib_cs;
using Lidgren.Network;
using System.Reflection;

namespace ReDream.Client
{
    public class ClientWorker 
    {
        ScriptWorker worker = new();
       

        public ClientWorker()
        {
            worker.AddType(typeof(ClientWorker).GetTypeInfo().Assembly.Location);
            worker.Initialize("client_cs/");
            worker.eng.SetValue("getInput", new Func<int>(GetInput));
            worker.eng.SetValue("sendInput", new Action<string>(SendInput));
            Raylib.InitWindow(800, 600, "ReDream Client");
            Raylib.SetTargetFPS(60);
        }

        public void Update()
        {
            worker.Update();
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(255, 255, 255, 255));
            Raylib.EndDrawing();
        }

        protected static NetClient client;

        public void Connect(string url, int port)
        {
            var config = new NetPeerConfiguration("ReDream");
            client = new NetClient(config);
            client.Start();
            client.Connect(url, port);
        }

        public static void SendInput(string action)
        {
            var msg = client.CreateMessage();
            msg.Write("move");
            msg.Write(action);
            client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }

        public static int GetInput()
        {
            return Raylib.GetKeyPressed();
        }
    }
}
