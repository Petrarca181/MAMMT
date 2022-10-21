using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MAMMT;

public class Mwvm : INotifyPropertyChanged
{
    public static Mwvm? MainWpf;

    public Mwvm()
    {
        MainWpf = this;
    }

    private string? _caption = "Ready!";
    public string? Caption
    {
        get => _caption;
        set
        {
            _caption = value;

            OnPropertyChanged(nameof(Caption));
        }
    }

    private string? _progress = string.Empty;
    public string? Progress
    {
        get => _progress;
        set
        {
            _progress = value;

            OnPropertyChanged(nameof(Progress));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
