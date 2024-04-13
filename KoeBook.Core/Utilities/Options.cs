using System.Text.Json;

namespace KoeBook.Core.Utilities;

public static class Options
{
    public static JsonSerializerOptions JsonWeb { get; } = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    /// <summary>
    /// 正規化を行わないでUriを作成します
    /// </summary>
    public static UriCreationOptions RawUri => new() { DangerousDisablePathAndQueryCanonicalization = true };
}
