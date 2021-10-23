using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReDream.Shared;

namespace ReDream.Server
{
    public class ServerWorker
    {
        ScriptWorker worker = new();
        public ServerWorker(string path)
        {
            worker.Initialize(path);
        }

        public void Update()
        {
            worker.Update();
            
        }
    }
}
