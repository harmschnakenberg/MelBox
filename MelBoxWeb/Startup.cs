using Grapevine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxWeb
{
    public class Startup
    {
        public IConfiguration Configuration { get; private set; }

        private readonly string _serverPort = PortFinder.FindNextLocalOpenPort(1234);

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
            });
        }

        public void ConfigureServer(IRestServer server)
        {           
            server.Prefixes.Add($"http://localhost:{_serverPort}/");

            /* Configure Router Options (if supported by your router implementation) */
            server.Router.Options.SendExceptionMessages = true;
        }
    }
}
