using System.Web;
using System.Web.Mvc;
using Server.Controllers;

namespace Server
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new AiHandleErrorAttribute());
        }
    }
}
