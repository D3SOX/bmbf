using System;

namespace BMBF.WebServer
{
    internal class WebException : Exception
    {
        public readonly Response Response;

        public WebException(Response response)
        {
            Response = response;
        }
    }
}
