using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WPFLLM.Services;
using WPFLLM.ViewModels;

namespace WPFLLM;

[SupportedOSPlatform("windows")]
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers for debugging
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"[UNHANDLED] {ex}");
            MessageBox.Show($"Unhandled Exception:\n{ex?.Message}\n\n{ex?.StackTrace}", "Crash", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        
        DispatcherUnhandledException += (s, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"[DISPATCHER] {args.Exception}");
            MessageBox.Show($"UI Exception:\n{args.Exception.Message}\n\n{args.Exception.StackTrace}", "Crash", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Initialize critical services synchronously before UI loads
        try
        {
            var dbService = Services.GetRequiredService<IDatabaseService>();
            dbService.InitializeAsync().GetAwaiter().GetResult();
            
            var settingsService = Services.GetRequiredService<ISettingsService>();
            var appSettings = settingsService.GetSettingsAsync().GetAwaiter().GetResult();
            
            var localization = Services.GetRequiredService<ILocalizationService>();
            localization.SetLanguage(appSettings.Language);
            
            var encryption = Services.GetRequiredService<IEncryptionService>();
            encryption.SetEnabled(appSettings.EncryptData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[INIT] Error: {ex}");
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IStatusService, StatusService>();
        services.AddSingleton<ILlmService, LlmService>();
        services.AddSingleton<IRagService, RagService>();
        services.AddSingleton<IIngestionService, IngestionService>();
        services.AddSingleton<IDocumentAnalysisService, DocumentAnalysisService>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IOpenRouterService, OpenRouterService>();
        services.AddSingleton<IModelDownloadService, ModelDownloadService>();
        services.AddSingleton<ILocalEmbeddingService, LocalEmbeddingService>();
        services.AddSingleton<ILocalLlmService, LocalLlmService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IRateLimiter, RateLimiter>();
        services.AddSingleton<IExportService, ExportService>();

        services.AddHttpClient();

        services.AddTransient<MainViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<DocumentAnalysisViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<RagViewModel>();
        services.AddTransient<EmbeddingsViewModel>();
    }
}
