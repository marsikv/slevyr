using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Web.Http;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;

namespace Slevyr.WebAppHost
{
    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            //registruji middleware odchytávající a zapisující vyjimky
            appBuilder.Use<GlobalExceptionMiddleware>();
            


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

            appBuilder.Use<GlobalExceptionMiddleware>().UseWebApi(config);
            //appBuilder.UseWebApi(config);


            //A. nacitam z adresare www, ktery je soucasti solution
            //var physicalFileSystem = new PhysicalFileSystem(@"./WWW1");

            //B. nacitam z adresare ktery je mimo solution, dle nastaveni www.rootDir v  app.config
            var contentDir = ConfigurationManager.AppSettings["www.rootDir"];
            var physicalFileSystem = new PhysicalFileSystem(contentDir);

            var options = new FileServerOptions
            {
                EnableDefaultFiles = true,
                FileSystem = physicalFileSystem,
            };
            options.StaticFileOptions.FileSystem = physicalFileSystem;
            options.StaticFileOptions.ServeUnknownFileTypes = true;
            options.StaticFileOptions.OnPrepareResponse = (staticFileResponseContext) =>
            {
                staticFileResponseContext.OwinContext.Response.Headers.Add("Cache-Control",
                    new[] { "public", "no-cache, no-store, must-revalidate, max-age=0" });
            };

            options.DefaultFilesOptions.DefaultFileNames = new[]
            {
                "menu.html"
            };

            appBuilder.UseFileServer(options);

            //appBuilder.UseStaticFiles(new StaticFileOptions()
            //{
            //    FileSystem = new PhysicalFileSystem(contentDir)
            //});
        }
    }
}
