using System;
using System.IO;

namespace Bigly
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //TODO args
                //TODO logging

                if (!args[0].Equals("extract", StringComparison.OrdinalIgnoreCase))
                    throw new NotImplementedException();

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
            catch(Exception e)
            {
                Console.Error.WriteLine($"Fatal error: {e.GetType().Name}");
                Console.Error.WriteLine(e.Message);
            }
        }
    }
}
