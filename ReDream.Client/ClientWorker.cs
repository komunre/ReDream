using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReDream.Shared;
using Raylib_cs;
using Lidgren.Network;

namespace ReDream.Client
{
    class ClientWorker 
    {
        ScriptWorker worker = new();
       

        public ClientWorker()
        {
            worker.Initialize("client_lua/");
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

        protected NetClient client;

        public void Connect(string url, int port)
        {
            var config = new NetPeerConfiguration("ReDream");
            client = new NetClient(config);
            client.Start();
            client.Connect(url, port);
        }

        public void SendInput(string action)
        {
            var msg = client.CreateMessage();
            msg.Write("move");
            msg.Write(action);
            client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }

        public int GetInput()
        {
            return Raylib.GetKeyPressed();
        }
    }
}
