using Serilog;

namespace IL2CPU
{
    public class Program
    {
        public static int Main(string[] args) =>
            Cosmos.IL2CPU.Program.Run(args, new LoggerConfiguration().WriteTo.Console().CreateLogger());
    }
}
