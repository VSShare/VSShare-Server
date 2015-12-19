using System.Collections.Generic;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Server.Models
{
    // ApplicationUser クラスにプロパティを追加することでユーザーのプロファイル データを追加できます。詳細については、http://go.microsoft.com/fwlink/?LinkID=317594 を参照してください。
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // authenticationType が CookieAuthenticationOptions.AuthenticationType で定義されているものと一致している必要があります
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // ここにカスタム ユーザー クレームを追加します
            return userIdentity;
        }

        public virtual ICollection<Room> OwnerRooms { get; set; }

        public virtual ICollection<UserAccessToken> AccessTokens { get; set; }


    }
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() : base()
        {
        }

        public ApplicationRole(string pRoleName) : base(pRoleName)
        {
        }
    }


    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Room>().HasKey(c => c.Id);
            modelBuilder.Entity<Room>().HasRequired(c => c.Owner).WithMany(n => n.OwnerRooms);
            modelBuilder.Entity<UserAccessToken>().HasRequired(c => c.User).WithMany(n => n.AccessTokens);
        }

        public IDbSet<Room> Rooms { get; set; }

        public IDbSet<UserAccessToken> AccessTokens { get; set; }

        public System.Data.Entity.DbSet<Server.Models.Document> Documents { get; set; }
    }
}