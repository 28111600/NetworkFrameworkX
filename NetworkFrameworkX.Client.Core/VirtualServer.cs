using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Client
{
    public class VirtualServer : RemoteCallable, ICaller
    {
        public Client Client { get; set; }

        public CallerType Type => CallerType.Console;

        public string Name => "VirtualServer";

        internal RSAKey RSAKey = null;

        internal override TcpClient TcpClient => this.Client.TcpClient;

        public int CallFunction(string name, IArguments args = null) => this.CallFunction(name, args ?? new Arguments(), this);

        protected override void OnLog(LogLevel level, string name, string text)
        {
            /*
             * do nothing
             */
        }
    }
}