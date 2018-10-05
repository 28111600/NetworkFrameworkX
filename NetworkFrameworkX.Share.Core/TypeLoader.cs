using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;

namespace NetworkFrameworkX.Share
{
    internal class TypeLoader
    {
        public RemoteTypeLoader RemoteTypeLoader { get; protected set; }

        public AppDomain RemoteDomain { get; private set; }

        public bool UnLoadable => this.RemoteDomain != null;

        public string AssemblyPath { get; protected set; }

        public RemoteTypeLoader CreateRemoteTypeLoader(Type remoteLoaderType, string[] assemblyResolvePath = null)
        {
            AppDomainSetup appDomainSetup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                PrivateBinPath = assemblyResolvePath.Join(Path.PathSeparator)
            };

            this.RemoteDomain = AppDomain.CreateDomain(string.Concat("SubAppDomain_", Guid.NewGuid().ToString("N")), null, appDomainSetup);
           
            RemoteTypeLoader remoteTypeLoader = this.RemoteDomain.CreateInstanceAndUnwrap(remoteLoaderType.Assembly.FullName, remoteLoaderType.FullName) as RemoteTypeLoader;

            return remoteTypeLoader;
        }

        protected TypeLoader()
        {
        }

        public TypeLoader(string assemblyPath, string[] assemblyResolvePath = null)
        {
            this.RemoteTypeLoader = CreateRemoteTypeLoader(typeof(RemoteTypeLoader), assemblyResolvePath);
            this.RemoteTypeLoader.InitTypeLoader(assemblyPath);
        }

        public virtual bool Unload()
        {
            try {
                if (this.RemoteDomain != null && this.UnLoadable) {
                    AppDomain.Unload(this.RemoteDomain);
                    return true;
                }
            } catch {
            }
            return false;
        }
    }

    internal class RemoteTypeLoader : MarshalByRefObject
    {
        private string AssemblyPath { get; set; }
        private Assembly Assembly { get; set; }

        public RemoteTypeLoader()
        {
            LifetimeServices.LeaseTime = TimeSpan.Zero;
        }

        protected virtual Assembly LoadAssembly(string assemblyPath) => Assembly.LoadFile(assemblyPath);

        public void InitTypeLoader(string assemblyPath)
        {
            this.AssemblyPath = assemblyPath;
            this.Assembly = this.LoadAssembly(this.AssemblyPath);
        }

        public static bool TryGetInstance<T>(Assembly asm, out T instance) where T : class
        {
            Type type = asm.GetTypes().FirstOrDefault(x => x.GetInterfaces().Any((y) => y == typeof(T)));
            if (type != null) {
                instance = Activator.CreateInstance(type) as T;
            } else {
                instance = null;
            }

            return instance != null;
        }
    }
}