using System;
using System.IO;

namespace Bigly
{
    class Program
    {
        static void Main(string[] args)
        {
            //TODO args
            //TODO logging

            Console.WriteLine("Hello World!");

            byte[] data = File.ReadAllBytes("INIZH.big");
            //GlobalHeader globalHeader = GlobalHeader.FromBytes(data);
            BigArchive big = BigArchive.FromBytes(data);

            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "INIZH");
            big.WriteAllContents(outputPath);

            Console.WriteLine("Goodbye world!");
        }
    }
}
