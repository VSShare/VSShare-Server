using System;
using System.Configuration;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.Twitter;
using Owin;
using Server.Models;
using System.Security.Claims;

namespace Server
{
    public partial class Startup
    {
        // 認証設定の詳細については、http://go.microsoft.com/fwlink/?LinkId=301864 を参照してください
        public void ConfigureAuth(IAppBuilder app)
        {
            // 1 要求につき 1 インスタンスのみを使用するように DB コンテキスト、ユーザー マネージャー、サインイン マネージャーを構成します。
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // アプリケーションが Cookie を使用して、サインインしたユーザーの情報を格納できるようにします
            // また、サードパーティのログイン プロバイダーを使用してログインするユーザーに関する情報を、Cookie を使用して一時的に保存できるようにします
            // サインイン Cookie の設定
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    // ユーザーがログインするときにセキュリティ スタンプを検証するように設定します。
                    // これはセキュリティ機能の 1 つであり、パスワードを変更するときやアカウントに外部ログインを追加するときに使用されます。
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });            
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // 2 要素認証プロセスの中で 2 つ目の要素を確認するときにユーザー情報を一時的に保存するように設定します。
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // 2 つ目のログイン確認要素 (電話や電子メールなど) を記憶するように設定します。
            // このオプションをオンにすると、ログイン プロセスの中の確認の第 2 ステップは、ログインに使用されたデバイスに保存されます。
            // これは、ログイン時の「このアカウントを記憶する」オプションに似ています。
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // 次の行のコメントを解除して、サード パーティのログイン プロバイダーを使用したログインを有効にします
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");


            app.UseTwitterAuthentication(
                new TwitterAuthenticationOptions()
                {
                    ConsumerKey = ConfigurationManager.AppSettings["TwitterConsumerKey"],
                    ConsumerSecret = ConfigurationManager.AppSettings["TwitterConsumerSecret"],
                    Provider = new TwitterAuthenticationProvider
                    {
#pragma warning disable CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
                        OnAuthenticated = async context =>
                        {
                            context.Identity.AddClaim(new Claim("urn:tokens:twitter:accesstoken", context.AccessToken));
                            context.Identity.AddClaim(new Claim("urn:tokens:twitter:accesstokensecret",
                                context.AccessTokenSecret));
                        }
#pragma warning restore CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
                    },
                    BackchannelCertificateValidator = new CertificateSubjectKeyIdentifierValidator(new[]
                    {
                        "A5EF0B11CEC04103A34A659048B21CE0572D7D47",
                        "0D445C165344C1827E1D20AB25F40163D8BE79A5",
                        "7FD365A7C2DDECBBF03009F34339FA02AF333133",
                        "39A55D933676616E73A761DFA16A7E59CDE66FAD",
                        "4eb6d578499b1ccf5f581ead56be3d9b6744a5e5",
                        "5168FF90AF0207753CCCD9656462A212B859723B",
                        "B13EC36903F8BF4701D498261A0802EF63642BC3"
                    })
                });

            //app.UseFacebookAuthentication(
            //   appId: "",
            //   appSecret: "");

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = "",
            //    ClientSecret = ""
            //});
        }
    }
}