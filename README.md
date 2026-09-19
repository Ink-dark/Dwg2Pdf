# DWG → A4 PDF 转换器 (Windows)

一个简单易用的 Windows 桌面小工具:**把 .dwg / .dxf 文件拖进窗口,一键转换成 A4 尺寸的 PDF**。
支持批量、支持直接拖入整个文件夹。

## 注意：关于二次分发
请勿将此软件中包含的ODA File Converter进行二次分发！本release中提供安装包仅供学习使用，禁止倒卖或二次转售！

## 特性

- 拖拽即转:把文件拖到窗口松开即可加入队列,支持批量与文件夹
- 输出强制 **A4**:每一页等比缩放、居中绘制到 A4 页面(自动匹配横/纵向,多布局 DWG 的每一页都会转为 A4)
- 输出目录可选:留空则保存到 DWG 所在目录,重名自动加序号不覆盖
- 单文件绿色软件:自包含 .NET 8 运行时,免安装,拷贝即用
- 设置持久化:引擎路径、输出目录自动记住

## 快速开始

**使用 GitHub Actions 构建的产物(推荐)**:产物已内置 ODA 转换引擎,解压 `Dwg2Pdf-win-x64.zip` 直接运行 `Dwg2Pdf.exe` 即可,无需任何手动准备。

**自行构建时**才需要手动准备引擎(DWG 是闭源格式,转换依赖 Open Design Alliance 的免费工具 **ODA File Converter**):

1. 打开 https://www.opendesign.com/guestfiles/oda_file_converter 免费下载 Windows x64 版本并解压
2. 把解压出的 `ODAFileConverter` 文件夹放到 `Dwg2Pdf.exe` 同目录(或安装到默认路径,程序会自动在 `C:\Program Files\ODA\` 下查找;也可在程序里点「设置引擎…」手动指定)
3. 运行 `Dwg2Pdf.exe`,把 `.dwg`/`.dxf` 文件拖入窗口,点「开始转换」即可

> 引擎未就绪时,程序底部会给出提示,点「开始转换」也会弹出指引。

## 使用界面

| 区域 | 说明 |
|---|---|
| 拖放区 | 拖入文件或文件夹,也可点击选择文件 |
| 输出目录 | 留空 = 保存到源 DWG 所在目录;可点「浏览…」指定 |
| 文件列表 | 显示文件名/大小/状态(待转换/转换中/完成/失败),悬停可看输出路径或失败原因 |
| 开始转换 | 批量转换;转换中变为「取消转换」 |
| 打开输出目录 | 用资源管理器打开输出位置 |
| 设置引擎 | 手动指定 ODAFileConverter.exe |

## 构建与 CI

项目使用 WPF + .NET 8,提交到 `main` 后 GitHub Actions 自动构建并发布产物:

1. `dotnet publish -c Release` 产出 `Dwg2Pdf.exe`(win-x64 自包含单文件)
2. 自动下载 ODA File Converter 官方 MSI 并解压,把引擎目录 `ODAFileConverter\` 打包进产物
3. 产物整体上传为 `Dwg2Pdf-win-x64` 构件(解压即用)

本地手动构建:

```bash
dotnet publish -c Release -o publish
```

## 源码结构

```
Dwg2Pdf/
├── Dwg2Pdf.csproj    # WPF 工程(win-x64 自包含单文件)
├── App.xaml(.cs)     # 应用入口
├── MainWindow.xaml   # 主窗口界面(拖放区/列表/进度/按钮)
├── MainWindow.xaml.cs# 交互逻辑:拖拽、队列、进度、引擎查找
├── Converter.cs      # 调用 ODAFileConverter 转 PDF,输出文件去重
├── PdfToA4.cs        # PDFsharp 把每一页重排为 A4(等比缩放+居中)
├── Models.cs         # 队列条目(文件名/大小/状态)
└── Settings.cs       # 设置持久化(%AppData%\Dwg2Pdf\settings.json)
```

## 转换流程

```
.dwg/.dxf ──► ODAFileConverter(免费引擎) ──► 原始 PDF ──► PDFsharp 重排 ──► A4 PDF
```

## 常见问题

- **提示「缺少转换引擎」**:使用 CI 产物不会出现;自行构建时按上文「快速开始」放置/指定引擎即可。
- **某个文件转换失败**:文件可能损坏或 DWG 版本过新,可悬停失败项查看原因;引擎会先自动审核(audit)再转换。
- **中文字体乱码**:取决于图纸内嵌字体,建议图纸使用 TTF 字体。
- **A4 含义**:页面物理尺寸为 A4(210×297mm),原图内容等比缩放完整放入页面。

## 版权与许可

- 本项目源码可自由使用/修改。
- `ODAFileConverter` 版权归 Open Design Alliance 所有,免费下载使用;将引擎随产物分发时请遵守其许可条款。
- PDF 处理使用 [PDFsharp](https://github.com/empira/PDFsharp)(MIT License)。
