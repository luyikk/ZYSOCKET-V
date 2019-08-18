using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ZYSocket.FiberStream.Synchronization;

namespace TestSyncRun
{
    class Program
    {
        static async Task Main(string[] args)
        {
            SyncRun lambda = new SyncRun();

            #region use akka model
            {
                int icount = 0;

                List<Task> waitlist = new List<Task>();
                for (int i = 0; i < 10000; i++)
                {
                    waitlist.Add(Task.Factory.StartNew((p) =>
                    {
                        lambda.Tell(() =>
                        {
                            icount += (int)p;
                        });

                    }, i));
                }


                for (int i = 0; i < 10000; i++)
                {
                    waitlist.Add(Task.Factory.StartNew((p) =>
                    {
                        lambda.Tell(() =>
                        {
                            icount -= (int)p;

                        });

                    }, i));
                }

                await Task.WhenAll(waitlist);
                Debug.Assert(icount == 0);
                Console.WriteLine($"tell:{icount}");
            }

            {
                int icount = 0;

                List<Task> waitlist = new List<Task>();
                for (int i = 0; i < 10000; i++)
                {
                    waitlist.Add(Task.Factory.StartNew(async (p) =>
                    {
                        var res = await lambda.Ask(() => p);
                        icount -= res;
                    }, i));
                }

                for (int i = 0; i < 10000; i++)
                {
                    waitlist.Add(Task.Factory.StartNew(async (p) =>
                    {
                        var res = await lambda.Ask(() => p);
                        icount += res;
                    }, i));
                }

                await Task.WhenAll(waitlist);

                Debug.Assert(icount == 0);

                Console.WriteLine($"tell:{icount}");
            }

            {
                int icount = 0;

                List<Task> waitlist = new List<Task>();
                for (int i = 0; i < 10000; i++)
                {
                    waitlist.Add(Task.Factory.StartNew(async (p) =>
                    {
                        await lambda.Ask(() =>
                        {
                            icount += (int)p;
                        });

                    }, i));
                }

                for (int i = 0; i < 10000; i++)
                {
                    waitlist.Add(Task.Factory.StartNew(async (p) =>
                    {
                        await lambda.Ask(() =>
                        {
                            icount -= (int)p;
                        });

                    }, i));
                }

                await Task.WhenAll(waitlist);

                Debug.Assert(icount == 0);

                Console.WriteLine($"tell:{icount}");
            }

            #endregion

            var stop = System.Diagnostics.Stopwatch.StartNew();

            int x = 0;
            var count = 0;
            int cc = 0;

            for (int i = 0; i < 2000000; i++)
            {
               cc+= await lambda.Ask(() => {
                    x += i;
                    return x;
                });
                count++;
            }

            stop.Stop();
            Console.WriteLine(x);
            Console.WriteLine(cc);
            Console.WriteLine($"Count:{count} time {stop.ElapsedMilliseconds}");


            Console.ReadLine();
        }
    }
}
