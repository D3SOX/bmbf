using System.Reflection;
using Android.App;
using Android.Content;
using Microsoft.AspNetCore.Mvc;

namespace BMBF.Controllers
{
    [Route("[controller]")]
    public class HostController : Controller
    {
        // This is static to avoid reflecting multiple times
        private static readonly string Version;
        static HostController()
        {
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            Version = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
        }

        private readonly Service _bmbfService;

        public HostController(Service bmbfService)
        {
            _bmbfService = bmbfService;
        }

        [HttpGet]
        [Route("version")]
        public string GetVersion()
        {
            return Version;
        }

        [HttpPost]
        [Route("quit")]
        public void Quit()
        {
            // Tell frontend to quit too
            Intent intent = new Intent(BMBFIntents.Quit);
            _bmbfService.SendBroadcast(intent);
            
            // Actually stop BMBFService
            _bmbfService.StopSelf();
        }
    }
}