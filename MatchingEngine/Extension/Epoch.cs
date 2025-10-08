using System.Globalization;

namespace MatchingEngine.Extension;

public interface ITimeCounter
{
    int GetTimeNowInSeconds();

    long GetTimeNowInMiliseconds();
}

public class Epoch : ITimeCounter
{
    private static readonly string[] _dtFormats = [
            "ddMMyyyyHHmmss", "dd/MM/yyyy HH:mm:ss", "dd/MM/yy HH:mm:ss",
            "HHmmssddMMyyyy", "HH:mm:ss dd/MM/yyyy", "HH:mm:ss dd/MM/yy",
            "ddMMyyyy", "dd/MM/yyyy", "dd/MM/yy", "HH:mm dd/MM/yyyy", "HH:mm dd/MM/yy"
        ];
    public static readonly DateTime Zero = new DateTime(1970, 1, 1, 0, 0, 0).ToUniversalTime();

    public static int Now => TimestampInSeconds(DateTime.Now);

    public static long MsNow => TimestampInMiliSeconds(DateTime.Now);

    public static int TimestampInSeconds(DateTime? date)
        => date == null ? 0 : (int)date.Value.ToUniversalTime().Subtract(Zero).TotalSeconds;

    public static long TimestampInMiliSeconds(DateTime? date)
        => date == null ? 0 : (long)date.Value.ToUniversalTime().Subtract(Zero).TotalMilliseconds;

    public static DateTime FromTimestamp(int? value)
        => value == null ? Zero : Zero.AddSeconds(value.Value).ToLocalTime();

    public static DateTime FromTimestamp(long? value)
        => value == null ? Zero : Zero.AddMilliseconds(value.Value).ToLocalTime();

    public int GetTimeNowInSeconds()
        => TimestampInSeconds(DateTime.Now);

    public long GetTimeNowInMiliseconds()
        => TimestampInMiliSeconds(DateTime.Now);

    public static DateTime? FromString(string? value)
    {
        if (long.TryParse(value, out long stamp)) return FromTimestamp(stamp);
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt)) return dt;
        return DateTime.TryParseExact(value, _dtFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) ? date : null;
    }

    public static int FixMinute(DateTime? date = null)
    {
        var d = date ?? DateTime.Now;
        d = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
        return TimestampInSeconds(d);
    }

    public static int FixHour(DateTime? date = null)
    {
        var d = date ?? DateTime.Now;
        d = new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0);
        return TimestampInSeconds(d);
    }

    public static int FixDate(DateTime? date = null)
    {
        var d = (date ?? DateTime.Now).Date;
        return TimestampInSeconds(d);
    }
}
