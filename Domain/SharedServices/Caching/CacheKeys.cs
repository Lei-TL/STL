namespace STL.SharedServices.Caching;

public static class CacheKeys
{
    public const string CategoryListVersion = "categories:list:version";
    public const string ProductListVersion = "products:list:version";

    public static string CategoryList(
        string version,
        int pageNumber,
        int pageSize)
        => $"categories:list:v:{version}:pageNumber:{pageNumber}:pageSize:{pageSize}";

    public static string ProductList(
        string version,
        int pageNumber,
        int pageSize)
        => $"products:list:v:{version}:pageNumber:{pageNumber}:pageSize:{pageSize}";
}
