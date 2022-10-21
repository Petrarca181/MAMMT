using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using CriCpkMaker;
using static MAMMT.Mwvm;

namespace MAMMT.Workers;

internal class Cpk
{
    public static async Task CpkHub(string file, string wtfIsThis)
    {
        var cpkMaker = new CpkMaker();

        try
        {
            var result = wtfIsThis == ".cpk" ? Unpack(file, cpkMaker) : Pack(file, cpkMaker);
            if (!result)
                return;

            var progress = (double)0;
            var state = cpkMaker.Execute();
            MainWpf!.Caption = wtfIsThis == ".cpk" ? "Unpacking..." : "Packing...";

            while (state > Status.Stop && progress < 100)
            {
                progress = Math.Round(cpkMaker.GetProgress());
                MainWpf!.Progress = $"Current progress {progress}%";
                state = cpkMaker.Execute();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error!");
        }
        finally
        {
            MainWpf!.Caption = "Done!";
            await Task.Delay(2000);
            MainWpf!.Caption = "Ready!";
            MainWpf!.Progress = string.Empty;

            cpkMaker.Dispose();
        }
    }

    public static bool Unpack(string filePath, CpkMaker cpkMaker)
    {
        try
        {
            cpkMaker.AnalyzeCpkFile(filePath);

            if (!cpkMaker.AnalyzeCpkFile(filePath))
            {
                MessageBox.Show(
                    $"Unsupported file {Path.GetFileName(filePath)}, skipping",
                    "Error!"
                );
                return false;
            }

            var outDirName = Regex.Replace(Path.GetFullPath(filePath), ".([^\\.]+)$", "_$1");
            Directory.CreateDirectory(outDirName);
            cpkMaker.StartToExtract(outDirName);

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error!");
            return false;
        }
    }

    public static bool Pack(string dirPath, CpkMaker cpkMaker)
    {
        try
        {
            cpkMaker.CpkFileMode = CpkMaker.EnumCpkFileMode.ModeFilename;
            cpkMaker.CompressCodec = EnumCompressCodec.CodecLayla;
            cpkMaker.EnableCrc = true;

            if (Directory.GetFiles(dirPath).Length == 0)
                File.Create(dirPath + "\\pizza_con_pepperoni").Close();

            var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);

            var result = MessageBox.Show(
                "Compress files?\n"
                    + "Warning! Compression will be much higher than original one, also time to packing files will be longer",
                "Compression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation
            );

            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var inCpkPath = file.Replace(dirPath + "\\", "");
                cpkMaker.AddFile(
                    file,
                    inCpkPath,
                    (uint)i,
                    result == MessageBoxResult.Yes,
                    "",
                    "",
                    2048
                );
            }

            var cpkPath = dirPath[..dirPath.LastIndexOf("_", StringComparison.Ordinal)] + ".cpk";

            File.Create(cpkPath).Close();
            cpkMaker.StartToBuild(cpkPath);

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error!");
            return false;
        }
    }
}
