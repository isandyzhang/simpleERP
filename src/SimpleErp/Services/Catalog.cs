namespace SimpleErp.Services;

public static class Channels
{
    public const string Tcat = "tcat";
    public const string Unimart = "unimart_c2c";

    public static string Label(string channel) => channel switch
    {
        Tcat => "官方契約（黑貓）",
        Unimart => "產生代碼（7-ELEVEN）",
        _ => channel
    };
}

public static class Temps
{
    public const string Ambient = "ambient";
    public const string Chilled = "chilled";
    public const string Frozen = "frozen";

    public static string Label(string temp) => temp switch
    {
        Ambient => "常溫",
        Chilled => "冷藏",
        Frozen => "冷凍",
        _ => temp
    };
}

public static class Clock
{
    public static DateTime UtcNow() => DateTime.UtcNow;

    public static string Local(DateTime utc)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Taipei Standard Time" : "Asia/Taipei");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone)
            .ToString("yyyy-MM-dd HH:mm");
    }
}
