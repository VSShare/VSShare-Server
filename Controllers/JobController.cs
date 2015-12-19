using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ApplicationInsights;
using Server.Models;
using Server.Models.Manager;

namespace Server.Controllers
{
    [Route("job")]
    [Authorize(Roles = "job")]
    [AiHandleError()]
    public class JobController : ApiController
    {
        public HttpResponseMessage RunJob()
        {
            var telemetry = new TelemetryClient();

            try
            {
                // トークンの削除
                var manager = TokenManager.GetInstance();
                manager.CleanExpiredToken();

                // 放送していないルームの削除
                using (var db = new ApplicationDbContext())
                {
                    var instance = RoomManager.GetInstance();
                    var liveRoom = db.Rooms.Where(c => c.IsLive);
                    foreach (var item in liveRoom)
                    {
                        if (instance.GetRoomInfo(item.Id) == null)
                        {
                            item.IsLive = true;
                        }
                    }
                    db.SaveChanges();
                }
                return Request.CreateResponse(HttpStatusCode.Accepted);
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex, new Dictionary<string, string>() { { "Method", "RunJob" } }, null);
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

    }
}
