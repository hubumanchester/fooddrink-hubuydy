using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace FoodVisionMauiDemo.Services
{
    public class LocationService
    {
        public async Task<Location> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                    throw new NearbySearchException("Location permission is required for nearby recommendations.");

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken);

                if (location == null)
                    throw new NearbySearchException("Could not get your current location.");

                return location;
            }
            catch (NearbySearchException)
            {
                throw;
            }
            catch (FeatureNotEnabledException ex)
            {
                Debug.WriteLine(ex);
                throw new NearbySearchException("Could not get your current location.", ex);
            }
            catch (FeatureNotSupportedException ex)
            {
                Debug.WriteLine(ex);
                throw new NearbySearchException("Could not get your current location.", ex);
            }
            catch (PermissionException ex)
            {
                Debug.WriteLine(ex);
                throw new NearbySearchException("Location permission is required for nearby recommendations.", ex);
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine(ex);
                throw new NearbySearchException("Could not get your current location.", ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw new NearbySearchException("Could not get your current location.", ex);
            }
        }
    }
}
