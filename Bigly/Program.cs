using System;
using System.IO;

namespace Bigly
{
    class Program
    {
        static void Main(string[] args)
        {
            //TODO args

            Console.WriteLine("Hello World!");

            byte[] data = File.ReadAllBytes("INIZH.big");
            GlobalHeader globalHeader = GlobalHeader.FromBytes(data);

            Console.WriteLine("Goodbye world!");
        }
    }
}
