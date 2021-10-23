using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReDream.Shared
{
    public class ReTools
    {
        public static void Log(string msg)
        {
            var time = DateTime.Now;
            Console.WriteLine("[" + time.Hour + ":" + time.Minute + ":" + time.Second + ":" + time.Millisecond + "] " + msg);
        }

        public static void LogArr(Array arr)
        {
            Log("[");
            foreach (var element in arr)
            {
                Log(element.ToString());
            }
            Log("]");
        }
    }
}
