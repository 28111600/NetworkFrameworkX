using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Client.Sample
{
    public partial class FrmLogin : Form
    {
        private Client Client;

        public FrmLogin()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.textUserName.Text = "username";
            this.textPassword.Text = "password";
            this.textHost.Text = "127.0.0.1";
            this.textPort.Text = "32768";
        }

        private void Button1_Click(object _sender, EventArgs _e)
        {
            if (this.Client == null || this.Client.Status.In(ServerStatus.Close)) {
                string Host = this.textHost.Text;

                IPAddress[] IPAddressList = Dns.GetHostAddresses(Host);
                IPAddress IPAddress = IPAddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                if (this.Client == null) {
                    this.Client = new Client();

                    this.Client.Log += (sender, e) => {
                        this.Invoke(new MethodInvoker(() => {
                            StringBuilder write = new StringBuilder();
                            if (string.IsNullOrWhiteSpace(e.Name)) {
                                write.AppendLine(string.Format("[{0} {1}]: {2}", Utility.GetTimeString(DateTime.Now), e.LevelText, e.Text));
                            } else {
                                write.AppendLine(string.Format("[{0} {1}][{2}]: {3}", Utility.GetTimeString(DateTime.Now), e.LevelText, e.Name, e.Text));
                            }

                            this.textLog.Text += write.ToString();
                            this.textLog.Select(this.textLog.Text.Length, 0);
                            this.textLog.ScrollToCaret();
                        }));
                    };

                    this.Client.ClientLogin += (sender, e) => {
                    };

                    this.Client.StatusChanged += (sender, e) => {
                        this.Invoke(new MethodInvoker(() => {
                            switch (e.Status) {
                                case ServerStatus.Connecting:
                                    this.textHost.ReadOnly = true;
                                    this.textPort.ReadOnly = true;
                                    this.textUserName.ReadOnly = true;
                                    this.btnLogin.Enabled = false;
                                    this.textCommand.Focus();
                                    break;

                                case ServerStatus.Connected:

                                    string username = this.textUserName.Text;
                                    string password = this.textPassword.Text;
                                    Arguments args = new Arguments();
                                    args.Put("username", username);
                                    args.Put("password", password);
                                    this.Client.Login(args);
                                    break;

                                case ServerStatus.Close:

                                    this.textHost.ReadOnly = false;
                                    this.textPort.ReadOnly = false;
                                    this.textUserName.ReadOnly = false;
                                    this.btnLogin.Enabled = true;
                                    this.btnLogin.Focus();
                                    break;
                            }
                        }));
                    };
                }

                if (IPAddress != null) {
                    string IP = IPAddress.ToString();
                    int Port = int.Parse(this.textPort.Text);

                    this.Client.Start(IP, Port);
                }
            }
        }

        private void TextCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) {
                if (!string.IsNullOrWhiteSpace(this.textCommand.Text)) {
                    if (this.Client != null && this.Client.Status == ServerStatus.Connected) {
                        string Command = this.textCommand.Text;
                        this.textCommand.Text = string.Empty;
                        if (Command.IndexOf("/") == 0) {
                            this.Client.HandleCommand(Command.Substring(1));
                        } else {
                            this.Client.HandleCommand($"say {Command}");
                        }
                        e.SuppressKeyPress = true;
                    }
                }
            }
        }

        private void FrmLogin_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.Client != null) {
                this.Client.Stop();
                Application.Exit();
            }
        }
    }
}