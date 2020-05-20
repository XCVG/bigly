using System;
using System.IO;
using System.Linq;

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
                    Console.WriteLine("BIG File loaded");
                    //GlobalHeader globalHeader = GlobalHeader.FromBytes(data);
                    BigArchive big = BigArchive.FromBytes(data, Console.Write);
                    Console.WriteLine("BIG File parsed");

                    //for testing only
                    //big.WriteBigFile("test.bin", Console.Write);

                    string outputPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(fileName));
                    if (Directory.Exists(outputPath))
                        Console.WriteLine("Warning: output path already exists! Files may be overwritten.");
                    big.WriteAllContents(outputPath, Console.Write);
                    Console.WriteLine("BIG contents saved");                    

                    Console.WriteLine("Done extracting " + fileName);
                }
                else if (args[0].Equals("pack", StringComparison.OrdinalIgnoreCase))
                {
                    BigArchive big = new BigArchive();

                    int modeIndex = Array.FindIndex(args, s => s.Equals("-m", StringComparison.OrdinalIgnoreCase));
                    if (modeIndex >= 0)
                    {
                        string mode = args[modeIndex + 1];
                        if(mode.Equals("generals", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] generalsFolders = new string[] { "Art", "Data", "Maps", "Window"};
                            Console.WriteLine("Packing " + string.Join(',', generalsFolders));
                            foreach(var folder in generalsFolders)
                            {
                                if (Directory.Exists(folder))
                                {
                                    Console.WriteLine("Packing " + folder);
                                    big.AddDirectory(folder, true, folder, Console.Write);
                                }
                                else
                                {
                                    Console.WriteLine("Skipped " + folder + " because it doesn't exist");
                                }
                            }
                        }
                        else
                        {
                            throw new NotSupportedException("Only the \"generals\" mode is supported at this time");
                        }

                    }
                    else if(args.Contains("-i", StringComparer.OrdinalIgnoreCase))
                    {
                        throw new NotImplementedException("-i option is not yet implemented");
                    }
                    else
                    {
                        throw new NotSupportedException($"Either a mode or input paths must be specified for the pack option");
                    }

                    string fileName = args[args.Length - 1];

                    big.WriteBigFile(fileName, Console.Write);
                    Console.WriteLine("Done packing " + fileName);

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
