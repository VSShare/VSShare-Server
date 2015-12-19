using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Migrations;
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
    [Authorize()]
    public class RoomsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        private const int PagingItemCount = 20;

        // GET: Rooms
        [AllowAnonymous()]
        public async Task<ViewResult> Index(string filter = null, SortType sort = SortType.Created, bool isLive = false, int page = 1)
        {
            var viewModel = new PagingItemViewModel<Room>()
            {
                IsLive = false
            };

            IQueryable<Room> items = db.Rooms.Where(c => !c.IsHidden);

            if (filter != null)
            {
                items = items.Where(c => c.DisplayName.Contains(filter));
                viewModel.Query = filter;
            }

            if (isLive)
            {
                items = items.Where(c => c.IsLive);
                viewModel.IsLive = true;
            }

            switch (sort)
            {
                case SortType.Visitor:
                    items = items.OrderByDescending(c => c.TotalVisitor);
                    viewModel.SortType = SortType.Visitor;
                    break;
                case SortType.Created:
                default:
                    items = items.OrderByDescending(c => c.CreatedAt);
                    viewModel.SortType = SortType.Created;
                    break;
            }

            // トータル投稿数
            viewModel.TotalCount = await items.CountAsync();

            // トータルページ数
            viewModel.TotalPages = (viewModel.TotalCount - 1) / PagingItemCount + 1;
            if (viewModel.TotalPages < page)
            {
                viewModel.CurrentPage = 1;
            }
            else
            {
                viewModel.CurrentPage = page;
            }

            // StartPageのNumber
            viewModel.StartPage = viewModel.CurrentPage - PagingItemCount;
            if (viewModel.StartPage < 1)
                viewModel.StartPage = 1;

            // EndPageのNumber
            viewModel.EndPage = viewModel.CurrentPage + PagingItemCount - 1;
            if (viewModel.EndPage > viewModel.TotalPages)
                viewModel.EndPage = viewModel.TotalPages;

            // 最初のn件だけ要素を取得
            viewModel.Results = await items.Skip((viewModel.CurrentPage - 1) * PagingItemCount).Take(PagingItemCount).ToListAsync();

            return View(viewModel);
        }

        // GET: Rooms/Details/5
        [AllowAnonymous()]
        public async Task<ActionResult> Details(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
            if (room == null)
            {
                return HttpNotFound();
            }

            var user = await GetApplicationUser();
            if (user != null && user.Id == room.Owner.Id)
            {
                ViewBag.IsOwner = true;
            }
            else
            {
                ViewBag.IsOwner = false;
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

            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
            if (room == null)
            {
                return HttpNotFound();
            }

            // ユーザーを取得できれば取得
            var viewModel = new StatusMessageViewModel<JoinRoomViewModel>()
            {
                Item = new JoinRoomViewModel() {Room = room}
            };

            var appUser = await GetApplicationUser();
            bool isBroadcaster = (appUser != null && room.Owner.Id == appUser.Id);
            viewModel.Item.IsBroadcaster = isBroadcaster;

            if (!room.IsLive)
            {
                if (appUser == null || !appUser.OwnerRooms.Contains(room))
                {
                    // まだ入れませんよ
                    viewModel.Message = "現在ルームはオフラインです。管理者以外は入室できません。";
                    viewModel.Type = MessageType.Error;
                    return View(viewModel);
                }
                else if (isBroadcaster)
                {
                    var instance = RoomManager.GetInstance();
                    if (instance.GetRoomInfo(room.Id) == null)
                    {
                        // まだ入れませんよ
                        viewModel.Message = "あなたはこのルームの管理者です。ルームに入室するには、まずエディタからログインしてセッションを確立してください。";
                        viewModel.Type = MessageType.Error;
                        return View(viewModel);
                    }
                }
            }

            viewModel.Item.CanJoin = true;

            if (room.IsPrivate && !isBroadcaster)
            {
                // 認証する
                viewModel.Type = MessageType.Warning;
                viewModel.Message = "このルームは認証が必要です";
                viewModel.Item.CanJoin = true;
                return View(viewModel);
            }

            return View(viewModel);
        }

        [AllowAnonymous()]
        public async Task<ActionResult> JoinEmbedded(string name)
        {
            return await Join(name);
        }

        [HttpPost()]
        [ValidateAntiForgeryToken()]
        [AllowAnonymous()]
        public async Task<ActionResult> Join(string name, string auth)
        {
            if (string.IsNullOrEmpty(name))
                return RedirectToAction("Index", "Rooms");

            // Join
            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
            if (room == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // ユーザーを取得できれば取得
            var viewModel = new StatusMessageViewModel<JoinRoomViewModel>()
            {
                Item = new JoinRoomViewModel() { Room = room}
            };
            
            var appUser = await GetApplicationUser();
            bool isBroadcaster = (appUser != null && room.Owner.Id == appUser.Id);
            viewModel.Item.IsBroadcaster = isBroadcaster;

            if (!room.IsLive)
            {
                if (appUser == null || !appUser.OwnerRooms.Contains(room))
                {
                    viewModel.Message = "現在ルームはオフラインです。管理者以外は入室できません。";
                    viewModel.Type = MessageType.Error;
                    return View(viewModel);
                }
                else if (isBroadcaster)
                {
                    var instance = RoomManager.GetInstance();
                    if (instance.GetRoomInfo(room.Id) == null)
                    {
                        // まだ入れませんよ
                        viewModel.Message = "あなたはこのルームの管理者です。ルームに入室するには、まずエディタからログインしてセッションを確立してください。";
                        viewModel.Type = MessageType.Error;
                        return View(viewModel);
                    }
                }
            }

            viewModel.Item.CanJoin = true;

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

        [ValidateAntiForgeryToken()]
        [HttpPost()]
        [AllowAnonymous()]
        public async Task<ActionResult> JoinEmbedded(string name, string auth)
        {

            if (string.IsNullOrEmpty(name))
                return RedirectToAction("Index", "Rooms");

            // Join
            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
            if (room == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // ユーザーを取得できれば取得
            var viewModel = new StatusMessageViewModel<JoinRoomViewModel>()
            {
                Item = new JoinRoomViewModel() { Room = room }
            };

            var appUser = await GetApplicationUser();
            bool isBroadcaster = (appUser != null && room.Owner.Id == appUser.Id);
            viewModel.Item.IsBroadcaster = isBroadcaster;

            if (!room.IsLive)
            {
                if (appUser == null || !appUser.OwnerRooms.Contains(room))
                {
                    viewModel.Message = "現在ルームはオフラインです。管理者以外は入室できません。";
                    viewModel.Type = MessageType.Error;
                    return View(viewModel);
                }
                else if (isBroadcaster)
                {
                    var instance = RoomManager.GetInstance();
                    if (instance.GetRoomInfo(room.Id) == null)
                    {
                        // まだ入れませんよ
                        viewModel.Message = "あなたはこのルームの管理者です。ルームに入室するには、まずエディタからログインしてセッションを確立してください。";
                        viewModel.Type = MessageType.Error;
                        return View(viewModel);
                    }
                }
            }

            viewModel.Item.CanJoin = true;

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

            return RedirectToAction("LiveEmbedded", new { name = name });
        }

        [AllowAnonymous()]
        public async Task<ActionResult> Live(string name, string token = null)
        {
            var authCode = "";

            var temp = TempData["token"] as string;
            if (!string.IsNullOrEmpty(temp))
                authCode = temp;

            // クエリストリングでも対応可能に
            if (token != null)
                authCode = token;

            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
            if (room == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            if (string.IsNullOrEmpty(authCode))
            {
                if (string.IsNullOrEmpty(name))
                    return RedirectToAction("Index", "Rooms");

                return RedirectToAction("Join", new { name = name });
            }
            
            var instance = TokenManager.GetInstance();
            var tokenInfo = instance.GetTokenInfo(authCode);
            if (tokenInfo == null || tokenInfo != room.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return View(new Tuple<string,Room>(authCode, room));
        }

        [AllowAnonymous()]
        public async Task<ActionResult> LiveEmbedded(string name, string token = null)
        {
            var authCode = "";

            var temp = TempData["token"] as string;
            if (!string.IsNullOrEmpty(temp))
                authCode = temp;

            // クエリストリングでも対応可能に
            if (token != null)
                authCode = token;

            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
            if (room == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            if (string.IsNullOrEmpty(authCode))
            {
                if (string.IsNullOrEmpty(name))
                    return RedirectToAction("Index", "Rooms");

                return RedirectToAction("JoinEmbedded", new { name = name });
            }

            var instance = TokenManager.GetInstance();
            var tokenInfo = instance.GetTokenInfo(authCode);
            if (tokenInfo == null || tokenInfo != room.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return View(new Tuple<string, Room>(authCode, room));
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
                LatestBroadcastDate = DateTime.Now,
                AccessCode = Guid.NewGuid().ToString(),
                Description = viewModel.Description,
                DisplayName = viewModel.DisplayName,
                IsHidden = viewModel.IsHidden,
                IsPrivate = viewModel.IsPrivate,
                Name = viewModel.Name,
                Id = Guid.NewGuid().ToString(),
                Owner = user,
                BroadcastToken = Guid.NewGuid().ToString()
            };
            db.Rooms.AddOrUpdate(room);

            await db.SaveChangesAsync();
            return RedirectToAction("Details", new { name = room.Name });
        }

        [HttpPost()]
        [ValidateAntiForgeryToken()]
        public async Task<ActionResult> ResetBroadcastToken(string name)
        {

            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var model = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
            if (model.Owner.Id != user.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            model.BroadcastToken = Guid.NewGuid().ToString();
            await db.SaveChangesAsync();

            return RedirectToAction("Manage", new {name = name});
        }

        // GET: Rooms/Manage/5
        [Authorize()]
        public async Task<ActionResult> Manage(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
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

            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
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
            return RedirectToAction("Details", new {name = name});
        }

        // GET: Rooms/Manage/5
        [Authorize()]
        public async Task<ActionResult> Settings(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
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
        public async Task<ActionResult> Settings(Room room)
        {
            var user = await GetApplicationUser();
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            ModelState.Remove("BroadcastToken");
            ModelState.Remove("Owner");
            if (!ModelState.IsValid)
            {
                return View(room);
            }

            var model = await db.Rooms.FirstOrDefaultAsync(c => c.Name == room.Name);
            if (model.Owner.Id != user.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            model.DisplayName = room.DisplayName;
            model.Description = room.Description;
            model.AccessCode = room.AccessCode;
            model.IsHidden = room.IsHidden;
            model.IsPrivate = room.IsPrivate;

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

            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
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

            var room = await db.Rooms.FirstOrDefaultAsync(c => c.Name == name);
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
            var appUser = await db.Users.FirstOrDefaultAsync(user => user.Id == userId);
            return appUser;
        }


        [Authorize()]
        public async Task<ActionResult> IsRoomNameAvailable(string name)
        {
            var room = await db.Rooms.FirstOrDefaultAsync(m => m.Name == name);
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
