using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using MAMMT.Helpers;
using static MAMMT.Helpers.Converters;

namespace MAMMT.Workers;

public static class Extractor
{
    public static async void UnPack(string filepath)
    {
        Mwvm.MainWpf!.Caption = "Unpacking files...";
        FileStream fs = null!;
        BinaryReader br = null!;
        try
        {
            fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            br = new BinaryReader(fs);

            if (Byte2String(br.ReadBytes(4)) != "DAT")
            {
                MessageBox.Show(
                    Path.GetFileName(filepath) + " is not supported, skipping...",
                    "Error!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly
                );
                return;
            }

            var files = GetFiles(br);
            await WriteFiles(files, br, filepath);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to unpack files!\n\n" + ex.Message, "Error!");
        }
        finally
        {
            br.Dispose();
            br.Close();
            await fs.DisposeAsync();
            fs.Close();
            Mwvm.MainWpf!.Caption = "Done!";
            await Task.Delay(2000);
            Mwvm.MainWpf!.Caption = "Ready!";
            Mwvm.MainWpf!.Progress = string.Empty;
        }
    }

    private static List<PackedFileInfo> GetFiles(BinaryReader br)
    {
        // Reading header
        var fileCount = Byte2Int(br.ReadBytes(4));
        var fileTableOffset = Byte2Int(br.ReadBytes(4));
        var extensionTableOffset = Byte2Int(br.ReadBytes(4));
        var nameTableOffset = Byte2Int(br.ReadBytes(4));
        var sizeTableOffset = Byte2Int(br.ReadBytes(4));
        var hashMapOffset = Byte2Int(br.ReadBytes(4));

        var files = new List<PackedFileInfo>();

        // Reading whole tables
        br.BaseStream.Seek(extensionTableOffset, SeekOrigin.Begin);
        var tempExtensions = br.ReadBytes(fileCount * 4);

        br.BaseStream.Seek(nameTableOffset + 4, SeekOrigin.Begin);
        var tempFileNames = Byte2NullString(br.ReadBytes(sizeTableOffset - nameTableOffset))
            .Split("_null_")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        br.BaseStream.Seek(fileTableOffset, SeekOrigin.Begin);
        var tempFileOffsets = br.ReadBytes(extensionTableOffset - fileTableOffset);

        br.BaseStream.Seek(sizeTableOffset, SeekOrigin.Begin);
        var tempSizeTableOffset = br.ReadBytes(hashMapOffset - sizeTableOffset);

        // Splitting Info for each file
        for (var i = 0; i < fileCount; i++)
        {
            var file = new PackedFileInfo
            {
                Name = tempFileNames[i],
                Extension = Byte2String(tempExtensions.Skip(i * 4).Take(4).ToArray()),
                Offset = Byte2Int(tempFileOffsets.Skip(i * 4).Take(4).ToArray()),
                Size = Byte2Int(tempSizeTableOffset.Skip(i * 4).Take(4).ToArray())
            };

            files.Add(file);
        }

        return files;
    }

    private static async Task WriteFiles(
        List<PackedFileInfo> files,
        BinaryReader br,
        string filepath
    )
    {
        var dirName = Regex.Replace(Path.GetFullPath(filepath), ".([^\\.]+)$", "_$1") + "\\";
        Directory.CreateDirectory(dirName);
        await File.WriteAllLinesAsync(dirName + "\\mamma.mia", files.Select(x => x.Name)!);

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            br.BaseStream.Seek(file.Offset, SeekOrigin.Begin);
            var fileContent = br.ReadBytes(file.Size);

            FileStream fs = new(dirName + file.Name, FileMode.Create);
            await fs.WriteAsync(fileContent);
            fs.Close();

            Mwvm.MainWpf!.Progress = $"Current progress {i+1}/{files.Count}";
        }
    }
}
