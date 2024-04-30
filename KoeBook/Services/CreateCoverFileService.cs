using System.Drawing;
using System.Drawing.Imaging;
using KoeBook.Core;
using KoeBook.Core.Contracts.Services;

namespace KoeBook.Services;

public class CreateCoverFileService : ICreateCoverFileService
{
    public void Create(string title, string author, string coverFilePath)
    {
        try
        {
            // ビットマップの作成
            // サイズはKindleガイドラインの推奨サイズによる
            // https://kdp.amazon.co.jp/ja_JP/help/topic/G6GTK3T3NUHKLEFX
            using var bitmap = new Bitmap(1600, 2560);
            using var graphics = Graphics.FromImage(bitmap);

            // 塗りつぶし
            graphics.FillRectangle(Brushes.PaleGoldenrod, graphics.VisibleClipBounds);

            // フォントの指定
            using var titleFont = new Font("游ゴシック Medium", 125, FontStyle.Bold);
            using var authorFont = new Font("游ゴシック Medium", 75, FontStyle.Bold);

            // 色の指定
            using var brush = Brushes.Black;

            // 表示位置の指定
            using var stringFormat = new StringFormat()
            {
                Alignment = StringAlignment.Center, 
                LineAlignment = StringAlignment.Center
            };
            

            // 文字の入力
            graphics.DrawString(title, titleFont, brush, new Rectangle(0, 0, 1600, 1920), stringFormat);
            graphics.DrawString($"著者: {author}", authorFont, brush, new Rectangle(0, 1920, 1600, 640), stringFormat);

            // png として出力
            bitmap.Save(Path.Combine(coverFilePath, "Cover.png"), ImageFormat.Png);
        }
        catch (Exception ex)
        {
            throw new EbookException(ExceptionType.CreateCoverFileFailed, ex.Message);
        }

    }
}
