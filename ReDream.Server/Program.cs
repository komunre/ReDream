using System;

namespace ReDream.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var worker = new ServerWorker(args[0]);

            while (true)
            {
                worker.Update();
            }
        }
    }
}
