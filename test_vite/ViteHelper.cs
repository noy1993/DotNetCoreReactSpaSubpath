using Microsoft.Extensions.FileProviders;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace test_vite
{
    public class ViteOption
    {
        public readonly int debugPort;
        public readonly string clientName;
        public readonly string projectName;
        public string buildPath = "dist";

        public ViteOption(int port, string clientName, string projectName)
        {
            this.debugPort = port;
            this.clientName = clientName;
            this.projectName = projectName;
        }
    }

    public static class ViteHelper
    {
        private static ILogger? ViteLogger;

        public static void UseViteSpaServer(this WebApplication app,
            string pathMatch, ViteOption option)
        {
            ViteLogger = app.Services.GetService<ILoggerFactory>()?.CreateLogger($"Vite: ({pathMatch} => {option.clientName}/{option.projectName})");
            if (app.Environment.IsDevelopment())
            {
                EnsureNodeModuleAlreadyInstalled(option);
                RunDev(option);
                MapUrl(app, pathMatch, option);
            }
            else
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", option.clientName, option.projectName, option.buildPath);
                var sfo = new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(path)
                };
                app.UseStaticFiles(sfo);
                var index_path = Path.Combine(option.clientName, option.projectName, option.buildPath, "index.html");
                if (pathMatch == "/")
                {
                    app.MapFallbackToFile(index_path);
                }
                else
                {
                    app.MapFallbackToFile(pathMatch, index_path);
                }
            }
        }

        private static void MapUrl(WebApplication app, string pathMatch, ViteOption option)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments(pathMatch))
                {
                    context.Response.Redirect($"http://127.0.0.1:{option.debugPort}" + context.Request.Path);
                    return;
                }
                await next();
            });
        }

        private static bool IsPortInUse(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpListeners = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endpoint in tcpListeners)
            {
                if (endpoint.Port == port)
                {
                    return true;
                }
            }

            return false;
        }
        private static void RunDev(ViteOption option)
        {
            if (!IsPortInUse(option.debugPort))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), option.clientName, option.projectName);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "yarn.cmd",
                    Arguments = $"dev --port {option.debugPort} --strictPort",
                    WorkingDirectory = path,
                    CreateNoWindow = false,
                    UseShellExecute = true,
                });
            }
            else
            {
                ViteLogger?.LogInformation($"The port {option.debugPort} is already in use.");
            }
        }

        private static void EnsureNodeModuleAlreadyInstalled(ViteOption option)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), option.clientName, option.projectName);
            if (!Directory.Exists(Path.Combine(path, "node_modules")))
            {
                ViteLogger?.LogWarning($"node_modules not found , run yarn ...");

                var ps = Process.Start(new ProcessStartInfo()
                {
                    FileName = "yarn.cmd",
                    WorkingDirectory = path,
                });
                ps?.WaitForExit();
                ViteLogger?.LogWarning("yarn done!");
            }
        }
    }
}
