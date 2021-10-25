using System;
using System.IO;

namespace ReDream.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var worker = new ServerWorker(args[0], Path.Combine(args[0], "client"));

            while (true)
            {
                worker.Update();
            }
        }
    }
}
