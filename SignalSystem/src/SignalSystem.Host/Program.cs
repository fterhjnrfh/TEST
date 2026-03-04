using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SignalSystem.Processing.Abstractions;
using SignalSystem.Processing.Impl;
using SignalSystem.SDK.Abstractions;
using SignalSystem.SDK.Impl;
using SignalSystem.Storage.Sdf;
using SignalSystem.UI;

namespace SignalSystem.Host;

class Program
{
    /// <summary>
    /// 全局 DI 容器（供 ViewModel 层按需取用）
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // 1. 构建 DI 容器
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            // 2. 启动 Avalonia
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
            System.IO.File.WriteAllText(logPath, $"{DateTime.Now}\n{ex}\n");
            Console.Error.WriteLine(ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // SDK
        services.AddSingleton<ISignalSourceSdk, SignalSourceSdk>();

        // Processing
        services.AddSingleton<IProcessingPipeline, DefaultProcessingPipeline>();
        services.AddSingleton<IIntegrityValidator, DefaultIntegrityValidator>();

        // Storage
        services.AddTransient<IStorageWriter, SdfStorageWriter>();
    }
}
