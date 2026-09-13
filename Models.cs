using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Dwg2Pdf;

/// <summary>转换队列中的一个文件。</summary>
public sealed class FileItem : INotifyPropertyChanged
{
    public string Path { get; }

    public string Name => System.IO.Path.GetFileName(Path);

    public string Size { get; }

    private string _status = "待转换";

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _message;

    /// <summary>附加说明(如输出路径、失败原因), 显示为悬停提示。</summary>
    public string? Message
    {
        get => _message;
        set
        {
            if (_message != value)
            {
                _message = value;
                OnPropertyChanged();
            }
        }
    }

    public FileItem(string path)
    {
        Path = path;
        var fi = new FileInfo(path);
        Size = fi.Exists ? FormatSize(fi.Length) : "-";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:0.0} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:0.0} KB";
        return $"{bytes} B";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
