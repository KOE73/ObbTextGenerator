using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ObbTextGenerator;

internal static class Program
{
    private static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en");
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");

        AnsiConsole.Write(
            new FigletText("BG Downloader")
                .Color(Color.DeepSkyBlue1));

        AnsiConsole.MarkupLine("[grey]OBB Text Generator background tools[/]");
        AnsiConsole.WriteLine();

        var app = new CommandApp<DownloadBackgroundsCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("obb-bg-downloader");
            config.AddCommand<DownloadBackgroundsCommand>("download")
                .WithDescription("Download background images through a supported image source API.")
                .WithExample(["download", "--count", "100"])
                .WithExample(["download", "--query", "fabric", "--count", "200", "--output", "LocalArtifacts/Backgrounds/Wallhaven/Fabric"])
                .WithExample(["download", "--sorting", "toplist", "--top-range", "6M", "--atleast", "1920x1080"]);
        });

        return app.Run(args);
    }
}
