namespace STL.Models.Auth;

public static class AuthConstants
{
    public const string RoleLevelClaim = "role_level";

    public static class Policies
    {
        public const string User = "UserLevel";
        public const string Manager = "ManagerLevel";
        public const string Admin = "AdminLevel";
    }
}
