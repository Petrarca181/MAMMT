using System;
using System.IO;
using System.Linq;
using System.Text;

namespace YAMMR.Helpers;

public static class Converters
{
    public static int Byte2Int(byte[] bytes)
    {
        return BitConverter.ToInt32(bytes);
    }

    public static string Byte2String(byte[] bytes)
    {
        return Encoding.ASCII.GetString(bytes).Replace("\0", string.Empty);
    }

    public static string Byte2NullString(byte[] bytes)
    {
        return Encoding.ASCII.GetString(bytes).Replace("\0", "_null_");
    }

    public static int IntLength(int x)
    {
        return x == 0 ? 1 : (int)Math.Log(x, 2) + 1;
    }

    public static uint Crc32String(string text)
    {
        var arrayOfBytes = Encoding.ASCII.GetBytes(text);
        var crc32 = new Crc32();
        return crc32.Get(arrayOfBytes);
    }

    public static byte[] Str2Byte(string text, int minLength)
    {
        if (minLength <= -1)
            return Encoding.UTF8.GetBytes(text + "\0");

        while (text.Length < minLength)
            text += "\0";

        return Encoding.UTF8.GetBytes(text + "\0");
    }

    public static byte[] Int2Byte(int value)
    {
        return BitConverter.IsLittleEndian
            ? BitConverter.GetBytes(value)
            : BitConverter.GetBytes(value).Reverse().ToArray();
    }

    public static byte[] Short2Byte(short value)
    {
        return BitConverter.IsLittleEndian
            ? BitConverter.GetBytes(value)
            : BitConverter.GetBytes(value).Reverse().ToArray();
    }

    public static (byte[] fileBytes, int Length) File2Bytes(string pathToFile)
    {
        FileStream fs = new(pathToFile, FileMode.Open);
        BinaryReader br = new(fs);
        var fileBytes = br.ReadBytes((int)fs.Length);
        fs.Close();
        br.Close();
        return (fileBytes, fileBytes.Length);
    }
}