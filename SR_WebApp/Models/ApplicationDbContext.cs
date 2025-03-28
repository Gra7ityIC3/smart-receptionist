using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Web;

namespace SR_WebApp.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<UserTokenCache> UserTokenCacheList { get; set; }

        public DbSet<StaffModel> StaffModels { get; set; }

        public DbSet<EchoDeviceModel> EchoDeviceModels { get; set; }

        public DbSet<DirectionsStepModel> DirectionsStepModels { get; set; }

        public DbSet<FloorDirectoryModel> FloorDirectoryModels { get; set; }

        public DbSet<StepActionImageModel> StepActionImageModels { get; set; }

        public DbSet<LostItemModel> LostItemModels { get; set; }
    }

    public class UserTokenCache
    {
        [Key]
        public int UserTokenCacheId { get; set; }
        public string webUserUniqueId { get; set; }
        public byte[] cacheBits { get; set; }
        public DateTime LastWrite { get; set; }
    }
}
