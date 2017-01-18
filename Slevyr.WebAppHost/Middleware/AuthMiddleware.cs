using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin;
using NLog;

namespace Slevyr.WebAppHost.Middleware
{
    public class AuthMiddleware : OwinMiddleware
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public AuthMiddleware(OwinMiddleware next) : base(next)
        { }

        public override async Task Invoke(IOwinContext context)
        {
            try
            {
                var ipAddress = context.Request.RemoteIpAddress;
                var user = context.Authentication.User.Identity.Name;

                bool autorizationActive = Globals.AuthorizedUsers != null;
                if (autorizationActive)
                {
                    bool userAuthorized = Globals.AuthorizedUsers.FirstOrDefault(
                        s => s.Equals(user, StringComparison.InvariantCultureIgnoreCase)) != null;

                    if (!userAuthorized && context.Request.Path.HasValue &&
                        context.Request.Path.Value.Contains("/linka-params.html"))
                    {
                        NotAuthorizedResponse(context, user);
                        return;
                    }

                    if (!userAuthorized && context.Request.Path.HasValue &&
                        context.Request.Path.Value.Contains("nastav"))
                    {
                        context.Response.StatusCode = 401;
                        NotAuthorizedResponse(context, user);
                        return;
                    }
                }

                await Next.Invoke(context);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                //throw;
            }
        }

        private void NotAuthorizedResponse(IOwinContext context, string user)
        {
            //context.Response.StatusCode = 401;
            //context.Response.Redirect("http://192.168.10.230:81/service/ErrorPages/401.html#" + ipAddress);

            context.Response.ContentType = "text/plain";
            context.Response.WriteAsync($"Uživatel {user} nebyl autorizován !");
        
        }
    }
}
