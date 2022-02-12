using System.IO;
using System.Text.Json;
using Android.App;
using Android.Content;
using BMBF.Backend;
using BMBF.Backend.Configuration;
using BMBF.Backend.Implementations;
using BMBF.Backend.Services;
using BMBF.WebServer;
using Serilog;

namespace BMBF.Implementations;

public class AndroidWebService : WebService
{
    private readonly Service _bmbfService;
    

    protected override void SetupApi(Router router)
    {
        base.SetupApi(router);
        
        router.Post("/quit", _ =>
        {
            var intent = new Intent(BMBFIntents.Quit);
            _bmbfService.SendBroadcast(intent);
            _bmbfService.StopSelf();
            return Response.Empty().Async();
        });
        router.Post("/restart", _ =>
        {
            var intent = new Intent(BMBFIntents.Restart);
            _bmbfService.SendBroadcast(intent);
            return Response.Empty().Async();
        });
        router.Post("/runInBackground", req =>
        {
            bool runInBackground = req.JsonBody<bool>();
            bool currentlyEnabled = File.Exists(Constants.RunForegroundConfig);
            if (currentlyEnabled && !runInBackground)
            {
                Log.Information("Running in background disabled");
                File.Delete(Constants.RunForegroundConfig);
            }
            else if (!currentlyEnabled && runInBackground)
            {
                Log.Information("Running in background enabled");
                File.Create(Constants.RunForegroundConfig).Dispose();
            }
            return Response.Empty().Async();
        });
        router.Get("/runInBackground", _ => Response.Json(File.Exists(Constants.RunForegroundConfig)).Async());
    }

    public AndroidWebService(BMBFSettings settings,
        FileProviders fileProviders,
        IBeatSaberService beatSaberService,
        IModService modService,
        ICoreModService coreModService,
        IPlaylistService playlistService,
        ISongService songService, 
        IFileImporter fileImporter, 
        JsonSerializerOptions serializerOptions,
        IMessageService messageService,
        ISetupService setupService,
        IAssetService assetService, 
        Service bmbfService) : base(settings,
        fileProviders,
        beatSaberService,
        modService,
        coreModService,
        playlistService,
        songService, fileImporter,
        serializerOptions, 
        messageService, 
        setupService, 
        assetService)
    {
        _bmbfService = bmbfService;
    }
}
