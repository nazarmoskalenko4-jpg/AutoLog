using Microsoft.Extensions.Logging;
using AutoLog.Services;
using AutoLog.ViewModels;
using AutoLog.Views;

namespace AutoLog;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

     // Реєструємо нашу базу даних як Singleton (один екземпляр на весь додаток)
        builder.Services.AddSingleton<LocalDatabase>();
		builder.Services.AddSingleton<OptimizationService>();

    // Реєструємо ViewModel та View
        builder.Services.AddTransient<VehiclesViewModel>();
        builder.Services.AddTransient<VehiclesPage>();

		builder.Services.AddTransient<AddVehicleViewModel>();
        builder.Services.AddTransient<AddVehiclePage>();

		builder.Services.AddTransient<FuelHistoryViewModel>();
        builder.Services.AddTransient<FuelHistoryPage>();

		builder.Services.AddTransient<AddFuelRecordViewModel>();
        builder.Services.AddTransient<AddFuelRecordPage>();

		builder.Services.AddTransient<OptimizerViewModel>();
        builder.Services.AddTransient<OptimizerPage>();

		builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();

		builder.Services.AddTransient<AuthViewModel>();
        builder.Services.AddTransient<AuthPage>(); 

		builder.Services.AddSingleton<RecommendationService>();

		builder.Services.AddSingleton<CarCatalogService>();

		builder.Services.AddSingleton<FuelMarketService>();

		return builder.Build();
	}
}
