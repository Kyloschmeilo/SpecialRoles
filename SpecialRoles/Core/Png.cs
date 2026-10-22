using System;
using System.IO;
using System.IO.Compression;

namespace SpecialRoles.Core;

/// <summary>
/// Minimaler PNG-Encoder (RGBA, 8 Bit). Die Mod zeichnet ihre Bilder selbst und lädt sie per ImageConversion.LoadImage –
/// das benutzen alle Among-Us-Mods, während Texture2D.SetPixels* im Spiel weggekürzt sein könnte.
/// </summary>
public static class Png
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <param name="rgba">width × height × 4 Bytes, Zeile 0 = oberste Bildzeile.</param>
    public static byte[] Encode(int width, int height, byte[] rgba)
    {
        if (width <= 0 || height <= 0 || rgba.Length != width * height * 4)
            throw new ArgumentException("Bildgröße passt nicht zu den Daten.");

        var raw = new byte[height * (width * 4 + 1)];
        for (var y = 0; y < height; y++)
        {
            var row = y * (width * 4 + 1);
            raw[row] = 0; // Filter: keiner
            Buffer.BlockCopy(rgba, y * width * 4, raw, row + 1, width * 4);
        }

        byte[] compressed;
        using (var buffer = new MemoryStream())
        {
            using (var zlib = new ZLibStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
                zlib.Write(raw, 0, raw.Length);
            compressed = buffer.ToArray();
        }

        using var png = new MemoryStream();
        png.Write(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A });
        var header = new byte[13];
        WriteUInt32(header, 0, (uint)width);
        WriteUInt32(header, 4, (uint)height);
        header[8] = 8; // Bit pro Kanal
        header[9] = 6; // RGBA
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", compressed);
        WriteChunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var length = new byte[4];
        WriteUInt32(length, 0, (uint)data.Length);
        stream.Write(length);
        var typeBytes = new[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
        stream.Write(typeBytes);
        stream.Write(data);
        var crc = 0xFFFFFFFFu;
        crc = Crc(crc, typeBytes);
        crc = Crc(crc, data);
        var crcBytes = new byte[4];
        WriteUInt32(crcBytes, 0, crc ^ 0xFFFFFFFFu);
        stream.Write(crcBytes);
    }

    private static uint Crc(uint crc, byte[] data)
    {
        foreach (var b in data) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }

    private static void WriteUInt32(byte[] target, int offset, uint value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }
}
