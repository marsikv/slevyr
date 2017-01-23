using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Web.Http;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;
using Slevyr.WebAppHost.Middleware;
using Slevyr.WebAppHost.Model;
using Swashbuckle.Application;
using Thinktecture.IdentityModel.Owin;

namespace Slevyr.WebAppHost
{
    public class Startup
    {

        private static void ConfigureSwagger(HttpConfiguration httpConfiguration)
        {
            httpConfiguration
                .EnableSwagger(SwaggerConfig.ConfigureSwagger)
                .EnableSwaggerUi(SwaggerConfig.ConfigureSwaggerUi);
        }

        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            //Integrate windows authentication
            //HttpListener listener =
            //    (HttpListener)appBuilder.Properties["System.Net.HttpListener"];
            //listener.AuthenticationSchemes =
            //    AuthenticationSchemes.IntegratedWindowsAuthentication;

            //Basic authentication
            appBuilder.UseBasicAuthentication(new BasicAuthenticationOptions("SlevyrApi",
                async (username, password) => await Authenticate(username, password)));

            //registruji middleware odchytávající a zapisující vyjimky
            appBuilder.Use<GlobalExceptionMiddleware>();

            appBuilder.Use<AuthMiddleware>();

            // Configure Web API for self-host.             

            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );


            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
            config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);
            //config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"))

            //appBuilder.Use<GlobalExceptionMiddleware>().UseWebApi(config);
            appBuilder.UseWebApi(config);

            if (Globals.StartSwagger) ConfigureSwagger(config);

            //A. nacitam z adresare www, ktery je soucasti solution
            //var physicalFileSystem = new PhysicalFileSystem(@"./WWW1");

            //B. nacitam z adresare ktery je mimo solution, dle nastaveni www.rootDir v  app.config
            //var contentDir = ConfigurationManager.AppSettings["www.rootDir"];
            var physicalFileSystem = new PhysicalFileSystem(Globals.WwwRootDir);

            var options = new FileServerOptions
            {
                EnableDefaultFiles = true,
                FileSystem = physicalFileSystem,
            };
            options.StaticFileOptions.FileSystem = physicalFileSystem;
            options.StaticFileOptions.ServeUnknownFileTypes = true;

            //options.StaticFileOptions.OnPrepareResponse = (staticFileResponseContext) =>
            //{
            //    staticFileResponseContext.OwinContext.Response.Headers.Add("Cache-Control",
            //        new[] { "public", "no-cache, no-store, must-revalidate, max-age=0" });
            //};

            options.DefaultFilesOptions.DefaultFileNames = new[]
            {
                "index.html"
            };

            appBuilder.UseFileServer(options);

            //appBuilder.UseStaticFiles(new StaticFileOptions()
            //{
            //    FileSystem = new PhysicalFileSystem(contentDir)
            //});
        }

        
        /// <summary>
        /// Autentizacni handler pro BasicAuthentication
        /// </summary>
        /// <param Name="username"></param>
        /// <param Name="password"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Claim>> Authenticate(string username, string password)
        {
            // authenticate user
            User autentizedUser = Globals.Users.FirstOrDefault(
                        u => u.IsAutentized(username, password));

            if (autentizedUser != null)
            {
                var claims = new List<Claim>
                {
                    new Claim("Name", autentizedUser.Name),
                    new Claim(ClaimTypes.Name, autentizedUser.Name),
                    //new Claim(ClaimTypes.Role,"admin")   //TODO 
                };
                return claims;
                //var id = new ClaimsIdentity(claims, "Console App");
                //var user = new ClaimsPrincipal(id);
                //Thread.CurrentPrincipal = user;
            }

            return null;
        }
        
    }
}
