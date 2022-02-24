using System;
using Hydra;

namespace BMBF.WebServer
{
    internal class WebException : Exception
    {
        public readonly HttpResponse Response;

        public WebException(HttpResponse response)
        {
            Response = response;
        }
    }
}
