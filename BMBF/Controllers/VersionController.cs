using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace BMBF.Controllers
{
    [Route("[controller]")]
    public class VersionController : Controller
    {
        private readonly string _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        [HttpGet]
        public string Get()
        {
            return _version;
        }
    }
}