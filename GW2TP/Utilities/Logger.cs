using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GW2TP
{
    /// <summary>
    /// Summary description for Logger Singleton.
    /// </summary>
    public class Logger
    {
        private static Logger _singleton = null;
        private static Object _classLock = typeof(Logger);

        private TextWriter _logWriter = null;

        private Logger() { }

        public static Logger Instance
        {
            get 
            {
                lock (_classLock)
                {
                    return _singleton ?? (_singleton = new Logger());
                }
            }
        }

        public void CreateEntry(string entry)
        {
            _logWriter.WriteLine("{0} - {1}",
                    DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss"),
                    entry);
        }

        public void Open(string filePath, bool append)
        {
            if (this._logWriter != null)
                throw new InvalidOperationException(
                    "Logger is already open");

            // set append to true
            StreamWriter sw = new StreamWriter(filePath, append, UnicodeEncoding.Default);
            sw.AutoFlush = true;
            this._logWriter = TextWriter.Synchronized(sw);
        }

        public void Close()
        {
            if (this._logWriter != null)
            {
                this._logWriter.Close();
                this._logWriter.Dispose();
                this._logWriter = null;
            }
        }
    }
}
