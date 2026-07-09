namespace STL.SharedServices.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int AbsoluteExpirationMinutes { get; set; } = 10;

}
