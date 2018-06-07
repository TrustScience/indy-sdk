using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using indy_sdk_spike;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebAgent
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
           (Config.WebAppWallet, Config.WebAppDid, Config.WebAppVerKey) = 
                IndyUtils.CreateWalletIfNotExist(Config.WebAppSeed, Config.WebAppPoolName, Config.WebAppWalletName).Result;

            Config.WebAppPool = IndyUtils.CreatePool(Config.WebAppPoolName).Result;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

        }
    }
}
