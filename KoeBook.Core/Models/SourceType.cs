using System.Runtime.Serialization;

namespace KoeBook.Core.Models;

public enum SourceType
{
    [EnumMember(Value = "外部サイト")]
    Url,

    [EnumMember(Value = "ローカルファイル")]
    FilePath,

    [EnumMember(Value = "AI生成")]
    AiStory,
}
