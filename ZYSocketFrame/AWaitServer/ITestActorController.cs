using System.Threading.Tasks;
using Netx;
using Netx.Loggine;

namespace AWaitServer
{
    [Build]
    public interface ITestActorController
    {
        [TAG(1000)]
        Task Run();
    }
}