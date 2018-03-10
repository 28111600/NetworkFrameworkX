using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server.Plugin
{
    internal sealed class ServerInfo : IPlugin
    {
        public string Name => "ServerInfo";

        public IServer Server { get; set; }

        public PluginConfig Config { get; private set; } = new PluginConfig();

        public string SerializeConfig() => null;

        private bool CalcCPUUsage = false;

        public void DeserializeConfig(string text)
        {
        }

        public void OnDestroy()
        {
            this.CalcCPUUsage = false;
        }

        public void OnLoad()
        {
            AddCommand_GC();
            AddCommand_Process();
        }

        private void AddCommand_GC()
        {
            Function commandGC = new Function()
            {
                Name = "gc",
                Comment = "Garbage collect",
                Func = (args, caller) => {
                    GC.Collect();
                    return 0;
                }
            };
            this.Server.AddCommand(commandGC);
        }

        private void AddCommand_Process()
        {
            Process CurrentProcess = Process.GetCurrentProcess();

            double CPUUsage = 0;

            ThreadStart ts = new ThreadStart(() => {
                this.CalcCPUUsage = true;

                const int interval = 500;
                TimeSpan preCPUTime = TimeSpan.Zero;
                Stopwatch sw = new Stopwatch();

                double reciprocalProcessorCount = 1 / Environment.ProcessorCount;

                sw.Restart();

                while (this.CalcCPUUsage) {
                    TimeSpan curCPUTime = CurrentProcess.TotalProcessorTime;
                    CPUUsage = (curCPUTime - preCPUTime).TotalMilliseconds / sw.ElapsedMilliseconds;
                    preCPUTime = curCPUTime;
                    sw.Restart();
                    Thread.Sleep(interval);
                }
            });

            new Thread(ts).Start();

            Function commandMemory = new Function()
            {
                Name = "mem",
                Comment = "Reports memory info",
                Func = (args, caller) => {
                    CurrentProcess.Refresh();
                    caller.Logger.Info("Load Average");

                    var dict = new Dictionary<string, string>();

                    dict.Add("CPU Usage", $"{CPUUsage * 100:0.00} %");
                    dict.Add("Threads", CurrentProcess.Threads.Count.ToString());
                    dict.Add("Memory Usage", Utility.GetSizeString(CurrentProcess.WorkingSet64));
                    dict.Add("Network", $"In : {Utility.GetSizeString(this.Server.Traffic_In)} / Out : {Utility.GetSizeString(this.Server.Traffic_Out)}");

                    caller.Logger.Info(dict);

                    return 0;
                }
            };
            this.Server.AddCommand(commandMemory);

            var targetFramework = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(TargetFrameworkAttribute), true) as TargetFrameworkAttribute[];
            string frameworkDisplayName = targetFramework.Length > 0 ? targetFramework[0].FrameworkDisplayName : string.Empty;

            Function commandServerInfo = new Function()
            {
                Name = "server-info",
                Comment = "Reports server info",
                Func = (args, caller) => {
                    CurrentProcess.Refresh();

                    caller.Logger.Info("Server Info");

                    var dict = new Dictionary<string, string>();

                    dict.Add("Machine Name", Environment.MachineName);
                    dict.Add("User Name", Environment.UserName);
                    dict.Add("Platform", Environment.OSVersion.ToString());
                    dict.Add("Target Framework", frameworkDisplayName);
                    dict.Add("Start Time", CurrentProcess.StartTime.GetDateTimeString());
                    dict.Add("Date Now", DateTime.Now.GetDateTimeString());

                    caller.Logger.Info(dict);

                    this.Server.CallCommand("mem", caller);

                    return 0;
                }
            };

            this.Server.AddCommand(commandServerInfo);
        }
    }
}