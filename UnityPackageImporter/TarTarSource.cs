using System;
using System.IO;
using System.Text;

namespace UnityPackageImporter {

    public static class TarTarSource
    {
        public struct TarEntry
        {
            public string Name;
            public byte[] Data;
        }
    
        public static TarEntry ReadTarEntry(Stream fs)
        {
            var entry = new TarEntry();

            // Read 200 bytes for the header
            var header = new byte[0x200];
            var bytesRead = fs.Read(header, 0, 0x200);

            // Get the name and size of the file
            entry.Name = Encoding.ASCII.GetString(header, 0, 100).Replace('\0', ' ').Trim();
            if (string.IsNullOrEmpty(entry.Name))
                return entry;

            // Skip 24 bytes and read 12 for the size string from the header (octal)
            var size = Encoding.ASCII.GetString(header, 124, 12).Replace('\0', ' ').Trim();
            var sizeInt = Convert.ToInt32(size, 8);

            // Round size up to the nearest 512 bytes
            var sizeRounded = (sizeInt + 511) & ~511;

            entry.Data = new byte[sizeRounded];
            bytesRead = fs.Read(entry.Data, 0, sizeRounded);

            return entry;
        }
    }
};