namespace CodemotionRome19.Core.Configuration
{
    public interface IConfiguration
    {
        string ClientId { get; }
        string ClientSecret { get; }
        string TenantId { get; }
        string SubscriptionId { get; }
        string AldoClientId { get; }
        string AldoClientSecret { get; }

        string GetValue(string key);
    }
}
