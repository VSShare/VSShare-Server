using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Server.Models;

namespace Server.Controllers
{
    public class DocumentsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Documents
        [AllowAnonymous()]
        public async Task<ActionResult> Index()
        {
            var items = await db.Documents.OrderBy(c => c.Index).ToListAsync();
            return View(items);
        }

        [AllowAnonymous()]
        public async Task<ActionResult> Details(string name)
        {
            if (name == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var document = await db.Documents.FirstOrDefaultAsync(c => c.Name == name);
            if (document == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            return View(document);
        }

        // GET: Documents/Create
        [Authorize(Roles = "admin")]
        public ActionResult Create()
        {
            return View(new CreateDocumentViewModel());
        }

        // POST: Documents/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateDocumentViewModel viewModel)
        {
            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var document = new Document()
            {
                Name = viewModel.Name,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                Body = viewModel.Body,
                Owner = user,
                Index = viewModel.Index,
                Title = viewModel.Title
            };
            db.Documents.Add(document);
            await db.SaveChangesAsync();

            return RedirectToAction("Details", new {name = viewModel.Name});
        }

        // GET: Documents/Edit/5
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Edit(string name)
        {
            if (name == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);


            var document = await db.Documents.FirstOrDefaultAsync(c => c.Name == name);
            if (document == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            if (user.Id != document.Owner.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var viewModel = new EditDocumentViewModel()
            {
                Name = document.Name,
                Title = document.Title,
                Index = document.Index,
                Body = document.Body
            };

            return View(viewModel);
        }

        // POST: Documents/Edit/5
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Edit(EditDocumentViewModel viewModel)
        {
            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);


            var document = await db.Documents.FirstOrDefaultAsync(c => c.Name == viewModel.Name);
            if (document == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            if (user.Id != document.Owner.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            document.Title = viewModel.Title;
            document.Body = viewModel.Body;
            document.Index = viewModel.Index;
            document.ModifiedAt = DateTime.Now;
            await db.SaveChangesAsync();

            return RedirectToAction("Details", new {name = document.Name});
        }

        // GET: Documents/Delete/5
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Delete(string name)
        {
            if (name == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);


            var document = await db.Documents.FirstOrDefaultAsync(c => c.Name == name);
            if (document == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            if (user.Id != document.Owner.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return View(document);
        }

        // POST: Documents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteConfirmed(string name)
        {
            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var document = await db.Documents.FirstOrDefaultAsync(c => c.Name == name);
            if (document == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            if (user.Id != document.Owner.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            db.Documents.Remove(document);
            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        private async Task<ApplicationUser> GetApplicationUser()
        {
            var userId = User.Identity.GetUserId();
            var appUser = await db.Users.FirstOrDefaultAsync(user => user.Id == userId);
            return appUser;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
