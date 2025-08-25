using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace MyWeatherAgent
{
    public class AuthHelper
    {
        private readonly DataverseSettings _settings;

        public AuthHelper(IOptions<DataverseSettings> options)
        {
            _settings = options.Value;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var app = ConfidentialClientApplicationBuilder.Create(_settings.ClientId)
                .WithClientSecret(_settings.ClientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{_settings.TenantId}"))
                .Build();

            var scopes = new[] { $"{_settings.DataverseUrl}/.default" };

            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }

        public string GetDataverseUrl() => _settings.DataverseUrl;
    }
    public class DataverseSettings
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string DataverseUrl { get; set; }
    }



}
