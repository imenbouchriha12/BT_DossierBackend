using Backend.BL.IBL;
using Backend.DAL.IDAL;
using Backend.DTO.Finance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.BL.BL
{
    /// <summary>
    /// Cours de change du jour en temps réel depuis une API publique (open.er-api.com),
    /// avec cache mémoire et repli sur les cours SISS si l'API est indisponible.
    /// </summary>
    public class CoursTempsReelService : ICoursTempsReelService
    {
        private static readonly Dictionary<string, (string Nom, string Symbole)> Devises =
            new()
            {
                ["TND"] = ("Dinar tunisien", "DT"),
                ["EUR"] = ("Euro", "€"),
                ["USD"] = ("Dollar américain", "$"),
                ["GBP"] = ("Livre sterling", "£"),
                ["CHF"] = ("Franc suisse", "CHF"),
                ["CAD"] = ("Dollar canadien", "C$"),
                ["AUD"] = ("Dollar australien", "A$"),
                ["JPY"] = ("Yen japonais", "¥"),
                ["SAR"] = ("Riyal saoudien", "SAR"),
                ["AED"] = ("Dirham des Émirats", "AED"),
                ["KWD"] = ("Dinar koweïtien", "KWD"),
                ["CNY"] = ("Yuan chinois", "CN¥"),
                ["MAD"] = ("Dirham marocain", "MAD"),
                ["EGP"] = ("Livre égyptienne", "EGP"),
                ["SEK"] = ("Couronne suédoise", "kr"),
                ["NOK"] = ("Couronne norvégienne", "kr"),
                ["DKK"] = ("Couronne danoise", "kr")
            };

        // Devises affichées, dans un ordre logique (TND en référence)
        private static readonly string[] DevisesAffichees =
            { "EUR", "USD", "GBP", "CHF", "CAD", "AUD", "JPY", "SAR", "AED", "KWD", "CNY", "MAD" };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ICoursRepository _coursRepository;
        private readonly ILogger<CoursTempsReelService> _logger;

        private readonly object _cacheLock = new();
        private List<CoursTempsReelDto>? _cache;
        private DateTime _cacheDate = DateTime.MinValue;

        public CoursTempsReelService(
            HttpClient httpClient,
            IConfiguration configuration,
            ICoursRepository coursRepository,
            ILogger<CoursTempsReelService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _coursRepository = coursRepository;
            _logger = logger;
        }

        public async Task<List<CoursTempsReelDto>> GetCoursDuJourAsync()
        {
            lock (_cacheLock)
            {
                var cacheMinutes = int.TryParse(_configuration["CoursTempsReel:CacheMinutes"], out var m) ? m : 360;
                if (_cache is not null && DateTime.UtcNow - _cacheDate < TimeSpan.FromMinutes(cacheMinutes))
                    return _cache;
            }

            var cours = await FetchTempsReelAsync()
                ?? await FetchRepliSissAsync();

            if (cours is null || cours.Count == 0)
                return new List<CoursTempsReelDto>();

            lock (_cacheLock)
            {
                _cache = cours;
                _cacheDate = DateTime.UtcNow;
            }

            return cours;
        }

        private async Task<List<CoursTempsReelDto>?> FetchTempsReelAsync()
        {
            try
            {
                var url = _configuration["CoursTempsReel:ApiUrl"]
                          ?? "https://open.er-api.com/v6/latest/TND";
                var timeout = TimeSpan.FromSeconds(int.TryParse(_configuration["CoursTempsReel:TimeoutSeconds"], out var t) ? t : 15);

                using var cts = new CancellationTokenSource(timeout);
                var json = await _httpClient.GetStringAsync(url, cts.Token);
                var response = JsonSerializer.Deserialize<ErApiResponse>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (response?.Rates is null || response.Result != "success")
                {
                    _logger.LogWarning("API de change indisponible (result={Result}).", response?.Result);
                    return null;
                }

                var dateSource = DateTime.TryParse(response.TimeLastUpdateUtc,
                    CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var d) ? d : (DateTime?)DateTime.UtcNow;

                var result = new List<CoursTempsReelDto>();
                foreach (var code in DevisesAffichees)
                {
                    if (code == "TND")
                        continue;

                    if (!response.Rates.TryGetValue(code, out var taux) || taux <= 0)
                        continue;

                    var deviseInfo = Devises[code];
                    result.Add(new CoursTempsReelDto
                    {
                        Devise = code,
                        Nom = deviseInfo.Nom,
                        Symbole = deviseInfo.Symbole,
                        Valeur = Math.Round(1m / (decimal)taux, 4),
                        Date = dateSource
                    });
                }

                result.Add(new CoursTempsReelDto
                {
                    Devise = "TND",
                    Nom = Devises["TND"].Nom,
                    Symbole = Devises["TND"].Symbole,
                    Valeur = 1m,
                    Date = dateSource
                });

                return result;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Échec de l'appel à l'API de change temps réel.");
                return null;
            }
        }

        /// <summary>
        /// Repli : derniers cours SISS disponibles en base.
        /// </summary>
        private async Task<List<CoursTempsReelDto>> FetchRepliSissAsync()
        {
            try
            {
                var cours = await _coursRepository.GetLatestAsync(null);
                var result = new List<CoursTempsReelDto>();

                foreach (var c in cours)
                {
                    var devise = c.Devise.Trim();
                    if (devise == "ZZ" || !Devises.ContainsKey(devise))
                        continue;

                    result.Add(new CoursTempsReelDto
                    {
                        Devise = devise,
                        Nom = Devises[devise].Nom,
                        Symbole = Devises[devise].Symbole,
                        Valeur = c.Valeur,
                        Date = c.Journee
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec du repli sur les cours SISS.");
                return new List<CoursTempsReelDto>();
            }
        }

        private sealed class ErApiResponse
        {
            [JsonPropertyName("result")]
            public string? Result { get; set; }

            [JsonPropertyName("base_code")]
            public string? BaseCode { get; set; }

            [JsonPropertyName("rates")]
            public Dictionary<string, double>? Rates { get; set; }

            [JsonPropertyName("time_last_update_utc")]
            public string? TimeLastUpdateUtc { get; set; }
        }
    }
}
