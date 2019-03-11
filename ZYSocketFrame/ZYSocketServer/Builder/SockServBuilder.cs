using Autofac;
using System;
using System.Buffers;
using System.Text;
using ZYSocket.Share;
using ZYSocket.Interface;
using ZYSocket.FiberStream;

namespace ZYSocket.Server.Builder
{
    public class SockServBuilder :  ISockServBuilder,IDisposable
    {
        private ContainerBuilder Container { get; set; }

        public IContainer ContainerBuilder { get; private set; }
     

        public SockServBuilder(Func<IComponentContext,ISocketServer> func=null)
        {
            Container = new ContainerBuilder();
            ConfigureDefaults();
            if (func is null)
                Container.Register<ZYSocketSuper>(p => new ZYSocketSuper(p)).As<ISocketServer>().SingleInstance();
            else
                Container.Register<ISocketServer>(func).SingleInstance();

        }

        public SockServBuilder(ContainerBuilder container, Func<IComponentContext, ISocketServer> func = null)
        {
            this.Container = container;
            ConfigureDefaults();

            if (func is null)
                Container.Register<ZYSocketSuper>(p => new ZYSocketSuper(p)).As<ISocketServer>().SingleInstance();
            else
                Container.Register<ISocketServer>(func).SingleInstance();            
        }



        public ISockServBuilder ConfigureDefaults()
        {
            ConfigServer();
            ConfigMemoryPool();
            ConfigISend();
            ConfigIAsyncSend();
            ConfigEncode();
            ConfigObjFormat();
            return this;
        }

        public ISockServBuilder ConfigServer(Action<SocketServerOptions> config = null)
        {
            Container.Register<SocketServerOptions>(p =>
               {
                   var c = new SocketServerOptions();
                   config?.Invoke(c);
                   return c;
               }).SingleInstance();

            return this;
        }

        public ISockServBuilder ConfigEncode(Func<Encoding> func=null)
        {
            Container.Register<Encoding>(p =>
            {
                if (func is null)
                    return Encoding.UTF8;
                else
                    return func();
            }).SingleInstance();

            return this;
        }

        public ISockServBuilder ConfigMemoryPool(Func<MemoryPool<byte>> func = null)
        {
            Container.Register<MemoryPool<byte>>(p =>
            {
                if (func is null)
                    return new Thruster.FastMemoryPool<byte>();
                else
                    return func();
            });

            return this;
        }


        public ISockServBuilder ConfigISend(Func<ISend> func=null)
        {
            Container.Register<ISend>(p =>
            {
                if (func is null)
                    return new PoolSend();
                else
                    return func();
            });

            return this;
        }

        public ISockServBuilder ConfigIAsyncSend(Func<IAsyncSend> func = null)
        {
            Container.Register<IAsyncSend>(p =>
            {
                if (func is null)
                    return new PoolSend();
                else
                    return func();
            });

            return this;
        }

        public ISockServBuilder ConfigObjFormat(Func<ISerialization> func = null)
        {
            Container.Register<ISerialization>(p =>
            {
                if (func is null)
                    return new ProtobuffObjFormat();
                else
                    return func();
            });

            return this;

        }


        public ISocketServer Bulid()
        {
            var build = Container.Build();

            ContainerBuilder = build;

            return build.Resolve<ISocketServer>();
        }

        public void Dispose()
        {
            ContainerBuilder?.Dispose();
        }
    }
}
