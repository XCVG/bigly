using EndianBitConverter;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Bigly
{
    //big shout-out to https://gist.github.com/EmilHernvall/953967 which documents the format, also https://chipgw.com/2014/12/19/the-big-file-format/

    public class BigArchive
    {
        public GlobalHeader GlobalHeader { get; private set; } = new GlobalHeader(); //will probably remove this, doesn't make sense to keep here
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

        public void WriteBigFile(string path, Action<string> writeLog = null)
        {
            if (writeLog == null)
                writeLog = NullLogger.Write;

            //write out the contents of this BigArchive as a .big file

            //figure out a heuristic and preallocate a big list of bytes
            int contentsTotalBytes = 0;
            foreach(byte[] file in Files.Values)
            {
                contentsTotalBytes += file.Length;
            }

            List<byte> bytes = new List<byte>(contentsTotalBytes + (Files.Count * 64)); //heuristic

            //write an incomplete global header (can write fourcc, numfiles at this point)
            writeLog("Write global header...");
            {
                bytes.OverwriteRange(0, Encoding.ASCII.GetBytes("BIGF"));

                var fileCount = EndianBitConverter.EndianBitConverter.BigEndian.GetBytes((uint)Files.Count);
                bytes.OverwriteRange(8, fileCount);
            }

            List<KeyValuePair<string, byte[]>> fileList = Files.OrderBy(x => x.Key).ToList();
            int[] fileIndexStartIndices = new int[fileList.Count];

            int bytesIndex = 16; //start after the header

            //write an incomplete index (can write file names and sizes at this point)
            writeLog("\nWrite file index");
            for (int i = 0; i < fileList.Count; i++)
            {
                fileIndexStartIndices[i] = bytesIndex;
                bytes.OverwriteRange(bytesIndex, new byte[] { 0, 0, 0, 0 }); //address, unknown
                bytes.OverwriteRange(bytesIndex + 4, EndianBitConverter.EndianBitConverter.BigEndian.GetBytes((uint)fileList[i].Value.Length));
                byte[] nameBytes = CStringConverter.FromString(fileList[i].Key);
                bytes.OverwriteRange(bytesIndex + 8, nameBytes);
                bytesIndex += 4 + 4 + nameBytes.Length; //off-by-one?
                writeLog(".");
            }
            writeLog("\n");

            //write the L231 and four bytes of padding
            writeLog("Write L231 and padding...\n");
            bytes.OverwriteRange(bytesIndex, Encoding.ASCII.GetBytes("L231"));
            bytes.OverwriteRange(bytesIndex + 4, new byte[] { 0, 0, 0, 0 });
            bytesIndex += 8;

            //write HeaderLastIndex to the global header
            writeLog("Update global header...\n");
            uint headerLastIndex = (uint)bytesIndex - 1;
            bytes.OverwriteRange(12, EndianBitConverter.EndianBitConverter.BigEndian.GetBytes(headerLastIndex));

            //File.WriteAllBytes("test.bin", bytes.ToArray()); //was just for testing

            //loop through the index and write each file, writing back its size to the index
            writeLog("Write files");
            for (int i = 0; i < fileList.Count; i++)
            {
                byte[] fileBytes = fileList[i].Value;
                bytes.OverwriteRange(bytesIndex, fileBytes);

                bytes.OverwriteRange(fileIndexStartIndices[i], EndianBitConverter.EndianBitConverter.BigEndian.GetBytes((uint)bytesIndex));

                bytesIndex += fileBytes.Length;
                writeLog(".");
            }
            writeLog("\n");

            //write FileSize to the global header
            writeLog("Update global header...\n");
            bytes.OverwriteRange(4, EndianBitConverter.EndianBitConverter.LittleEndian.GetBytes((uint)bytesIndex));

            //update the stored global header; why do we even store this?
            GlobalHeader.Header = "BIGF";
            GlobalHeader.NumFiles = (uint)fileList.Count;
            GlobalHeader.FileSize = (uint)bytesIndex;
            GlobalHeader.HeaderLastIndex = headerLastIndex;

            //write the file out
            writeLog("File built, writing out to disk!\n");
            File.WriteAllBytes(path, bytes.ToArray());
        }

        public void AddFile(string filePath, string fileName = null, Action<string> writeLog = null)
        {
            if (writeLog == null)
                writeLog = NullLogger.Write;

            if (fileName == null)
                fileName = filePath;

            if(File.Exists(filePath))
            {
                Files.Add(fileName, File.ReadAllBytes(filePath));
            }
            else
            {
                writeLog($"Didn't add {filePath} because it doesn't exist!");
                throw new FileNotFoundException();
            }
        }

        public void AddDirectory(string dirPath, bool recurse, string basePath = null, Action<string> writeLog = null)
        {
            if (writeLog == null)
                writeLog = NullLogger.Write;

            if (basePath == null)
                basePath = dirPath;

            if (!Directory.Exists(dirPath))
                throw new DirectoryNotFoundException();

            var fileList = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories).ToList();

            writeLog("Adding files");
            foreach(string file in fileList)
            {
                //correct the file path name in the archive (probably off-by-one as fuck)
                string fileName = file;
                if (basePath != dirPath && file.StartsWith(dirPath))
                {
                    fileName = file.Substring(dirPath.Length - 1, file.Length - dirPath.Length);
                }

                Files.Add(fileName, File.ReadAllBytes(file));
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

    internal static class ListExtensions
    {
        //this is stupid as fuck because I tried to use a List<T> like a std::vector
        public static void OverwriteRange<T>(this List<T> list, int index, IEnumerable<T> collection)
        {
            foreach(T item in collection)
            {
                if (index > list.Count)
                    extendList();
                else if (index == list.Count)
                    list.Add(default);

                list[index] = item;
                index++;
            }

            void extendList()
            {
                while(list.Count <= index)
                {
                    list.Add(default);
                }
            }
        }

        
    }

}
