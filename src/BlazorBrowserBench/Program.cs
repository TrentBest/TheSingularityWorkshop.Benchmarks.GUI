using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Playwright;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.Benchmarks.GUI.BlazorBrowserBench;

internal static class Program
{
    private const string Address = "http://127.0.0.1:5099";

    private static async Task Main(string[] args)
    {
        var app = WebApplication.CreateBuilder(args).Build();
        app.Urls.Add(Address);
        app.MapGet("/{kind}", async (string kind, IServiceProvider services, ILoggerFactory loggerFactory) =>
        {
            if (kind is not ("gui" or "native"))
                return Results.NotFound();

            await using var renderer = new HtmlRenderer(services, loggerFactory);
            var html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(BenchmarkPage.Native)] = kind == "native",
                    [nameof(BenchmarkPage.Sections)] = 18
                });
                var output = await renderer.RenderComponentAsync<BenchmarkPage>(parameters);
                return output.ToHtmlString();
            });

            return Results.Content($"<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{kind}</title></head><body>{html}</body></html>", "text/html");
        });

        await app.StartAsync();
        try
        {
            await RunBrowserMeasurements();
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static async Task RunBrowserMeasurements()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        const int samplesPerImplementation = 30;
        var results = new List<Measurement>();
        for (var sample = 0; sample < samplesPerImplementation; sample++)
        {
            // Alternate the paired order to reduce fixed native-first/GUI-first bias.
            var order = sample % 2 == 0
                ? new[] { "native", "gui" }
                : new[] { "gui", "native" };

            foreach (var kind in order)
            {
                var page = await browser.NewPageAsync(new BrowserNewPageOptions
                {
                    ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
                    DeviceScaleFactor = 1
                });
                try
                {
                    await page.GotoAsync($"{Address}/{kind}", new PageGotoOptions { WaitUntil = WaitUntilState.Load });
                    await page.Locator("#dashboardHeading").WaitForAsync();
                    var timing = await page.EvaluateAsync<JsonElement>("""
                        () => {
                            const nav = performance.getEntriesByType('navigation')[0];
                            const root = document.querySelector('#dashboard');
                            const rect = root.getBoundingClientRect();
                            // Force style/layout calculation; this is not a physical-paint measurement.
                            const settled = getComputedStyle(root).display;
                            return {
                                responseStart: nav.responseStart,
                                domContentLoaded: nav.domContentLoadedEventEnd,
                                load: nav.loadEventEnd,
                                layoutWidth: rect.width,
                                layoutHeight: rect.height,
                                computedDisplay: settled
                            };
                        }
                        """);
                    results.Add(new Measurement(kind, sample + 1, timing.GetProperty("responseStart").GetDouble(), timing.GetProperty("domContentLoaded").GetDouble(), timing.GetProperty("load").GetDouble(), timing.GetProperty("layoutWidth").GetDouble(), timing.GetProperty("layoutHeight").GetDouble()));
                }
                finally
                {
                    await page.CloseAsync();
                }
            }
        }

        var outputDirectory = Path.Combine("results", "browser");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "blazor-browser.csv");
        var csv = new StringBuilder();
        csv.AppendLine("kind,sample,ttfb_ms,dom_content_loaded_ms,load_ms,layout_width,layout_height");
        foreach (var item in results)
            csv.AppendLine($"{item.Kind},{item.Sample},{item.Ttfb:F3},{item.DomContentLoaded:F3},{item.Load:F3},{item.LayoutWidth:F1},{item.LayoutHeight:F1}");
        await File.WriteAllTextAsync(outputPath, csv.ToString(), new System.Text.UTF8Encoding(false));
        Console.WriteLine($"Browser comparison samples written: {outputPath} ({samplesPerImplementation} per implementation; paired alternating order).");
        Console.WriteLine("Scope: loopback HTTP + server-side static HTML rendering + browser parsing/layout; not interactive Blazor startup, event latency, or physical paint.");
    }

    private sealed record Measurement(string Kind, int Sample, double Ttfb, double DomContentLoaded, double Load, double LayoutWidth, double LayoutHeight);

    public sealed class BenchmarkPage : ComponentBase
    {
        [Parameter] public bool Native { get; set; }
        [Parameter] public int Sections { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Native)
            {
                BlazorWorkloads.CreateNativeFragment(Sections)(builder);
                return;
            }

            var tree = BlazorWorkloads.CreateSemanticTree(Sections);
            BlazorGuiRenderer.Render(tree)(builder);
        }
    }
}
