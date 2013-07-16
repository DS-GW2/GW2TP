using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Drawing;
using System.Net;

namespace GW2TP
{
    public class ImageCache
    {
        protected String _cachePath = "";
        LockFreeQueue<Tuple<String, String>> _queue = new LockFreeQueue<Tuple<string, string>>();
        private String _cacheDirectory;
        private bool _isRunning = true;
        public event EventHandler<CacheStoredEventArgs> CacheStored;
        Thread thread;
        public bool IsRunning
        {
            get { return _isRunning; }
            set { 
                _isRunning = value;
                if (_isRunning == false && thread != null)
                {
                    thread.Join();
                    thread = null;
                }
            }
        }

        public String CacheDirectory
        {
            get { return _cacheDirectory; }
            protected set { _cacheDirectory = value; }
        }

        public ImageCache(String cachePath)
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AssemblyName asmName = Assembly.GetEntryAssembly().GetName(); // name of the main executing project
            path = System.IO.Path.Combine(path, asmName.Name);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            this.CacheDirectory = cachePath;
            this._cachePath = System.IO.Path.Combine(path, CacheDirectory);
            if (!Directory.Exists(this._cachePath))
            {
                Directory.CreateDirectory(this._cachePath);
            }
        }

        public void StoreAndRequest(string uri, string name)
        {
            Tuple<String, String> item = new Tuple<string, string>(uri, name);
            if (!IsStored(name))
            {
                Start(item); // Enqueue with tuple
            }
            else
            {
                if (CacheStored != null)
                {
                    CacheStored(this, new CacheStoredEventArgs(item.Item2)); // directly give stored file
                }
            }
        }

        public bool IsStored(string name)
        {
            return File.Exists(Path.Combine(_cachePath, name));
        }

        public String GetPath(string name)
        {
            if (IsStored(name))
            {
                return Path.Combine(_cachePath, name);
            }
            return "";
        }

        private void Start(Tuple<String, String> item)
        {
            if (thread == null)
            {
                thread = new Thread(new ParameterizedThreadStart(Worker));
                thread.Start();
            }
            this._queue.Enqueue(item);
        }

        private Image LoadImage(string url)
        {
            System.Net.WebRequest request =
                System.Net.WebRequest.Create(url);

            System.Net.WebResponse response = request.GetResponse();
            System.IO.Stream responseStream =
                response.GetResponseStream();

            Bitmap bmp = new Bitmap(responseStream);

            responseStream.Dispose();

            return bmp;
        }

        private void Worker(object obj)
        {
            Tuple<String, String> item;
            while (IsRunning)
            {
                while (_queue.Dequeue(out item))
                {
                    if (item != null && !String.IsNullOrEmpty(item.Item1) && !String.IsNullOrEmpty(item.Item2))
                    {
                        try
                        {
                            //using (var w = new WebClient())
                            //{
                            //    w.Proxy = new WebProxy("127.0.0.1", 8888);
                            //    w.DownloadFile(item.Item1, Path.Combine(_cachePath, item.Item2));
                            //    Thread.Sleep(5);
                            //}
                            Image img = LoadImage(item.Item1);
                            img.Save(Path.Combine(_cachePath, item.Item2));
                            Thread.Sleep(5);
                        }
                        catch
                        {
                            _queue.Enqueue(item);
                        }
                    }
                    if (CacheStored != null)
                    {
                        CacheStored(this, new CacheStoredEventArgs(item.Item2));
                    }
                    Thread.Sleep(1);
                }
                Thread.Sleep(50);
            }
        }
    }
}
