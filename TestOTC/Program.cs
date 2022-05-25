// See https://aka.ms/new-console-template for more information
using System.Globalization;
using TestOTC.Configuration;
using static TestOTC.Services.MainService;

var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await MainConfiguration.CreateHostBuilder<MainWorkerService>(args).Build().StartAsync();
