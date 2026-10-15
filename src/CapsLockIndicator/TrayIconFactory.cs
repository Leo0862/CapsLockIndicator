using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace CapsLockIndicator;

/// <summary>
/// Draws the tray icon at runtime so no binary icon file is required.
/// ON: white "A" on a filled gray box. OFF: gray "a" on an outlined box.
/// </summary>
internal static class TrayIconFactory
{
    public static Icon Create(bool capsOn)
    {
        int size = SystemInformation.SmallIconSize.Width;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            var rect = new Rectangle(0, 0, size - 1, size - 1);
            var gray = Color.FromArgb(0x50, 0x50, 0x50);
            if (capsOn)
            {
                using var fill = new SolidBrush(gray);
                g.FillRectangle(fill, rect);
            }
            else
            {
                using var pen = new Pen(Color.FromArgb(0x90, 0x90, 0x90), Math.Max(1, size / 16f));
                g.DrawRectangle(pen, rect);
            }

            using var font = new Font("Segoe UI", size * 0.62f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(capsOn ? Color.White : Color.FromArgb(0x90, 0x90, 0x90));
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(capsOn ? "A" : "a", font, textBrush, new RectangleF(0, 0, size, size), format);
        }

        // Clone the icon so the unmanaged HICON can be released immediately.
        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(hIcon);
        }
    }
}
