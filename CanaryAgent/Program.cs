//This is the main program
using System.Threading.Tasks;
using CanaryAgent.Core;

namespace CanaryAgent
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var agent = new Agent();
            agent.RunContinuously();
        }
    }
}
