//by luyikk 2010.5.9

using System;
using System.Reflection;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ZYSocket.Share
{
    /// <summary>
    ///     泛型的对象池-可输入构造函数,以及参数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T> where T : new()
    {

   
        /// <summary>
        ///     对象处理代理
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pool"></param>
        /// <returns></returns>
        public delegate T ObjectRunTimeHandle(T obj, ObjectPool<T> pool);


        /// <summary>
        /// 对象第一次创建时的代理
        /// </summary>
        public ObjectRunTimeHandle? ObjectCreateRunTime { get; set; }


        /// <summary>
        ///     获取对象时所处理的方法
        /// </summary>
        public ObjectRunTimeHandle? GetObjectRunTime { get; set; }
     

        /// <summary>
        ///     回收对象时处理的方法
        /// </summary>
        public ObjectRunTimeHandle? ReleaseObjectRunTime { get; set; }

        /// <summary>
        ///     最大对象数量
        /// </summary>
        public int MaxObjectCount { get; set; }


        /// <summary>
        ///     对象存储Stack
        /// </summary>
        public ConcurrentStack<T> ObjectStack { get; set; }


        /// <summary>
        ///     构造函数
        /// </summary>
        public ConstructorInfo? TheConstructor { get; set; }

        /// <summary>
        ///     参数
        /// </summary>
        public object[]? Param { get; set; }

        public ObjectPool(int maxObjectCount)
        {
            ObjectStack = new ConcurrentStack<T>();
            MaxObjectCount = maxObjectCount;
        }

        private T GetT()
        {
            T x;
            if (TheConstructor != null)
                x = (T)TheConstructor.Invoke(Param);
            else
                x = new T();

            if (ObjectCreateRunTime != null)
                x=ObjectCreateRunTime(x, this);

            return x;
        }


        /// <summary>
        ///     获取对象
        /// </summary>
        /// <returns></returns>
        public T GetObject()
        {
            if (ObjectStack.TryPop(out T p))
            {

                if (GetObjectRunTime != null)
                    p = GetObjectRunTime(p, this);
                return p;
            }

            p = GetT();
            if (GetObjectRunTime != null)
                p = GetObjectRunTime(p, this);
            return p;
        }

        /// <summary>
        ///     获取对象
        /// </summary>
        /// <param name="cout"></param>
        /// <returns></returns>
        public T[] GetObject(int cout)
        {


            T[] p = new T[cout];

            int Lpcout = ObjectStack.TryPopRange(p);


            if (Lpcout < cout)
            {
                int x = cout - Lpcout;

                T[] xp = new T[x];

                for (int i = 0; i < x; i++)
                {
                    xp[i] = GetT();
                }

                Array.Copy(xp, 0, p, Lpcout, x);
            }

            if (GetObjectRunTime != null)
            {
                for (int i = 0; i < p.Length; i++)
                {
                    p[i] = GetObjectRunTime(p[i], this);

                }

            }


            return p;


        }


        /// <summary>
        ///     回收对象
        /// </summary>
        /// <param name="obj"></param>
        public void ReleaseObject(T obj)
        {
            if (ReleaseObjectRunTime != null)
            {
                obj = ReleaseObjectRunTime(obj, this);

                if (obj == null)
                    return;
            }

            if (ObjectStack.Count >= MaxObjectCount)
            {
                if (obj is IDisposable)
                    ((IDisposable)obj).Dispose();
            }
            else
            {
                ObjectStack.Push(obj);
            }

        }

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="obj"></param>
        public void ReleaseObject(T[] obj)
        {
            foreach (var p in obj)
                ReleaseObject(p);
        }
    }



    /// <summary>A SocketAsyncEventArgs with an associated async method builder.</summary>
    public class TaskSocketAsyncEventArgs<TResult> : SocketAsyncEventArgs
    {
      
        public AsyncTaskMethodBuilder<TResult> _builder;

        public new event EventHandler<TaskSocketAsyncEventArgs<TResult>>? Completed;

        public bool _accessed = false;
        public TaskSocketAsyncEventArgs() :base() // avoid flowing context at lower layers as we only expose Task, which handles it
        {
            base.Completed += TaskSocketAsyncEventArgs_Completed;
        }

        private void TaskSocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            this.Completed?.Invoke(sender, this);
        }

        /// <summary>Gets the builder's task with appropriate synchronization.</summary>
        public ref AsyncTaskMethodBuilder<TResult> GetCompletionResponsibility(out bool responsibleForReturningToPool)
        {
            lock (this)
            {
                responsibleForReturningToPool = _accessed;
                _accessed = true;             
                return ref _builder;
            }
        }
    }

    public class SendTaskSocketAsyncEventArgs : TaskSocketAsyncEventArgs<int>
    {
    
        public new event EventHandler<SendTaskSocketAsyncEventArgs>? Completed;

        public void Reset()
        {
            _accessed = false;
            _builder = default; 
        }

        public SendTaskSocketAsyncEventArgs():base(){
            base.Completed += IntTaskSocketAsyncEventArgs_Completed;
        }

        private void IntTaskSocketAsyncEventArgs_Completed(object sender, TaskSocketAsyncEventArgs<int> e)
        {
            Completed?.Invoke(sender, this);
        }

        public Task<int> SendSync(Socket sock)
        {
            Task<int> t;
            
            if (sock.SendAsync(this))
            {
                t = GetCompletionResponsibility(out bool responsibleForReturningToPool).Task;

                if (responsibleForReturningToPool)
                    Reset();
            }
            else
            {              
                if (SocketError == SocketError.Success)
                    return Task.FromResult(BytesTransferred);
                else if (SocketError != SocketError.TimedOut &&
                     SocketError != SocketError.ConnectionReset &&
                     SocketError != SocketError.OperationAborted &&
                     SocketError != SocketError.Shutdown &&
                     SocketError != SocketError.ConnectionAborted&&
                     SocketError !=SocketError.Interrupted)
                        return Task.FromException<int>(GetException(SocketError));
                else
                    return Task.FromResult(0);
            }

            return t;
        }

        private static Exception GetException(SocketError error)
        {
            return new IOException($"SocketError:{error}",new SocketException((int)error));
           
        }
    }

       

    public class SendSocketAsyncEventPool:ObjectPool<SendTaskSocketAsyncEventArgs>
    {
        public static SendSocketAsyncEventPool Shared { get; } = new SendSocketAsyncEventPool();


        public SendSocketAsyncEventPool(int maxObjectCount=1000) :base(maxObjectCount)
        {
            base.ObjectCreateRunTime=(obj,pool)=>
            {
                obj.Completed += Async_Completed;
                return obj;
            };

          
            base.ReleaseObjectRunTime = (obj, pool) =>
              {
                  obj.Reset();
                  return obj;
              };
        }

        private void Async_Completed(object sender, SendTaskSocketAsyncEventArgs e)
        {
            CompleteAccept(e);

        }

        private static void CompleteAccept(SendTaskSocketAsyncEventArgs saea)
        {
          
            SocketError error = saea.SocketError;

            AsyncTaskMethodBuilder<int> builder = saea.GetCompletionResponsibility(out bool responsibleForReturningToPool);

            if (responsibleForReturningToPool)
                saea.Reset();
          
            if (error == SocketError.Success)
            {
                builder.SetResult(saea.BytesTransferred);
            }
            else if (error != SocketError.TimedOut &&
                     error != SocketError.ConnectionReset &&
                     error != SocketError.OperationAborted &&
                     error != SocketError.Shutdown &&
                     error != SocketError.ConnectionAborted&&
                     error != SocketError.Interrupted)
            {
                builder.SetException(GetException(error));
            }
            else
            {
                builder.SetResult(0);
            }

        }

        private static Exception GetException(SocketError error)
        {
            Exception e = new SocketException((int)error);
            return new IOException($"SocketError:{error}", e);

        }



    }


}