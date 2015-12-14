using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Server.Models;
using Server.Models.Manager;

namespace Server.Controllers
{
    public class RoomsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Rooms
        public ActionResult Index()
        {
            return View(db.Rooms.ToList());
        }

        // GET: Rooms/Details/5
        public async Task<ActionResult> Details(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            var room = await db.Rooms.Where(c => c.Name == name).SingleOrDefaultAsync();
            if (room == null)
            {
                return HttpNotFound();
            }

            return View(room);
        }


        [AllowAnonymous()]
        public async Task<ActionResult> Join(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            var room = await db.Rooms.Where(c => c.Name == name).SingleOrDefaultAsync();
            if (room == null)
            {
                return HttpNotFound();
            }

            // ユーザーを取得できれば取得
            var viewModel = new StatusMessageViewModel<Room>();
            var appUser = await GetApplicationUser();
            if (!room.IsLive)
            {
                if (appUser == null || !appUser.OwnerRooms.Contains(room))
                {
                    // まだ入れませんよ
                    viewModel.Message = "現在ルームはオフラインです。管理者以外は入室できません。";
                    viewModel.Type = MessageType.Error;
                    return View(viewModel);
                }
            }

            var userName = appUser == null ? "Anonymous" : appUser.UserName;
            bool isBroadcaster = (appUser != null && room.Owner.Id == appUser.Id);

            if (room.IsPrivate && !isBroadcaster)
            {
                // 認証する
                viewModel.Type = MessageType.Warning;
                viewModel.Message = "このルームは認証が必要です";
                return View(viewModel);
            }

            // アクセスを許可 -> Tokenを発行
            var instance = TokenManager.GetInstance();
            var token = instance.CreateToken(room.Id);
            TempData["token"] = token;

            return RedirectToAction("Live", new { name = name });
        }

        [HttpPost()]
        [ValidateAntiForgeryToken()]
        [AllowAnonymous()]
        public async Task<ActionResult> Join(string name, string auth)
        {
            if (string.IsNullOrEmpty(name))
                return RedirectToAction("Index", "Rooms");

            // Join
            var room = await (from item in db.Rooms where item.Name == name select item).SingleOrDefaultAsync();
            if (room == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // ユーザーを取得できれば取得
            var viewModel = new StatusMessageViewModel<Room>();

            var userId = User.Identity.GetUserId();
            var appUser = await db.Users.Where(user => user.Id == userId).SingleOrDefaultAsync();
            if (!room.IsLive)
            {
                if (appUser == null || !appUser.OwnerRooms.Contains(room))
                {
                    viewModel.Message = "現在ルームはオフラインです。管理者以外は入室できません。";
                    viewModel.Type = MessageType.Error;
                    return View(viewModel);
                }
            }

            var userName = appUser == null ? "Anonymous" : appUser.UserName;
            bool isBroadcaster = (appUser != null && room.Owner.Id == appUser.Id);

            if (room.IsPrivate && !isBroadcaster)
            {
                // 認証する
                if (!String.IsNullOrWhiteSpace(auth))
                {
                    string passCode = auth;
                    // 値が違う場合
                    if (passCode != room.AccessCode)
                    {
                        viewModel.Type = MessageType.Error;
                        viewModel.Message = "認証コードが違います";

                        return View(viewModel);
                    }
                }
                else
                {
                    viewModel.Type = MessageType.Warning;
                    viewModel.Message = "このルームは認証が必要です";
                    return View(viewModel);
                }
            }

            // アクセスを許可 -> Tokenを発行
            var tokenManager = TokenManager.GetInstance();
            var token = tokenManager.CreateToken(room.Id);
            await db.SaveChangesAsync();

            TempData["token"] = token;

            return RedirectToAction("Live", new { name = name });
        }


        [AllowAnonymous()]
        public async Task<ActionResult> Live(string name, string auth = null)
        {
            var authCode = "";

            var temp = TempData["token"] as string;
            if (!string.IsNullOrEmpty(temp))
                authCode = temp;

            // クエリストリングでも対応可能に
            if (auth != null)
                authCode = auth;

            var room = await db.Rooms.Where(c => c.Name == name).SingleOrDefaultAsync();
            if (room == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            if (string.IsNullOrEmpty(authCode))
            {
                if (string.IsNullOrEmpty(name))
                    return RedirectToAction("Index", "Rooms");

                // TODO: うまく動作するか確認
                return RedirectToAction("Join", new { name = name });
            }
            
            var instance = TokenManager.GetInstance();
            var tokenInfo = instance.GetTokenInfo(authCode);
            if (tokenInfo == null || tokenInfo != room.Id)
            {
                // TODO: エラーページを用意
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return View(new Tuple<string,Room>(authCode, room));
        }

        // GET: Rooms/Create
        [Authorize()]
        public ActionResult Create()
        {
            return View(new CreateRoomViewModel());
        }

        // POST: Rooms/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [Authorize()]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateRoomViewModel viewModel)
        {
            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var room = new Room()
            {
                CreatedAt = DateTime.Now,
                AccessCode = "Random Seed",
                Description = viewModel.Description,
                DisplayName = viewModel.DisplayName,
                IsHidden = viewModel.IsHidden,
                IsPrivate = viewModel.IsPrivate,
                Name = viewModel.Name,
                Owner = user,
                BroadcastToken = "Broadcast Token"
            };

            room.Owner = user;
            db.Rooms.Add(room);
            await db.SaveChangesAsync();
            return RedirectToAction("Details", new { name = room.Name });
        }

        // GET: Rooms/Manage/5
        [Authorize()]
        public async Task<ActionResult> Manage(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            var room = await db.Rooms.Where(c => c.Name == name).SingleOrDefaultAsync();
            if (room == null)
            {
                return HttpNotFound();
            }
            
            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (room.Owner.Id != user.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            return View(room);
        }


        // GET: Rooms/Action/5
        [Authorize()]
        [HttpPost()]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Action(string name, RoomAction action)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            var room = await db.Rooms.Where(c => c.Name == name).SingleOrDefaultAsync();
            if (room == null)
            {
                return HttpNotFound();
            }

            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (room.Owner.Id != user.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            if (RoomManager.GetInstance().GetRoomInfo(room.Id) != null)
            {
                switch (action)
                {
                    case RoomAction.StartBroadcast:
                        room.IsLive = true;
                        break;
                    case RoomAction.StopBroadcast:
                        room.IsLive = false;
                        break;
                }
            }

            await db.SaveChangesAsync();
            return RedirectToAction("Manage", new {name = name});
        }

        // GET: Rooms/Manage/5
        [Authorize()]
        public async Task<ActionResult> Settings(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            var room = await db.Rooms.Where(c => c.Name == name).SingleOrDefaultAsync();
            if (room == null)
            {
                return HttpNotFound();
            }

            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (room.Owner.Id != user.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            return View(room);
        }

        // POST: Rooms/Edit/5
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、http://go.microsoft.com/fwlink/?LinkId=317598 を参照してください。
        [HttpPost]
        [Authorize()]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Settings([Bind(Include = "DisplayName")] Room room)
        {
            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (!ModelState.IsValid)
            {
                return View(room);
            }

            if (room.Owner.Id != user.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            // 変更をCommit
            db.Entry(room).State = EntityState.Modified;

            await db.SaveChangesAsync();
            return RedirectToAction("Manage", new {name = room.Name});
        }

        // GET: Rooms/Delete/5
        [Authorize()]
        public async Task<ActionResult> Delete(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            var room = await db.Rooms.Where(c => c.Name == name).SingleOrDefaultAsync();
            if (room == null)
            {
                return HttpNotFound();
            }

            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (room.Owner.Id != user.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize()]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            var room = await db.Rooms.Where(c => c.Name == name).SingleOrDefaultAsync();
            if (room == null)
            {
                return HttpNotFound();
            }

            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (room.Owner.Id != user.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            db.Rooms.Remove(room);
            db.SaveChanges();
            return RedirectToAction("Index");
        }



        private async Task<ApplicationUser> GetApplicationUser()
        {
            var userId = User.Identity.GetUserId();
            var appUser = await db.Users.Where(user => user.Id == userId).SingleOrDefaultAsync();
            return appUser;
        }


        [Authorize()]
        public async Task<ActionResult> IsRoomNameAvailable(string name)
        {
            var room = await db.Rooms.Where(m => m.Name == name).SingleOrDefaultAsync();
            return Json(room == null, JsonRequestBehavior.AllowGet);
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
