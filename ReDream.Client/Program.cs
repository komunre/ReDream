using System;
using Raylib_cs;

namespace ReDream.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new ClientWorker();
            client.Connect(args[0], int.Parse(args[1]));

            while (!Raylib.WindowShouldClose()) {
                client.Update();
            }
        }
    }
}
