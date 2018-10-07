using System;

namespace IL2CPU
{
    internal static class Program
    {
        private static int Main(string[] args) =>
            Cosmos.IL2CPU.Program.Run(args,
                m => Console.WriteLine($"Message: {m}"),
                e => Console.Error.WriteLine($"Error: {e}"));
    }
}
