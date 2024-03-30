using System.Text.Json;

namespace KoeBook.Core.Utilities;

public static class JsonOptions
{
    public static JsonSerializerOptions Default => JsonSerializerOptions.Default;

    public static JsonSerializerOptions Web { get; } = new JsonSerializerOptions(JsonSerializerDefaults.Web);
}
