using Microsoft.AspNetCore.Mvc;

namespace GuiasBackend.Configuration
{
    public static class CacheProfiles
    {
        public static IEnumerable<KeyValuePair<string, CacheProfile>> GetProfiles()
        {
            yield return new KeyValuePair<string, CacheProfile>(
                "Default",
                new CacheProfile
                {
                    Duration = 60,
                    Location = ResponseCacheLocation.Any
                }
            );

            yield return new KeyValuePair<string, CacheProfile>(
                "Static",
                new CacheProfile
                {
                    Duration = 86400, // 24 horas
                    Location = ResponseCacheLocation.Any
                }
            );

            yield return new KeyValuePair<string, CacheProfile>(
                "NoCache",
                new CacheProfile
                {
                    NoStore = true,
                    Location = ResponseCacheLocation.None
                }
            );
        }
    }
} 