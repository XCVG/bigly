using BitConverter;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bigly
{
    //big shout-out to https://gist.github.com/EmilHernvall/953967 which documents the format

    class BigArchive
    {
        public GlobalHeader GlobalHeader { get; private set; } = new GlobalHeader();
        public List<BigFileEntry> Files { get; private set; } = new List<BigFileEntry>();

        public static BigArchive FromBytes(byte[] data)
        {
            BigArchive bf = new BigArchive();

            //load global header
            bf.GlobalHeader = GlobalHeader.FromBytes(data);


            return bf;
        }


        //temporary object; index of files
        private class FileIndex
        {
            public List<FileIndexEntry> Entries { get; private set; } = new List<FileIndexEntry>();

            public static FileIndex FromBytes(byte[] data, GlobalHeader header)
            {
                throw new NotImplementedException();
            }

        }

        private class FileIndexEntry
        {
            public uint DataPosition { get; set; }
            public uint DataSize { get; set; }
            public string FileName { get; set; }
        }


    }

    class BigFileEntry
    {

    }

    class GlobalHeader
    {
        public string Header { get; set; } = "BIGF";
        public uint FileSize { get; set; }
        public uint NumFiles { get; set; }
        public uint IndexTableSize { get; set; }

        public GlobalHeader()
        {

        }

        public static GlobalHeader FromBytes(byte[] bytes)
        {
            GlobalHeader gh = new GlobalHeader();

            gh.Header = Encoding.ASCII.GetString(bytes, 0, 4); //probably actually ANSI but that's not supported on .NET Core
            gh.FileSize = EndianBitConverter.LittleEndian.ToUInt32(bytes, 4);
            gh.NumFiles = EndianBitConverter.BigEndian.ToUInt32(bytes, 8); //yes, really
            gh.IndexTableSize = EndianBitConverter.BigEndian.ToUInt32(bytes, 12);

            return gh;
        }
        
    }

}
