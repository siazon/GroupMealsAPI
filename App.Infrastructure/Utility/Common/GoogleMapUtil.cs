using App.Domain.Common.Address;
using App.Domain.Common.Shop;
using GoogleMapsApi;
using GoogleMapsApi.Entities.Directions.Request;
using GoogleMapsApi.Entities.Directions.Response;
using GoogleMapsApi.Entities.DistanceMatrix.Request;
using GoogleMapsApi.Entities.DistanceMatrix.Response;
using GoogleMapsApi.Entities.Geocoding.Request;
using GoogleMapsApi.Entities.PlaceAutocomplete.Request;
using GoogleMapsApi.Entities.PlaceAutocomplete.Response;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public interface IGoogleMapUtil
    {
        Task<List<DbAddress>> SuggestAddress(string address, string countryCode);

        Task<DbAddress> GetGeoAddress(string address, string countryCode);

        Task<DbAddress> GetGeoAddressByPostCode(string postCode, string countryCode);

        Task<int?> GetDistance(string originalAddress, string findAddress);

        Task<int?> CalculateDistanceMatrix(string fromLocation, string toLocation);
    }

    public class GoogleMapUtil : IGoogleMapUtil
    {
        private const string ApiKey = "AIzaSyDt-HUfx7q3MU9BkTMB-1adDHhXSamvoz4";

        public async Task<List<DbAddress>> SuggestAddress(string address, string countryCode)
        {
            var list = new List<DbAddress>();

            var request = new PlaceAutocompleteRequest
            {
                ApiKey = ApiKey,
                Input = address,
                Components = string.Format("country:{0}", countryCode),
                Type = "address"
            };

            var result = await GoogleMaps.PlaceAutocomplete.QueryAsync(request);

            if (result.Status == Status.OK)
            {
                list.AddRange(result.Results.Select(item => new DbAddress { PlaceId = item.PlaceId, Address1 = item.Description }));
            }

            return list;
        }

        public async Task<DbAddress> GetGeoAddress(string address, string countryCode)
        {
            var request = new GeocodingRequest
            {
                ApiKey = ApiKey,
                Address = address,
                Components = new GeocodingComponents()
                {
                    Country = countryCode,
                }
            };

            var result = await GoogleMaps.Geocode.QueryAsync(request);

            if (result.Status == GoogleMapsApi.Entities.Geocoding.Response.Status.OK)
            {
                var results = result.Results.Where(r => r.Types.Contains("street_address"));
                if (results == null || !results.Any())
                    return null;

                var match = results.FirstOrDefault();
                var returnAddress = new DbShopAddress
                {
                    Latitude = match.Geometry.Location.Latitude.ToString(),
                    Longitude = match.Geometry.Location.Longitude.ToString(),
                    PlaceId = match.PlaceId,
                    Address1 = match.FormattedAddress
                };

                return returnAddress;
            }

            return null;
        }

        public async Task<DbAddress> GetGeoAddressByPostCode(string postCode, string countryCode)
        {
            var request = new GeocodingRequest
            {
                ApiKey = ApiKey,
                Address = postCode,
                Components = new GeocodingComponents()
                {
                    Country = countryCode,
                }
            };

            var result = await GoogleMaps.Geocode.QueryAsync(request);

            if (result.Status == GoogleMapsApi.Entities.Geocoding.Response.Status.OK)
            {
                var results = result.Results;
                if (results == null || !results.Any())
                    return null;

                var firstResult = results.FirstOrDefault();
                var location = firstResult.Geometry.Location;

                return new DbAddress
                {
                    Latitude = location.Latitude.ToString(),
                    Longitude = location.Longitude.ToString(),
                    Address1 = firstResult.FormattedAddress
                };
            }

            return null;
        }

        public async Task<int?> GetDistance(string originalAddress, string findAddress)
        {
            var request = new DirectionsRequest
            {
                Origin = originalAddress,
                Destination = findAddress,
                ApiKey = ApiKey
            };

            var result = await GoogleMaps.Directions.QueryAsync(request);

            if (result.Status == DirectionsStatusCodes.OK)
            {
                var distance = result.Routes.FirstOrDefault().Legs.FirstOrDefault().Distance;
                return distance.Value;
            }

            return null;
        }

        public async Task<int?> CalculateDistanceMatrix(string fromLocation, string toLocation)
        {
            var request = new DistanceMatrixRequest
            {
                Origins = new[] { fromLocation },
                Destinations = new[] { toLocation },
                ApiKey = ApiKey
            };

            var result = await GoogleMaps.DistanceMatrix.QueryAsync(request);

            if (result.Status == DistanceMatrixStatusCodes.OK)
            {
                var distance = result.Rows.FirstOrDefault().Elements.FirstOrDefault().Distance;
                return distance.Value;
            }

            return null;
        }
    }
}