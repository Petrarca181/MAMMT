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
            switch (file.Substring(file.Length-4,4))
            {
                case ".dat":
                    await Task.Run(() => Extractor.UnPack(file));
                    break;
                case ".dtt":
                    await Task.Run(() => Extractor.UnPack(file));
                    break;
                case ".cpk":
                    CPK.Unpack(file);
                    break;
                case "_dat":
                    await Repacktor.RepackHub(file);
                    break;
                case "_dtt":
                    await Repacktor.RepackHub(file);
                    break;
                case "_cpk":
                    break;
            }

       
    }
}
