using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using MAMMT.Workers;

namespace MAMMT;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Grid_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = e.Data.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();

        foreach (var file in files)
        {
            var wtfIsThis = file.Substring(file.Length - 4, 4);

            switch (wtfIsThis)
            {
                case ".dat":
                case ".dtt":
                    await Task.Run(() => Extractor.UnPack(file));
                    break;

                case "_dat":
                case "_dtt":
                    await Task.Run(() => Repacktor.RepackHub(file));
                    break;

                case ".cpk":
                case "_cpk":
                    await Task.Run(() => Cpk.CpkHub(file, wtfIsThis));
                    break;

                default:
                    MessageBox.Show(
                        $"\"{Path.GetFileName(file)}\" is unsupported file/folder!\n\n"
                            + $"Tool is expecting xxx.dat, xxx.dtt, xxx.cpk files to unpack.\n"
                            + $"xxx_dat, xxx_dtt, xxx_cpk folders to pack.",
                        "Warning!",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        MessageBoxResult.OK,
                        MessageBoxOptions.DefaultDesktopOnly
                    );
                    break;
            }
        }
    }
}
