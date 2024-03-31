namespace KoeBook.Core.Utilities;

public static class UriOptions
{
    /// <summary>
    /// 正規化を行わないでUriを作成します
    /// </summary>
    public static UriCreationOptions RawUri => new() { DangerousDisablePathAndQueryCanonicalization = true };
}
