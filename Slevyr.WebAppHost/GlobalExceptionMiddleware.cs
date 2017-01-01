using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using NLog;

namespace Slevyr.WebAppHost
{
    public class GlobalExceptionMiddleware : OwinMiddleware
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public GlobalExceptionMiddleware(OwinMiddleware next) : base(next)
        { }

        public override async Task Invoke(IOwinContext context)
        {
            try
            {
                await Next.Invoke(context);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                //throw;
            }
        }
    }
}
