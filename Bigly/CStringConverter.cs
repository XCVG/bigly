using System;
using System.Collections.Generic;
using System.Text;

namespace Bigly
{
    class CStringConverter
    {
        //from https://stackoverflow.com/questions/144176/fastest-way-to-convert-a-possibly-null-terminated-ascii-byte-to-a-string
        //we'll probably replace this later
        public static string ToString(byte[] buffer, int offset, out int end)
        {
            end = offset;
            while (end < buffer.Length && buffer[end] != 0)
            {
                end++;
            }
            unsafe
            {
                fixed (byte* pAscii = buffer)
                {
                    return new String((sbyte*)pAscii, offset, end - offset);
                }
            }
        }

        //does the opposite, mostly
        public static byte[] FromString(string str)
        {
            //really stupid
            byte[] bytestr = Encoding.ASCII.GetBytes(str);
            byte[] bytes = new byte[bytestr.Length + 1];
            Array.Copy(bytestr, bytes, bytestr.Length);
            bytes[bytes.Length - 1] = 0;
            return bytes;
        }

    }
}
