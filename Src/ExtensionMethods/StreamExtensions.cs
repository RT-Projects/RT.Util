using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Contains extension methods for Stream classes.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Reads all bytes until the end of stream and returns them in a byte array.
        /// </summary>
        public static byte[] ReadAllBytes(this Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }
    }
}
