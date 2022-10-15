using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using YAMMR;

namespace YAMMR;

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
            if (Directory.Exists(file))
                await Repacktor.RepackHub(file);
            else
                await Task.Run(() => Extractor.UnPack(file));
    }
}
