public interface IDateTimeProvider
{
    DateTime NowLocal { get; }
}

public class DateTimeProvider : IDateTimeProvider
{
    private readonly TimeZoneInfo _tz;

    public DateTimeProvider()
    {
        try
        {
            _tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch
        {
            _tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }

    public DateTime NowLocal => TimeZoneInfo.ConvertTime(DateTime.UtcNow, _tz);
}
