using Microsoft.AspNetCore.Localization;
using Mugnum.YandexTimesheetApp.Application.Settings;
using Mugnum.YandexTimesheetApp.Application.Worklogs;
using Mugnum.YandexTimesheetApp.Components;
using Mugnum.YandexTimesheetApp.Infrastructure.Hosting;
using Mugnum.YandexTimesheetApp.Infrastructure.Settings;
using Mugnum.YandexTimesheetApp.Infrastructure.YandexOAuth;
using Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker;
using System.Globalization;

var hostCulture = CultureInfo.CurrentCulture;
var hostUiCulture = CultureInfo.CurrentUICulture;
CultureInfo.DefaultThreadCurrentCulture = hostCulture;
CultureInfo.DefaultThreadCurrentUICulture = hostUiCulture;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:7200");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
	options.DefaultRequestCulture = new RequestCulture(hostCulture, hostUiCulture);
	options.SupportedCultures =
	[
		hostCulture
	];
	options.SupportedUICultures =
	[
		hostUiCulture
	];
	options.RequestCultureProviders.Clear();
	options.ApplyCurrentCultureToResponseHeaders = true;
});

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddHttpClient();

builder.Services.AddHttpClient<ITrackerApiClient, TrackerApiClient>(client =>
{
	client.BaseAddress = new Uri("https://api.tracker.yandex.net/");
	client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddSingleton<IApplicationSettingsStore, JsonApplicationSettingsStore>();
builder.Services.AddSingleton<ISecretStore, KeyringSecretStore>();
builder.Services.AddSingleton<ApplicationSettingsService>();
builder.Services.AddSingleton<OAuthAuthorizationTransactionStore>();
builder.Services.AddSingleton<YandexOAuthService>();
builder.Services.AddSingleton<BrowserExecutableResolver>();
builder.Services.AddSingleton<BrowserLauncher>();
builder.Services.AddScoped<CurrentUserWorklogService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseRequestLocalization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapYandexOAuthEndpoints();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Services
	.GetRequiredService<BrowserLauncher>()
	.Register(app);

await app.RunAsync();
