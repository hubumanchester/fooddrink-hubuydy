using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using FoodVisionMauiDemo.Models;
using Microsoft.Maui.Devices.Sensors;

namespace FoodVisionMauiDemo.Services
{
    public class AmapPlaceSearchService
    {
        private readonly HttpClient _httpClient;

        public AmapPlaceSearchService()
            : this(new HttpClient { Timeout = TimeSpan.FromSeconds(12) })
        {
        }

        public AmapPlaceSearchService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<NearbyPlace>> SearchAroundAsync(
            Location location,
            string keywords,
            string recommendationReason,
            int radius = 3000,
            int offset = 10,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            if (!AmapApiOptions.HasConfiguredApiKey)
                throw new NearbySearchException("Nearby search is unavailable due to API configuration.");

            var longitude = location.Longitude.ToString(CultureInfo.InvariantCulture);
            var latitude = location.Latitude.ToString(CultureInfo.InvariantCulture);
            var requestUri = BuildRequestUri(longitude, latitude, keywords, radius, offset, page);

            AmapAroundSearchResponse? response;
            try
            {
                response = await _httpClient.GetFromJsonAsync<AmapAroundSearchResponse>(requestUri, cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine(ex);
                throw new NearbySearchException("Could not connect to the live nearby search service.", ex);
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex);
                throw new NearbySearchException("Could not connect to the live nearby search service.", ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw new NearbySearchException("Could not connect to the live nearby search service.", ex);
            }

            if (response == null)
                throw new NearbySearchException("Could not connect to the live nearby search service.");

            if (response.Status == "0")
            {
                var message = IsApiConfigurationError(response.InfoCode)
                    ? "Nearby search is unavailable due to API configuration."
                    : $"Live nearby search failed: {CleanApiInfo(response.Info)}";

                throw new NearbySearchException(message);
            }

            var places = response.Pois
                .Where(poi => !string.IsNullOrWhiteSpace(poi.Name))
                .Select(poi => new NearbyPlace
                {
                    Name = poi.Name,
                    Type = poi.Type,
                    Address = NormalizeAddress(poi.Address),
                    Location = poi.Location,
                    DistanceMeters = int.TryParse(poi.Distance, out var distance) ? distance : null,
                    Tel = poi.Tel,
                    RecommendationReason = recommendationReason
                })
                .OrderBy(place => place.DistanceMeters ?? int.MaxValue)
                .ToList();

            if (places.Count == 0)
                throw new NearbySearchException("No nearby places found for this recommendation.");

            return places;
        }

        private static Uri BuildRequestUri(
            string longitude,
            string latitude,
            string keywords,
            int radius,
            int offset,
            int page)
        {
            var query = new Dictionary<string, string>
            {
                ["key"] = AmapApiOptions.WebServiceApiKey,
                ["location"] = $"{longitude},{latitude}",
                ["keywords"] = keywords,
                ["radius"] = radius.ToString(CultureInfo.InvariantCulture),
                ["offset"] = offset.ToString(CultureInfo.InvariantCulture),
                ["page"] = page.ToString(CultureInfo.InvariantCulture),
                ["extensions"] = "base",
                ["output"] = "JSON"
            };

            var queryString = string.Join("&", query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

            return new Uri($"https://restapi.amap.com/v3/place/around?{queryString}");
        }

        private static bool IsApiConfigurationError(string infoCode)
        {
            return infoCode is "10001" or "10002" or "10003" or "10007" or "10008" or "10009";
        }

        private static string CleanApiInfo(string info)
        {
            return string.IsNullOrWhiteSpace(info) ? "The live service returned an error." : info;
        }

        private static string NormalizeAddress(string address)
        {
            return string.IsNullOrWhiteSpace(address) || address == "[]"
                ? "Address unavailable"
                : address;
        }
    }
}
