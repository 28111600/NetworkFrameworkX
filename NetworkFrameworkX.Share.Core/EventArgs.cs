using System;
using System.Net;

namespace NetworkFrameworkX.Share
{
    public class DataReceivedEventArgs : EventArgs
    {
        public IPAddress IPAddress { get; private set; }

        public int Port { get; private set; }

        public string Text { get; private set; }

        public DataReceivedEventArgs(IPAddress ipAddress, int port, string text)
        {
            this.IPAddress = ipAddress;
            this.Port = port;
            this.Text = text;
        }
    }
}