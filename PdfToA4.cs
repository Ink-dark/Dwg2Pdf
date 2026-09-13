using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Dwg2Pdf;

/// <summary>
/// 把任意尺寸的 PDF 每一页等比缩放、居中绘制到 A4 页面(自动匹配横/纵向)。
/// </summary>
public static class PdfToA4
{
    public static void Convert(string srcPath, string dstPath)
    {
        using var form = XPdfForm.FromFile(srcPath);
        using var doc = new PdfDocument();
        doc.Info.Title = "Converted by Dwg2Pdf (A4)";

        int count = form.PageCount;
        for (int i = 0; i < count; i++)
        {
            form.PageNumber = i + 1; // XPdfForm 页码从 1 开始

            double srcW = form.PointWidth;
            double srcH = form.PointHeight;
            if (srcW <= 0 || srcH <= 0) continue;

            bool landscape = srcW > srcH;
            var page = doc.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = landscape ? PageOrientation.Landscape : PageOrientation.Portrait;

            double a4W = page.Width.Point;
            double a4H = page.Height.Point;

            double scale = Math.Min(a4W / srcW, a4H / srcH);
            double w = srcW * scale;
            double h = srcH * scale;
            double x = (a4W - w) / 2;
            double y = (a4H - h) / 2;

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(form, x, y, w, h);
        }

        doc.Save(dstPath);
    }
}
