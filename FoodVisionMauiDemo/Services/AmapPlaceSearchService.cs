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
            bool allowBroadFallback = true,
            CancellationToken cancellationToken = default)
        {
            if (!AmapApiOptions.HasConfiguredApiKey)
                throw new NearbySearchException("Nearby search is unavailable due to API configuration.");

            var longitude = location.Longitude.ToString(CultureInfo.InvariantCulture);
            var latitude = location.Latitude.ToString(CultureInfo.InvariantCulture);
            foreach (var candidate in BuildSearchCandidates(keywords, radius, allowBroadFallback))
            {
                var requestUri = BuildRequestUri(
                    longitude,
                    latitude,
                    candidate.Keywords,
                    candidate.Radius,
                    offset,
                    page,
                    candidate.Types);

                var response = await GetResponseAsync(requestUri, cancellationToken);
                EnsureSuccessfulResponse(response);

                var places = MapPlaces(response, recommendationReason);
                if (places.Count > 0)
                    return places;

                Debug.WriteLine($"[AmapPlaceSearch] No POIs for keywords={candidate.Keywords}, types={candidate.Types}, radius={candidate.Radius}, count={response.Count}");
            }

            throw new NearbySearchException("No nearby places found for this recommendation.");
        }

        private async Task<AmapAroundSearchResponse> GetResponseAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<AmapAroundSearchResponse>(requestUri, cancellationToken);
                return response ?? throw new NearbySearchException("Could not connect to the live nearby search service.");
            }
            catch (NearbySearchException)
            {
                throw;
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
        }

        private static void EnsureSuccessfulResponse(AmapAroundSearchResponse response)
        {
            if (response.Status != "0")
                return;

            Debug.WriteLine($"[AmapPlaceSearch] status=0, infocode={response.InfoCode}, info={response.Info}");
            var message = IsApiConfigurationError(response.InfoCode)
                ? "Nearby search is unavailable due to API configuration."
                : "The live nearby search service returned an error. Please try again later.";

            throw new NearbySearchException(message);
        }

        private static IReadOnlyList<NearbyPlace> MapPlaces(
            AmapAroundSearchResponse response,
            string recommendationReason)
        {
            return response.Pois
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
        }

        private static IEnumerable<SearchCandidate> BuildSearchCandidates(
            string keywords,
            int radius,
            bool allowBroadFallback)
        {
            yield return new SearchCandidate(keywords, radius, string.Empty);
            yield return new SearchCandidate(keywords, 8000, string.Empty);
            yield return new SearchCandidate($"{keywords}|餐厅|美食", 10000, "050000");

            if (allowBroadFallback)
                yield return new SearchCandidate("餐饮服务|餐厅|美食", 15000, "050000");
        }

        private static Uri BuildRequestUri(
            string longitude,
            string latitude,
            string keywords,
            int radius,
            int offset,
            int page,
            string types)
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

            if (!string.IsNullOrWhiteSpace(types))
                query["types"] = types;

            var queryString = string.Join("&", query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

            return new Uri($"https://restapi.amap.com/v3/place/around?{queryString}");
        }

        private static bool IsApiConfigurationError(string infoCode)
        {
            return infoCode is "10001" or "10002" or "10003" or "10007" or "10008" or "10009";
        }

        private static string NormalizeAddress(string address)
        {
            return string.IsNullOrWhiteSpace(address) || address == "[]"
                ? "Address unavailable"
                : address;
        }

        private readonly record struct SearchCandidate(string Keywords, int Radius, string Types);
    }
}
