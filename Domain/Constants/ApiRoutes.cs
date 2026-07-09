namespace STL.Constants;

public static class ApiRoutes
{
    private const string Base = "/api";

    public static class Auth
    {
        public const string Login = Base + "/auth/login";
        public const string Logout = Base + "/auth/logout";
        public const string Refresh = Base + "/auth/refresh";
        public const string Register = Base + "/auth/register";
    }

    public static class Category
    {
        public const string Create = Base + "/category";
        public const string GetList = Base + "/categories";
        public const string Detail = Base + "/category/{id}";
        public const string Update = Base + "/category/{id}";
        public const string Delete = Base + "/category/{id}";
    }

    public static class Product
    {
        public const string Create = Base + "/products";
        public const string GetList = Base + "/products";
        public const string Detail = Base + "/product/{id}";
        public const string YouMayLike = Base + "/products/{id}/you-may-like";
        public const string OthersAlsoBought = Base + "/products/{id}/others-also-bought";
        public const string Update = Base + "/products/{id}";
        public const string Delete = Base + "/products/{id}";
        public const string Export = Base + "/products/export";
    }

    public static class Recommendation
    {
        public const string TrackProductInteraction = Base + "/recommendations/interactions";
        public const string RebuildFallback = Base + "/recommendations/rebuild-fallback";
    }
}
