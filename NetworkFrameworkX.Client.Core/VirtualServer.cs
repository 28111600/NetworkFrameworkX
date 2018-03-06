using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Client
{
    public class VirtualServer : RemoteCallable, ICaller, ITerminal
    {
        public Client Client { get; set; }

        public CallerType Type => CallerType.Console;

        public override IUdpSender UdpSender => this.Client;

        public string Name => "VirtualServer";

        internal RSAKey RSAKey = null;

        protected override void OnLog(LogLevel level, string name, string text)
        {
            /*
             * do nothing
             */
        }
    }
}