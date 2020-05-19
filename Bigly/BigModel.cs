using System;
using System.Collections.Generic;
using System.Text;

namespace Bigly
{
    //big shout-out to https://gist.github.com/EmilHernvall/953967 which documents the format

    class GlobalHeader
    {
        public string Header { get; set; } = "BIGF";
        public uint FileSize { get; set; }
        public uint NumFiles { get; set; }
        public uint IndexTableSize { get; set; }

        public GlobalHeader()
        {

        }

        public GlobalHeader FromBytes(byte[] bytes)
        {
            GlobalHeader gh = new GlobalHeader();

            gh.Header = Encoding.ASCII.GetString(bytes, 0, 4); //probably actually ANSI but that's not supported on .NET Core
            gh.FileSize = BitConverter.ToUInt32(bytes, 4);
            //gh.NumFiles = BitConverter.ToUInt32()

            return gh;
        }
        
    }

    class FileIndex
    {

    }

    class FileIndexEntry
    {

    }
}
