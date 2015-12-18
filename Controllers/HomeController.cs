using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Server.Models;

namespace Server.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ViewResult> Index()
        {
            var items = await db.Rooms.Where(c => c.IsLive && !c.IsHidden).OrderByDescending(c => c.TotalVisitor).Take(5).ToListAsync();

            return View(items);
        }
        
        public ActionResult Credit()
        {
            return View();
        }
    }
}