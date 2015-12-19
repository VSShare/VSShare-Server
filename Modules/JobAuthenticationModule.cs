using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Server.Modules
{
    public class JobAuthenticationModule : IHttpModule
    {
        private string _sharedKey = null;
        const string JobUserSharedKey = "SchedulerSecretKey";

        public void Init(HttpApplication context)
        {
            this._sharedKey = LoadSharedKey();
            context.AuthenticateRequest += AuthenticateScheduler;
        }

        private static string LoadSharedKey()
        {
            if (ConfigurationManager.AppSettings.AllKeys.Contains(JobUserSharedKey))
            {
                return ConfigurationManager.AppSettings[JobUserSharedKey];
            }
            return default(string);
        }

        void AuthenticateScheduler(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var request = new HttpRequestWrapper(application.Request);

            // スケジューラからのリクエストか検証
            if (!request.Headers.AllKeys.Contains("x-ms-scheduler-jobid"))
            return;

            // スケジューラからのリクエストの場合は、シークレットキーを認識してRoleを付与
            AuthenticateUsingSharedSecret(request);
        }

        private void AuthenticateUsingSharedSecret(HttpRequestBase request)
        {
            using (var sr = new StreamReader(request.GetBufferedInputStream(), request.ContentEncoding))
            {
                var bodyContent = sr.ReadToEnd();
                if (!bodyContent.StartsWith("secret:"))
                    return;

                var secret = bodyContent.Replace("secret:", string.Empty).Trim();
                if (secret != this._sharedKey)
                    return;
            }
            // secretトークンが一致
            CreateClaimsForScheduler();
        }

        private static void CreateClaimsForScheduler()
        {
            var nameIdClaim = new Claim(ClaimTypes.NameIdentifier, "job");
            var schedulerRoleClaim = new Claim(ClaimTypes.Role, "job");
            var identificatorClaim =
                new Claim(
                    "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider",
                    "application");

            var claimIdentity = new ClaimsIdentity(new List<Claim>
                {
                    nameIdClaim,
                    schedulerRoleClaim,
                    identificatorClaim
                }, "custom");

            var principal = new ClaimsPrincipal(claimIdentity);

            Thread.CurrentPrincipal = principal;
            HttpContext.Current.User = Thread.CurrentPrincipal;
        }


        public void Dispose()
        {
        }
    }
}
