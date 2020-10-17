using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ClothingMod
{
    public static class MemoryUtilities
    {
        public static void Write<T>(this byte[] bytes, int offset, T value) where T : struct
         => MemoryMarshal.Write(bytes.AsSpan(offset), ref value);

        public static T Read<T>(this ReadOnlySpan<byte> bytes, int offset)
            where T : struct
         => MemoryMarshal.Read<T>(bytes[offset..]);

        public static T Read<T>(this byte[] bytes, int offset)
            where T : struct
         => ((ReadOnlySpan<byte>)bytes).Read<T>(offset);

        internal static byte[] ReadAllBytes(this Stream stream)
        {
            var bytes = new byte[stream.Length];
            using (stream)
            {
                stream.Read(bytes, 0, bytes.Length);
            }
            return bytes;
        }

        internal static IEnumerable<string> ReadAllLines(this Stream stream)
        {
            using var reader = new StreamReader(stream);
            return Enumerable.Repeat(reader, int.MaxValue)
                .Select(x => x.ReadLine())
                .TakeWhile(x => x != null)
                .ToArray();
        }
    }
}
