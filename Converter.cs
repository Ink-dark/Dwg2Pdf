using System.Diagnostics;
using System.IO;

namespace Dwg2Pdf;

/// <summary>
/// 调用免费的 ODA File Converter(Open Design Alliance)把 .dwg/.dxf 转成 PDF,
/// 再用 PdfSharp 把每一页重新排版为 A4 尺寸。
/// </summary>
public static class Converter
{
    public const string OdaDownloadUrl = "https://www.opendesign.com/guestfiles/oda_file_converter";

    /// <summary>转换单个文件。成功时返回输出 PDF 的完整路径。</summary>
    public static (bool Ok, string Message) ConvertFile(string enginePath, string dwgPath, string outDir)
    {
        try
        {
            var rawPdf = OdaConvert(enginePath, dwgPath);
            Directory.CreateDirectory(outDir);
            var final = UniquePath(Path.Combine(outDir, Path.GetFileNameWithoutExtension(dwgPath) + ".pdf"));
            PdfToA4.Convert(rawPdf, final);
            return (true, final);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>用 ODA File Converter 把单个 DWG/DXF 转成 PDF, 返回临时 PDF 路径。</summary>
    private static string OdaConvert(string enginePath, string dwgPath)
    {
        var srcDir = Path.GetDirectoryName(dwgPath)
            ?? throw new InvalidOperationException("无法获取源文件目录");
        var tempDir = Path.Combine(Path.GetTempPath(), "Dwg2Pdf", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        // 命令行格式:
        // ODAFileConverter.exe <输入目录> <输出目录> <输出版本> <输出类型> <递归> <审核> [<输入过滤>]
        var psi = new ProcessStartInfo
        {
            FileName = enginePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(enginePath)!,
            Arguments =
                $"\"{srcDir}\" \"{tempDir}\" ACAD2018 PDF 0 1 \"{Path.GetFileName(dwgPath)}\""
        };

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException("无法启动转换引擎");
        // 异步读取输出, 防止管道缓冲区写满导致死锁
        _ = p.StandardOutput.ReadToEndAsync();
        _ = p.StandardError.ReadToEndAsync();

        if (!p.WaitForExit(TimeSpan.FromMinutes(15)))
        {
            try { p.Kill(true); } catch { /* 忽略 */ }
            throw new TimeoutException("转换超时(超过 15 分钟)");
        }

        var pdf = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(dwgPath) + ".pdf");
        if (p.ExitCode != 0 || !File.Exists(pdf))
        {
            throw new InvalidOperationException(
                $"转换引擎执行失败(退出码 {p.ExitCode}); 文件可能已损坏或版本过新");
        }
        return pdf;
    }

    /// <summary>按优先级查找 ODAFileConverter.exe: 设置路径 → 程序同目录 → 常见安装目录。</summary>
    public static string? FindEngine(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            return configured;

        var baseDir = AppContext.BaseDirectory;
        foreach (var cand in new[]
                 {
                     Path.Combine(baseDir, "ODAFileConverter", "ODAFileConverter.exe"),
                     Path.Combine(baseDir, "ODAFileConverter.exe")
                 })
        {
            if (File.Exists(cand)) return cand;
        }

        // 常见安装位置: C:\Program Files\ODA\ODAFileConverter <版本>\ODAFileConverter.exe
        try
        {
            var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var odaRoot = Path.Combine(pf, "ODA");
            if (Directory.Exists(odaRoot))
            {
                foreach (var dir in Directory.GetDirectories(odaRoot))
                {
                    var exe = Path.Combine(dir, "ODAFileConverter.exe");
                    if (File.Exists(exe)) return exe;
                }
            }
        }
        catch
        {
            // 忽略权限问题
        }
        return null;
    }

    private static string UniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (int i = 1; ; i++)
        {
            var cand = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(cand)) return cand;
        }
    }
}
