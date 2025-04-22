using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.LifecycleEvents; // Add this
using SubdiHub_v1.Components.Authentication.Login;
using SubdiHub_v1.Components.Authentication.Signup;
using SubdiHub_v1.Components.Dashboards.Admin;
using SubdiHub_v1.Components.Dashboards.Resident;
using SubdiHub_v1.Components.Dashboards.Rider;
using SubdiHub_v1.Components.Dashboards.Seller;
namespace SubdiHub_v1
{
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
            builder.Services.AddSingleton(new Supabase.Client(
            "https://cqslvtgwuabdgqxxtzfa.supabase.co",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g"
        ));

            builder.Services.AddHttpClient();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // ✅ Register Pages for Dependency Injection (DI)

            // ✅ Hide the Windows Title Bar
            builder.ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(w => w.OnWindowCreated((window) =>
                {
                    window.ExtendsContentIntoTitleBar = true; // Removes default title bar
                }));
#endif
            });

            return builder.Build();
        }
    }
}
