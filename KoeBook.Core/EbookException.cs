using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using FastEnumUtility;

namespace KoeBook.Core;

public class EbookException : Exception
{
#pragma warning disable CS8764 // 戻り値の型の NULL 値の許容が、オーバーライドされたメンバーと一致しません。おそらく、NULL 値の許容の属性が原因です。
    public override string? Message { get; }
#pragma warning restore CS8764 // 戻り値の型の NULL 値の許容が、オーバーライドされたメンバーと一致しません。おそらく、NULL 値の許容の属性が原因です。

    public ExceptionType ExceptionType { get; }

    public EbookException(ExceptionType exceptionType, string? message = null, Exception? innerException = null) : base(null, innerException)
    {
        Message = message ?? exceptionType.GetEnumMemberValue();
        ExceptionType = exceptionType;
    }

    [DoesNotReturn]
    public static void Throw(ExceptionType exceptionType, string? message = null, Exception? innerException = null)
    {
        throw new EbookException(exceptionType, message, innerException);
    }
}

public enum ExceptionType
{
    [EnumMember(Value = "Epubの生成に失敗しました")]
    EpubCreateError,

    [EnumMember(Value = "初期化に失敗しました")]
    InitializeFailed,

    [EnumMember(Value = "初期化が未完了です")]
    DoesNotInitialized,

    [EnumMember(Value = "音声生成に失敗しました")]
    SoundGenerationFailed,

    [EnumMember(Value = "有効な Style Bert VITS のAPIルートを設定してください")]
    UnknownStyleBertVitsRoot,

    [EnumMember(Value = "GPT4による話者・スタイル設定に失敗しました")]
    Gpt4TalkerAndStyleSettingFailed,

    [EnumMember(Value = "webページの解析に失敗しました")]
    WebScrapingFailed,

    [EnumMember(Value = "小説情報の取得に失敗しました")]
    NarouApiFailed,

    [EnumMember(Value = "Webページが予期しない構造です")]
    UnexpectedStructure,

    [EnumMember(Value = "HTTPリクエストエラー")]
    HttpResponseError,

    /// <summary>
    /// 無効なURLです
    /// </summary>
    [EnumMember(Value = "無効なURLです")]
    InvalidUrl,

    [EnumMember(Value = "不正なXMLです")]
    InvalidXml,
}
