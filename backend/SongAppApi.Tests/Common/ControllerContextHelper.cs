using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SongAppApi.Controllers;
using SongAppApi.Entities;

namespace SongAppApi.Tests.Common
{
    public static class ControllerContextHelper
    {
        public static void Attach(ControllerBase controller, Account? account, string? remoteIp = null)
        {
            var http = new DefaultHttpContext();
            if (account != null)
                http.Items["Account"] = account;
            if (remoteIp != null)
                http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
            controller.ControllerContext = new ControllerContext { HttpContext = http };
        }
    }
}
