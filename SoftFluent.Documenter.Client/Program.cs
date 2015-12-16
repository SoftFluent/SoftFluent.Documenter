using System;
using System.IO;
using SoftFluent.Documenter;

namespace SoftFluent.Documenter.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            // controls
            if (args.Length < 1)
            {
                Console.WriteLine("Not enough arguments!");
            }
            if (args.Length > 1)
            {
                Console.WriteLine("Too much arguments!");
            }

            Documenter documenter = new Documenter();
            documenter.RenderDirectory(args[0]);
        }
    }
}
