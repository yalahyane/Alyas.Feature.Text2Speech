using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Pipelines;

namespace Alyas.Feature.Text2Speech.Pipelines
{
    public class ApplicationStart
    {
        public void Process(PipelineArgs args)
        {
            var routes = RouteTable.Routes;
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute("ConvertText2Speech", "Text2Speech/ConvertText2Speech", new { controller = "Text2Speech", action = "ConvertText2Speech" });
        }
    }
}