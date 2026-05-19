using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using RentFlow.Shared.DTOs;

namespace RentFlow.Client.Services
{
    public class GeolocationService
    {
        private readonly IJSRuntime _jsRuntime;

        public GeolocationService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<LocationDto?> GetCurrentLocation()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<LocationDto>("getLocation");
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
