using System;
using System.Linq;
using System.Security.Claims;
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
                ClaimsIdentity claimsIdentity;
                string userName = null;

                try
                {
                    //-pro windows authentication
                    //user = context.Authentication.User.Identity.Name;
                    //bool isAuthenticated = context.Authentication.User.Identity.IsAuthenticated;

                    //-pro basic authentication - asi to neni legeartis
                    claimsIdentity = context.Authentication.User?.Identity as System.Security.Claims.ClaimsIdentity;
                    if (claimsIdentity != null) userName = claimsIdentity.Name;

                    Logger.Info($"user:{userName} ip:{ipAddress}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    throw;
                }

                if (userName == null || !claimsIdentity.IsAuthenticated)
                {
                    context.Authentication.Challenge();
                    return;
                }

                bool autorizationActive = Globals.Users != null;
                if (autorizationActive && context.Request.Path.HasValue)
                {
                    var user = Globals.Users.FirstOrDefault(s => s.NameMatch(userName));

                    if (context.Request.Path.Value.Contains("/linka-params.html") || context.Request.Path.Value.Contains("/nastaveni.html"))
                    {
                        if (user == null || !user.NastaveniEnabled)
                        {
                            NotAuthorizedResponse(context, userName);
                            return;
                        }
                    }

                    if (context.Request.Path.Value.Contains("nastav"))
                    {
                        if (user == null || !user.NastaveniEnabled)
                        {
                            context.Response.StatusCode = 401;
                            //NotAuthorizedResponse(context, user);
                            return;
                        }
                    }
                }

                await Next.Invoke(context);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
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
