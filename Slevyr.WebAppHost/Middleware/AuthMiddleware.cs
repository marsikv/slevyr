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
                string user;

                try
                {
                    user = context.Authentication.User.Identity.Name;
                    bool isAuthenticated = context.Authentication.User.Identity.IsAuthenticated;
                    Logger.Info($"user:{user} ip:{ipAddress} isAuth:{isAuthenticated}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    throw;
                }

                bool autorizationActive = Globals.AuthorizedUsers != null;
                if (autorizationActive)
                {
                    bool userAuthorized = user!=null && Globals.AuthorizedUsers.FirstOrDefault(
                        s => s.Equals(user, StringComparison.InvariantCultureIgnoreCase)) != null;

                    if (!userAuthorized && context.Request.Path.HasValue &&
                        (context.Request.Path.Value.Contains("/linka-params.html") || context.Request.Path.Value.Contains("/nastaveni.html")))
                    {
                        NotAuthorizedResponse(context, user);
                        return;
                    }

                    if (!userAuthorized && context.Request.Path.HasValue &&
                        context.Request.Path.Value.Contains("nastav"))
                    {
                        context.Response.StatusCode = 401;
                        //NotAuthorizedResponse(context, user);
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
            context.Response.StatusCode = 401;
            context.Response.Redirect("401.html#" + user);

            Logger.Info($"{user} not autorized");

            //context.Response.ContentType = "text/html";
            //context.Response.WriteAsync($"Uživatel {user} nebyl autorizován !");
        
        }
    }
}
