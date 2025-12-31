using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WPFLLM.Services;
using WPFLLM.ViewModels;

namespace WPFLLM;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var dbService = Services.GetRequiredService<IDatabaseService>();
        dbService.InitializeAsync().Wait();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILlmService, LlmService>();
        services.AddSingleton<IRagService, RagService>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IOpenRouterService, OpenRouterService>();
        services.AddSingleton<IModelDownloadService, ModelDownloadService>();
        services.AddSingleton<ILocalEmbeddingService, LocalEmbeddingService>();

        services.AddHttpClient();

        services.AddTransient<MainViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<RagViewModel>();
        services.AddTransient<EmbeddingsViewModel>();
    }
}
