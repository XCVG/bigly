using EndianBitConverter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bigly
{
    //big shout-out to https://gist.github.com/EmilHernvall/953967 which documents the format, also https://chipgw.com/2014/12/19/the-big-file-format/

    public class BigArchive
    {
        public GlobalHeader GlobalHeader { get; private set; } = new GlobalHeader();
        public Dictionary<string, byte[]> Files { get; private set; } = new Dictionary<string, byte[]>();

        public void WriteAllContents(string basePath, Action<string> writeLog = null)
        {
            if (writeLog == null)
                writeLog = NullLogger.Write;

            writeLog("Writing files");
            foreach (var kvp in Files)
            { 
                string writeFileName = Path.Combine(basePath, kvp.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(writeFileName));
                File.WriteAllBytes(writeFileName, kvp.Value);
                writeLog(".");
            }

            writeLog("\n");
        }

        public static BigArchive FromBytes(byte[] data, Action<string> writeLog = null)
        {
            if (writeLog == null)
                writeLog = NullLogger.Write;

            BigArchive bf = new BigArchive();

            //load global header
            bf.GlobalHeader = GlobalHeader.FromBytes(data);
            writeLog("header loaded...");

            //load the index
            FileIndex fi = FileIndex.FromBytes(data, bf.GlobalHeader);
            writeLog("index loaded...");

            //there's some junk after the index: "L225" or "L231" plus 4 or 5 bytes of padding
            //Music.big also has some weird junk in between the end of the index and the mystery junk, but I'm not sure if it's intended or not
            //we can probably ignore that when loading but not when saving
            //we will probably just try "L231" plus four bytes at least initially

            //load files from index
            writeLog("\nloading files");
            foreach (var indexEntry in fi.Entries)
            {
                byte[] fileData = data[(int)indexEntry.DataPosition..(int)(indexEntry.DataPosition + indexEntry.DataSize)];
                bf.Files.Add(indexEntry.FileName, fileData);
                writeLog(".");
            }
            writeLog("\n");

            return bf;
        }

        //temporary object; index of files
        private class FileIndex
        {
            public List<FileIndexEntry> Entries { get; private set; } = new List<FileIndexEntry>();

            public static FileIndex FromBytes(byte[] data, GlobalHeader header)
            {
                FileIndex fi = new FileIndex();

                byte[] indexData = data[16..(int)header.HeaderLastIndex];

                //File.WriteAllBytes("idx2.bin", indexData); //for testing

                //now it's time to loop through and grab all the file header data!
                for(int i = 0; i < indexData.Length;)
                {
                    FileIndexEntry fie = new FileIndexEntry();

                    fie.DataPosition = EndianBitConverter.EndianBitConverter.BigEndian.ToUInt32(indexData, i);
                    fie.DataSize = EndianBitConverter.EndianBitConverter.BigEndian.ToUInt32(indexData, i + 4);
                    fie.FileName = CStringConverter.ToString(indexData, i + 8, out int lastIndex);
                    i = lastIndex + 1;

                    fi.Entries.Add(fie);

                    if (indexData.Length - i <= 8) //we can't possibly have another entry
                        break;
                }

                return fi;
            }

        }

        private class FileIndexEntry
        {
            public uint DataPosition { get; set; }
            public uint DataSize { get; set; }
            public string FileName { get; set; }
        }


    }

    public class GlobalHeader
    {
        public string Header { get; set; } = "BIGF";
        public uint FileSize { get; set; }
        public uint NumFiles { get; set; }
        public uint HeaderLastIndex { get; set; } //actually not a length at all

        public GlobalHeader()
        {

        }

        public static GlobalHeader FromBytes(byte[] bytes)
        {
            GlobalHeader gh = new GlobalHeader();

            gh.Header = Encoding.ASCII.GetString(bytes, 0, 4); //probably actually ANSI but that's not supported on .NET Core
            gh.FileSize = EndianBitConverter.EndianBitConverter.LittleEndian.ToUInt32(bytes, 4);
            gh.NumFiles = EndianBitConverter.EndianBitConverter.BigEndian.ToUInt32(bytes, 8); //yes, really
            gh.HeaderLastIndex = EndianBitConverter.EndianBitConverter.BigEndian.ToUInt32(bytes, 12);

            return gh;
        }
        
    }

    internal class NullLogger
    {
        public static void Write(string text)
        {
            //nop
        }
    }

}
