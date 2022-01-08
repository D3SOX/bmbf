using BMBF.Implementations;
using BMBF.Patching;
using BMBF.Services;
using BMBF.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using Microsoft.AspNetCore.Hosting;

namespace BMBF
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton(Configuration.GetSection(BMBFSettings.Position).Get<BMBFSettings>());
            services.AddSingleton<ISongService, SongService>();
            services.AddSingleton<IPlaylistService, PlaylistService>();
            services.AddSingleton<IBeatSaberService, BeatSaberService>();
            services.AddSingleton<ISetupService, SetupService>();
            services.AddSingleton<IAssetService, AssetService>();
            
            services.AddSingleton(HttpClientUtil.CreateBMBFHttpClient());
            services.AddSingleton<IExtensionsService, ExtensionsService>();
            services.AddSingleton<IFileImporter, FileImporter>();
            services.AddSingleton<IBeatSaverService, BeatSaverService>();

            // Configure our legacy tags
            var tagManager = new TagManager();
            tagManager.RegisterLegacyTag("modded",
                () => new PatchManifest("QuestPatcher", null) { ModloaderName = "QuestLoader" });
            tagManager.RegisterLegacyTag("BMBF.modded",
                () => new PatchManifest("BMBF", null) { ModloaderName = "QuestLoader" });

            services.AddSingleton(tagManager);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMiddleware<Middleware>();
            app.UseMvc();
        }
    }
}