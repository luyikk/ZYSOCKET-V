using Autofac;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace ZYSocket.Server.Builder
{
    public class SockServBuilder :  ISockServBuilder,IDisposable
    {
        private ContainerBuilder Container { get; set; }

        public IContainer ContainerBuilder { get; private set; }

        public SockServBuilder()
        {
            Container = new ContainerBuilder();
            ConfigureDefaults();
            Container.Register<ZYSocketSuper>(p => new ZYSocketSuper(p)).As<ISocketServer>().SingleInstance();
        }

        public SockServBuilder(ContainerBuilder container)
        {
            this.Container = container;
            ConfigureDefaults();
            Container.Register<ZYSocketSuper>(p => new ZYSocketSuper(p)).As<ISocketServer>().SingleInstance();
        }



        public ISockServBuilder ConfigureDefaults()
        {
            ConfigServer();
            ConfigMemoryPool();
            ConfigISend();
            ConfigIAsyncSend();
            ConfigEncode();
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
