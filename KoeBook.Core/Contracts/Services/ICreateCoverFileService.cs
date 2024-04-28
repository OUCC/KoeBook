using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoeBook.Core.Contracts.Services;

public interface ICreateCoverFileService
{
    /// <summary>
    /// 表紙用の画像を作成
    /// </summary>
    /// <param name="title">作品の題名</param>
    /// <param name="author">作品の著者名</param>
    /// <param name="coverFilePath">表紙の画像を置くフォルダのパス</param>
    /// <returns>成功すれば、true、失敗すれば、false</returns>
    bool TryCreate(string title, string author, string coverFilePath);
}
