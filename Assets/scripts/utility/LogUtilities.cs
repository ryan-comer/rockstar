using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Assets.scripts.utility
{
    public static class LogUtilities
    {

        private static string logPath = "";

        public static void SetLogPath(string newPath)
        {
            logPath = newPath;
        }

        public static bool Log(string message)
        {
            if (string.IsNullOrEmpty(logPath))
            {
                return false;
            }

            using(var file = File.AppendText(logPath))
            {
                file.WriteLine(message);
            }

            return true;
        }
    }
}
