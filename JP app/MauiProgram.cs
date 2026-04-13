using CommunityToolkit.Maui;
using JP_app.Services;

namespace JP_app;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
        // Define local storage paths for all SQLite databases
        string kanjiPath = Path.Combine(FileSystem.AppDataDirectory, "KanjiData.db3");
		string distPath = Path.Combine(FileSystem.AppDataDirectory, "JMdict_e.db3");
		string sentPath = Path.Combine(FileSystem.AppDataDirectory, "Sentences.db3");

        // Helper function to extract database files from the app package to the device's file system
        void CopyDbIfNotExists(string fileName, string targetPath)
		{
			if (!File.Exists(targetPath))
			{
                // OpenAppPackageFileAsync accesses files marked as 'MauiAsset' in Resources/Raw
                using var assetStream = FileSystem.OpenAppPackageFileAsync(fileName)
												.GetAwaiter().GetResult();

				using var fileStream = File.Create(targetPath);
				assetStream.CopyTo(fileStream);
				System.Diagnostics.Debug.WriteLine($">>> DB {fileName} COPIED!");
			}
		}

        // Initialize databases by copying them to the writable app directory
        CopyDbIfNotExists("KanjiData.db3", kanjiPath);
		CopyDbIfNotExists("JMdict_e.db3", distPath);
		CopyDbIfNotExists("Sentences.db3", sentPath);

        // Register the data service as a Singleton (exists for the lifetime of the app)
        builder.Services.AddSingleton(new KanjiService(kanjiPath, distPath, sentPath));

		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoBorders", (handler, view) =>
		{
#if ANDROID
			handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#elif IOS || MACCATALYST
    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif WINDOWS
    handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
#endif
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        // Dependency Injection registration for ViewModels and Pages
        builder.Services.AddSingleton<DetailsViewModel>();
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<MainPage>();

		return builder.Build();
	}
}