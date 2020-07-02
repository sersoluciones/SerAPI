using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SerAPI.Resources;

namespace SerAPI.Utils
{
    public class Locales
    {
        private readonly ILogger _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public Locales(ILogger<Locales> logger, IStringLocalizer<SharedResource> localizer)
        {
            _logger = logger;
            _localizer = localizer;
        }

        public string __(string Key, params object[] arguments)
        {
            return _localizer[Key, arguments].Value;
        }
    }
}
