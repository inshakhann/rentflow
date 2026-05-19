using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RentFlow.Client;

using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using RentFlow.Client.Helpers;
using RentFlow.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthStateProvider>());

builder.Services.AddScoped<JwtAuthorizationMessageHandler>();

builder.Services.AddHttpClient("ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

// Default HttpClient for unauthenticated calls
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<PropertyService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<MaintenanceService>();
builder.Services.AddScoped<GeolocationService>();
builder.Services.AddScoped<LeaseService>();
builder.Services.AddScoped<WeatherService>();

await builder.Build().RunAsync();
