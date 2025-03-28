namespace SR_WebApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DirectionsSteps",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FloorDirectoryId = c.Int(nullable: false),
                        StepInstructions = c.String(nullable: false, maxLength: 67),
                        StepAction = c.String(nullable: false, maxLength: 17),
                        StepActionImageId = c.Int(nullable: false),
                        ContentDescription = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.FloorDirectory", t => t.FloorDirectoryId, cascadeDelete: true)
                .ForeignKey("dbo.StepActionImages", t => t.StepActionImageId, cascadeDelete: true)
                .Index(t => t.FloorDirectoryId)
                .Index(t => t.StepActionImageId);
            
            CreateTable(
                "dbo.FloorDirectory",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FacilityCode = c.String(nullable: false, maxLength: 5),
                        FacilityDescription = c.String(nullable: false),
                        FacilityAbbreviation = c.String(nullable: false, maxLength: 10),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.FacilityCode, unique: true);
            
            CreateTable(
                "dbo.StepActionImages",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FileName = c.String(nullable: false, maxLength: 50),
                        FileSize = c.Int(nullable: false),
                        WebPath = c.String(nullable: false, maxLength: 50),
                        SystemPath = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.EchoDevices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SerialNumber = c.String(nullable: false, maxLength: 16),
                        Name = c.String(nullable: false),
                        Location = c.String(nullable: false),
                        Model = c.String(nullable: false),
                        EchoDeviceImageId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.EchoDevicesImages", t => t.EchoDeviceImageId)
                .Index(t => t.EchoDeviceImageId);
            
            CreateTable(
                "dbo.EchoDevicesImages",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FileName = c.String(nullable: false, maxLength: 50),
                        FileSize = c.Int(nullable: false),
                        WebPath = c.String(nullable: false, maxLength: 50),
                        SystemPath = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.LostItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        PhoneNumber = c.String(nullable: false, maxLength: 15),
                        ItemDescription = c.String(nullable: false),
                        LocationLost = c.String(nullable: false),
                        DateLost = c.DateTime(nullable: false, storeType: "date"),
                        TimeLost = c.String(nullable: false),
                        Status = c.String(nullable: false, maxLength: 20, defaultValue: "Missing"),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Staff",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 250),
                        OfficeNumber = c.String(nullable: false, maxLength: 15),
                        PhoneNumber = c.String(nullable: false, maxLength: 15),
                        Status = c.String(nullable: false, maxLength: 100, defaultValue: "Available"),
                        EchoDeviceId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.EchoDevices", t => t.EchoDeviceId, cascadeDelete: true)
                .Index(t => t.EchoDeviceId);
            
            CreateTable(
                "dbo.UserTokenCaches",
                c => new
                    {
                        UserTokenCacheId = c.Int(nullable: false, identity: true),
                        webUserUniqueId = c.String(),
                        cacheBits = c.Binary(),
                        LastWrite = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.UserTokenCacheId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Staff", "EchoDeviceId", "dbo.EchoDevices");
            DropForeignKey("dbo.EchoDevices", "EchoDeviceImageId", "dbo.EchoDevicesImages");
            DropForeignKey("dbo.DirectionsSteps", "StepActionImageId", "dbo.StepActionImages");
            DropForeignKey("dbo.DirectionsSteps", "FloorDirectoryId", "dbo.FloorDirectory");
            DropIndex("dbo.Staff", new[] { "EchoDeviceId" });
            DropIndex("dbo.EchoDevices", new[] { "EchoDeviceImageId" });
            DropIndex("dbo.FloorDirectory", new[] { "FacilityCode" });
            DropIndex("dbo.DirectionsSteps", new[] { "StepActionImageId" });
            DropIndex("dbo.DirectionsSteps", new[] { "FloorDirectoryId" });
            DropTable("dbo.UserTokenCaches");
            DropTable("dbo.Staff");
            DropTable("dbo.LostItems");
            DropTable("dbo.EchoDevicesImages");
            DropTable("dbo.EchoDevices");
            DropTable("dbo.StepActionImages");
            DropTable("dbo.FloorDirectory");
            DropTable("dbo.DirectionsSteps");
        }
    }
}
