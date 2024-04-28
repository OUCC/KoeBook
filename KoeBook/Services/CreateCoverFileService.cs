using System.Drawing;
using System.Drawing.Imaging;
using KoeBook.Core.Contracts.Services;

namespace KoeBook.Services;

public class CreateCoverFileService : ICreateCoverFileService
{
    public bool TryCreate(string title, string author, string coverFilePath)
    {
        try
        {
            // ビットマップの作成
            using Bitmap bitmap = new Bitmap(1800, 2560);
            using Graphics graphics = Graphics.FromImage(bitmap);

            // 塗りつぶし
            graphics.FillRectangle(Brushes.PaleGoldenrod, graphics.VisibleClipBounds);

            // フォントの指定
            using Font titleFont = new Font("ＭＳ ゴシック", 125, FontStyle.Bold);
            using Font authorFont = new Font("ＭＳ ゴシック", 75, FontStyle.Bold);

            // 色の指定
            using Brush brush = new SolidBrush(Color.Black);

            // 表示位置の指定
            using StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            // 文字の入力
            graphics.DrawString(title, titleFont, brush, new Rectangle(0, 0, 1800, 1920), stringFormat);
            graphics.DrawString($"著者: {author}", authorFont, brush, new Rectangle(0, 1920, 1800, 640), stringFormat);

            // png として出力
            bitmap.Save(Path.Combine(coverFilePath, "Cover.png"), ImageFormat.Png);

            return true;
        }
        catch
        {
            return false;
        }
        
    }
}
