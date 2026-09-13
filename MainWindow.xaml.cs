using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Dwg2Pdf;

public partial class MainWindow : Window
{
    private static readonly string[] SupportedExt = { ".dwg", ".dxf" };

    private readonly ObservableCollection<FileItem> _items = new();
    private CancellationTokenSource _cts = new();
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        FileList.ItemsSource = _items;
        Settings.Load();
        OutputDirBox.Text = Settings.OutputDir ?? "";
        UpdateEngineStatus();
    }

    // ---------- 拖拽 ----------

    private void OnDragOver(object sender, DragEventArgs e)
    {
        bool valid = e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effects = valid ? DragDropEffects.Copy : DragDropEffects.None;
        if (valid)
        {
            DropZoneFill.Background = new SolidColorBrush(Color.FromRgb(0xDD, 0xEA, 0xFB));
            DropZoneOutline.Stroke = new SolidColorBrush(Color.FromRgb(0x1E, 0x6F, 0xC8));
        }
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        ResetDropZoneVisual();
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        ResetDropZoneVisual();
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            AddPaths(paths);
        e.Handled = true;
    }

    private void ResetDropZoneVisual()
    {
        DropZoneFill.Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
        DropZoneOutline.Stroke = new SolidColorBrush(Color.FromRgb(0x9A, 0xA7, 0xB4));
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        foreach (var raw in paths)
        {
            try
            {
                if (Directory.Exists(raw))
                {
                    foreach (var f in Directory.EnumerateFiles(raw, "*.*", SearchOption.AllDirectories))
                        TryAddFile(f);
                }
                else
                {
                    TryAddFile(raw);
                }
            }
            catch
            {
                // 忽略无法访问的路径
            }
        }
        StatusText.Text = $"已添加 {_items.Count} 个文件";
    }

    private void TryAddFile(string path)
    {
        if (!File.Exists(path)) return;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (!SupportedExt.Contains(ext)) return;
        if (_items.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))) return;
        _items.Add(new FileItem(path));
    }

    // ---------- 点击交互 ----------

    private void DropZone_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => BrowseFiles();

    private void BrowseFiles()
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择 DWG/DXF 文件",
            Multiselect = true,
            Filter = "DWG/DXF 文件|*.dwg;*.dxf|所有文件|*.*"
        };
        if (dlg.ShowDialog(this) == true)
            AddPaths(dlg.FileNames);
    }

    private void BrowseOutputDir_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "选择输出目录" };
        if (dlg.ShowDialog(this) == true)
        {
            OutputDirBox.Text = dlg.FolderName;
            Settings.OutputDir = dlg.FolderName;
            Settings.Save();
        }
    }

    private void SetEngine_Click(object? sender, RoutedEventArgs? e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择 ODAFileConverter.exe",
            Filter = "ODAFileConverter.exe|ODAFileConverter.exe|可执行文件|*.exe"
        };
        if (dlg.ShowDialog(this) == true)
        {
            Settings.EnginePath = dlg.FileName;
            Settings.Save();
            UpdateEngineStatus();
        }
    }

    private void ClearList_Click(object sender, RoutedEventArgs e)
    {
        _items.Clear();
        StatusText.Text = "就绪";
        Progress.Value = 0;
    }

    private void OpenOutput_Click(object sender, RoutedEventArgs e)
    {
        string? dir = null;
        if (!string.IsNullOrWhiteSpace(OutputDirBox.Text))
            dir = OutputDirBox.Text;
        else if (_items.Count > 0)
            dir = Path.GetDirectoryName(_items[0].Path);

        if (string.IsNullOrEmpty(dir)) return;
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
        }
        catch
        {
            // 忽略打开失败
        }
    }

    // ---------- 转换 ----------

    private async void StartConvert_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            _cts.Cancel();
            return;
        }

        var engine = Converter.FindEngine(Settings.EnginePath);
        if (engine == null)
        {
            ShowEngineMissing();
            return;
        }

        var pending = _items.Where(x => x.Status != "完成").ToList();
        if (pending.Count == 0)
        {
            StatusText.Text = "没有待转换的文件";
            return;
        }

        _busy = true;
        _cts = new CancellationTokenSource();
        BtnStart.Content = "取消转换";
        Progress.Maximum = pending.Count;
        Progress.Value = 0;

        try
        {
            await Task.Run(() => RunAllAsync(engine, pending));
        }
        finally
        {
            _busy = false;
            BtnStart.Content = "开始转换";
        }
    }

    private void RunAllAsync(string engine, List<FileItem> pending)
    {
        string? outDir = string.IsNullOrWhiteSpace(OutputDirBox.Text)
            ? null
            : OutputDirBox.Text.Trim();

        int done = 0;
        foreach (var item in pending)
        {
            if (_cts.IsCancellationRequested) break;

            Dispatcher.Invoke(() =>
            {
                item.Status = "转换中…";
                item.Message = null;
            });

            var targetDir = outDir ?? Path.GetDirectoryName(item.Path)!;
            var (ok, msg) = Converter.ConvertFile(engine, item.Path, targetDir);

            Dispatcher.Invoke(() =>
            {
                item.Status = ok ? "完成" : "失败";
                item.Message = ok ? $"输出: {msg}" : $"原因: {msg}";
                done++;
                Progress.Value = done;
                StatusText.Text = ok
                    ? $"[{done}/{pending.Count}] 完成: {Path.GetFileName(msg)}"
                    : $"[{done}/{pending.Count}] 失败: {item.Name}";
            });
        }

        int fail = pending.Count(x => x.Status == "失败");
        int canceled = pending.Count(x => x.Status != "完成" && x.Status != "失败");
        string tail = _cts.IsCancellationRequested && canceled > 0 ? $", 已取消 {canceled} 个" : "";
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = fail == 0
                ? $"全部完成, 共 {pending.Count - canceled} 个文件{tail}"
                : $"完成 {pending.Count - fail - canceled} 个, 失败 {fail} 个{tail}";
            UpdateEngineStatus();
        });
    }

    private void ShowEngineMissing()
    {
        var r = MessageBox.Show(this,
            "未找到转换引擎 ODAFileConverter.exe(免费, 由 Open Design Alliance 提供)。\n\n" +
            "解决方式:\n" +
            "1. 点击“是”打开官网下载页(免费注册后即可下载);\n" +
            "2. 把解压后的 ODAFileConverter 文件夹放到本程序同目录, 或点击“否”手动指定 exe 位置。\n",
            "缺少转换引擎",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if (r == MessageBoxResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo(Converter.OdaDownloadUrl) { UseShellExecute = true });
            }
            catch
            {
                // 忽略
            }
        }
        else if (r == MessageBoxResult.No)
        {
            SetEngine_Click(null, null);
        }
    }

    private void UpdateEngineStatus()
    {
        var engine = Converter.FindEngine(Settings.EnginePath);
        EngineStatus.Text = engine != null
            ? "转换引擎: " + engine
            : "转换引擎: 未找到, 转换前请下载 ODAFileConverter(免费) 或点击“设置引擎…”指定位置";
    }
}
