namespace Home.Api.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Key { get; set; } = "change-me-to-a-long-secret-key-32chars!";
    public string Issuer { get; set; } = "Home.Api";
    public string Audience { get; set; } = "Home.Web";
    public int ExpireHours { get; set; } = 72;
}
