// /r:Microsoft.Extensions.Hosting
// /r:NReco.Logging.File   https://github.com/nreco/logging

public partial class App : Application {
    readonly IHost _host;
    public App() {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var config = context.Configuration;
                services.Configure<AppSettings>(config.GetSection("AppSettings"));
                services.AddLogging(builder => builder.AddFile(config.GetSection("Logging")));
                services.AddScoped<MainWindowVM>();
            })
            .Build();
    }
    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync().ConfigureAwait(true);
        var log = _host.Services.GetRequiredService<ILogger<App>>();
        log.LogInformation("Starting");
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            log.LogError(e.ExceptionObject as Exception, "Domain Unhandled Exception");
        };
        Current.Exit += async (s, e) => {
            log!.LogInformation("Stopping");
            using (_host) await _host.StopAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        };
        // init db, load settings
        var mainVM = _host.Services.GetRequiredService<MainWindowVM>();
        var window = new Window { Content = mainVM, Title = "App" };
        window.Show();
        await mainVM.InitAsync().ConfigureAwait(true);
        base.OnStartup(e);
    }
}
public class AppSettings {
    public string? Prop { get; set; }
}
public partial class MainWindowVM : ObservableObject {
    readonly AppSettings _s;
    public MainWindowVM(IOptions<AppSettings> s) { _s = s.Value; }
    public async Task InitAsync() { }
}
