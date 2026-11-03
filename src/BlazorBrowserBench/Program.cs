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
        var results = new List<Measurement>();
        foreach (var kind in new[] { "native", "gui" })
        {
            for (var sample = 0; sample < 15; sample++)
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
                    var timing = await page.EvaluateAsync<Timing>("""
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
                    results.Add(new Measurement(kind, sample + 1, timing.ResponseStart, timing.DomContentLoaded, timing.Load, timing.LayoutWidth, timing.LayoutHeight));
                }
                finally
                {
                    await page.CloseAsync();
                }
            }
        }

        Console.WriteLine("kind,sample,ttfb_ms,dom_content_loaded_ms,load_ms,layout_width,layout_height");
        foreach (var item in results)
            Console.WriteLine($"{item.Kind},{item.Sample},{item.Ttfb:F3},{item.DomContentLoaded:F3},{item.Load:F3},{item.LayoutWidth:F1},{item.LayoutHeight:F1}");
        Console.WriteLine("Note: browser timing includes loopback HTTP + server-side static HTML rendering; it is not Blazor interactive/WASM startup or physical paint latency.");
    }

    private sealed record Timing(double ResponseStart, double DomContentLoaded, double Load, double LayoutWidth, double LayoutHeight);
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
