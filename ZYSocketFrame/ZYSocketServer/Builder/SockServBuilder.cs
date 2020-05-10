using System;
using System.Buffers;
using System.Text;
using ZYSocket.Share;
using ZYSocket.Interface;
using ZYSocket.FiberStream;
using Microsoft.Extensions.DependencyInjection;

namespace ZYSocket.Server.Builder
{
    public class SockServBuilder :  ISockServBuilder,IDisposable
    {
        private IServiceCollection Container { get; set; }

        public IServiceProvider? ContainerBuilder { get; private set; }
     

        public SockServBuilder(Func<IServiceProvider, ISocketServer>? func=null)
        {
            Container = new ServiceCollection();
            ConfigureDefaults();

            if (func is null)
                Container.AddSingleton<ISocketServer, ZYSocketSuper>(p => new ZYSocketSuper(p));
            else
                Container.AddSingleton<ISocketServer>(func);

        }

        public SockServBuilder(IServiceCollection container, Func<IServiceProvider, ISocketServer>? func = null)
        {
            this.Container = container;
            ConfigureDefaults();

            if (func is null)
                Container.AddSingleton<ISocketServer, ZYSocketSuper>(p => new ZYSocketSuper(p));
            else
                Container.AddSingleton<ISocketServer, ISocketServer>(func);
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

        public ISockServBuilder ConfigServer(Action<SocketServerOptions>? config = null)
        {
            Container.AddSingleton<SocketServerOptions>(p =>
               {
                   var c = new SocketServerOptions();
                   config?.Invoke(c);
                   return c;
               });

            return this;
        }

        public ISockServBuilder ConfigEncode(Func<Encoding>? func=null)
        {
            Container.AddSingleton<Encoding>(p =>
            {
                if (func is null)
                    return Encoding.UTF8;
                else
                    return func();
            });

            return this;
        }

        public ISockServBuilder ConfigMemoryPool(Func<MemoryPool<byte>>? func = null)
        {
            Container.AddTransient<MemoryPool<byte>>(p =>
            {
                if (func is null)
                {
                    var config = p.GetRequiredService<SocketServerOptions>();
                    ReadBytes.MaxPackerSize = config.MaxPackerSize;
                    return new Thruster.FastMemoryPool<byte>(config.MaxPackerSize);
                }
                else
                    return func();
            });

            return this;
        }


        public ISockServBuilder ConfigISend(Func<ISend>? func=null)
        {
            Container.AddTransient<ISend>(p =>
            {
                if (func is null)
                    return new NetSend(true);
                else
                    return func();
            });

            return this;
        }

        public ISockServBuilder ConfigIAsyncSend(Func<IAsyncSend>? func = null)
        {
            Container.AddTransient<IAsyncSend>(p =>
            {
                if (func is null)
                    return new NetSend(true);
                else
                    return func();
            });

            return this;
        }

        public ISockServBuilder ConfigObjFormat(Func<ISerialization>? func = null)
        {
            Container.AddTransient<ISerialization>(p =>
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
            var build = Container.BuildServiceProvider();

            ContainerBuilder = build;

            return build.GetRequiredService<ISocketServer>();
        }

        public void Dispose()
        {
            if (ContainerBuilder is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
