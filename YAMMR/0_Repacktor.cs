using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using YAMMR.Helpers;
using static YAMMR.Helpers.Converters;

namespace YAMMR;

internal class Repacktor
{
    public static async Task RepackHub(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            var files = dirInfo.GetFiles().ToList();

            var filePath =
                path[..path.LastIndexOf("_", StringComparison.Ordinal)]
                + (path.Contains("_dtt") ? ".dtt" : ".dat");

            await using var fileStream = new FileStream(filePath, FileMode.Create);

            var hashData = await Task.Run(() => GenerateHash(files));

            var headerResult = await Task.Run(() => WriteHeader(files, hashData, fileStream));

            await WriteFiles(files, fileStream, headerResult);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to repack files!\n\n" + ex.Message, "Error!");
        }
    }

    private static HashDataContainer GenerateHash(IReadOnlyCollection<FileInfo> files)
    {
        /* Summary and interpretation of:
        GrojBro => https://discord.com/channels/457656329235070981/457657370416513026/750803087631056906
        KalBro => https://github.com/Kerilk/bayonetta_tools
        RiderBro => https://github.com/ArthurHeitmann/NierDocs/blob/master/tools/datRepacker/datRepacker.py
        WoolfBro => https://github.com/WoefulWolf/NieR2Blender2NieR/blob/master/dat_dtt/exporter/datHashGenerator.py
        */

        var shift = Math.Min(31, 32 - IntLength(files.Count));
        var bucketSize = 1 << (31 - shift);
        var bucketTable = Enumerable.Repeat((short)-1, bucketSize).ToList();

        var hashTuple = files
            .Select((t, i) => ((int)(Crc32String(t.Name.ToLower()) & 0x7FFFFFFF), (short)i))
            .ToList();

        for (var i = 0; i < files.Count; i++)
            if (bucketTable[hashTuple[i].Item1 >> shift] == -1)
                bucketTable[hashTuple[i].Item1 >> shift] = (short)i;

        return new HashDataContainer
        {
            Shift = shift,
            Offsets = bucketTable,
            Hashes = hashTuple.Select(x => x.Item1).ToList(),
            Indices = hashTuple.Select(x => x.Item2).ToList(),
            StructSize = 4 + 2 * bucketTable.Count + 4 * hashTuple.Count + 2 * hashTuple.Count
        };
    }

    private static async Task<int> WriteHeader(
        IReadOnlyCollection<FileInfo> files,
        HashDataContainer hashData,
        Stream fileStream
    )
    {
        // Now lets do some quick math
        var longestString = files.Select(x => x.Name).Max(y => y.Length);
        var fileNumber = files.Count;

        var pack = new List<byte>();

        //HEADER
        pack.AddRange(Str2Byte("DAT", -1)); // File format first 4 bytes
        pack.AddRange(Int2Byte(fileNumber)); // Packed files quantity
        pack.AddRange(Int2Byte(32)); // File offsets table offset -> 32 is total header size.
        pack.AddRange(Int2Byte(32 + fileNumber * 4)); // Extensions table offsets
        pack.AddRange(Int2Byte(32 + fileNumber * 8)); // File names table offset
        pack.AddRange(Int2Byte(36 + fileNumber * (9 + longestString))); // File sizes table offset -> Don't forget there are additions 4 bytes for max name length before names table!
        pack.AddRange(Int2Byte(36 + fileNumber * (13 + longestString))); // HashData table offset
        pack.AddRange(Str2Byte(string.Empty, 7 + fileNumber * (13 + longestString))); // Dummy bytes(total tables length minus header), will be filled later.

        //HASHES HEADER
        pack.AddRange(Int2Byte(hashData.Shift)); // PreHashShift
        pack.AddRange(Int2Byte(16)); // Buckets table offset
        pack.AddRange(Int2Byte(16 + hashData.Offsets.Count * 2)); // Hashes table offset
        pack.AddRange(Int2Byte(16 + hashData.Offsets.Count * 2 + hashData.Hashes.Count * 4)); // Indices table offset

        //HASHES TABLES
        pack.AddRange(hashData.Offsets.SelectMany(Short2Byte)); // 2 bytes x offset
        pack.AddRange(hashData.Hashes.SelectMany(Int2Byte)); // 4 bytes x hash
        pack.AddRange(hashData.Indices.SelectMany(Short2Byte)); // 2 bytes x index

        // Writing full pack
        await fileStream.WriteAsync(pack.ToArray().AsMemory(0, pack.Count));

        // Writing longest name length variable;
        fileStream.Seek(32 + fileNumber * 8, SeekOrigin.Begin);
        await fileStream.WriteAsync(Int2Byte(longestString + 1).AsMemory(0, 4));

        return longestString;
    }

    public static async Task WriteFiles(
        List<FileInfo> files,
        FileStream fileStream,
        int nameMaxLength
    )
    {
        fileStream.Seek(0, SeekOrigin.End);

        for (var i = 0; i < files.Count; i++)
        {
            // This block aligns to 16bit, groj says it may be 32 also.
            var overflow = Math.Ceiling((double)fileStream.Length / 16) * 16 - fileStream.Length;
            if (overflow - 1 > 0)
            {
                var dummyBytes = Str2Byte(string.Empty, (int)(overflow - 1));
                await fileStream.WriteAsync(dummyBytes.AsMemory(0, dummyBytes.Length));
            }

            var fileInfo = files[i];

            // File offset
            var position = Int2Byte((int)fileStream.Position);
            fileStream.Seek(32 + i * 4, SeekOrigin.Begin);
            await fileStream.WriteAsync(position.AsMemory(0, 4));

            // Extension
            var fileExtension = Str2Byte(fileInfo.Extension[1..], 3);
            fileStream.Seek(32 + files.Count * 4 + i * 4, SeekOrigin.Begin);
            await fileStream.WriteAsync(fileExtension.AsMemory(0, 4));

            // Name
            var fileName = Str2Byte(fileInfo.Name, nameMaxLength);
            fileStream.Seek(36 + files.Count * 8 + i * (nameMaxLength + 1), SeekOrigin.Begin);
            await fileStream.WriteAsync(fileName.AsMemory(0, nameMaxLength + 1));

            // File Size
            var fileSize = Int2Byte((int)fileInfo.Length);
            fileStream.Seek(36 + files.Count * (9 + nameMaxLength) + i * 4, SeekOrigin.Begin);
            await fileStream.WriteAsync(fileSize.AsMemory(0, 4));

            // File bytes
            fileStream.Seek(0, SeekOrigin.End);
            var (fileBytes, length) = File2Bytes(files[i].FullName);
            await fileStream.WriteAsync(fileBytes.AsMemory(0, length));
        }
    }
}
