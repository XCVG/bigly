using System;
using System.IO;

namespace Bigly
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                    throw new NotSupportedException($"An operation must be specified");

                //TODO args
                //TODO logging

                if (args[0].Equals("extract", StringComparison.OrdinalIgnoreCase))
                {
                    string filePath = args[1];
                    string fileName = Path.GetFileName(filePath);

                    Console.WriteLine("Extracting " + fileName);

                    byte[] data = File.ReadAllBytes(filePath);
                    //GlobalHeader globalHeader = GlobalHeader.FromBytes(data);
                    BigArchive big = BigArchive.FromBytes(data);

                    string outputPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(fileName));
                    big.WriteAllContents(outputPath);

                    Console.WriteLine("Done extracting " + fileName);
                }
                else if (args[0].Equals("pack", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotImplementedException();
                }
                else
                {
                    throw new NotSupportedException($"Operation \"{args[0]}\" is not supported");
                }

                return 0;
            }
            catch(Exception e)
            {
                Console.Error.WriteLine($"Fatal error: {e.GetType().Name}");
                Console.Error.WriteLine(e.Message);
                return 1;
            }
        }
    }
}
