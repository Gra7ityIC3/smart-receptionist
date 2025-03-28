using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SR_WebApp.Models
{
    [Table("Staff")]
    public class StaffModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(250)]
        public string Email { get; set; }

        [Required]
        [MaxLength(15)]
        public string OfficeNumber { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string Status { get; set; }

        [ForeignKey("EchoDevices")]
        public int EchoDeviceId { get; set; }

        public virtual EchoDeviceModel EchoDevices { get; set; }
    }

    [Table("EchoDevices")]
    public class EchoDeviceModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(16)]
        public string SerialNumber { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string Model { get; set; }

        [ForeignKey("EchoDevicesImages")]
        public int? EchoDeviceImageId { get; set; }

        public virtual EchoDeviceImageModel EchoDevicesImages { get; set; }
    }

    [Table("EchoDevicesImages")]
    public class EchoDeviceImageModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string FileName { get; set; }

        [Required]
        public int FileSize { get; set; }

        [Required]
        [MaxLength(50)]
        public string WebPath { get; set; }

        [Required]
        public string SystemPath { get; set; }
    }

    [Table("FloorDirectory")]
    public class FloorDirectoryModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(5)]
        [Index(IsUnique = true)]
        public string FacilityCode { get; set; }

        [Required]
        public string FacilityDescription { get; set; }

        [Required]
        [MaxLength(10)]
        public string FacilityAbbreviation { get; set; }
    }

    [Table("DirectionsSteps")]
    public class DirectionsStepModel
    {
        public int Id { get; set; }

        [ForeignKey("FloorDirectory")]
        public int FloorDirectoryId { get; set; }

        [Required]
        [MaxLength(67)]
        public string StepInstructions { get; set; }

        [Required]
        [MaxLength(17)]
        public string StepAction { get; set; }

        [ForeignKey("StepActionImages")]
        public int StepActionImageId { get; set; }

        [Required]
        public string ContentDescription { get; set; }

        public virtual FloorDirectoryModel FloorDirectory { get; set; }

        public virtual StepActionImageModel StepActionImages { get; set; }
    }

    [Table("StepActionImages")]
    public class StepActionImageModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string FileName { get; set; }

        [Required]
        public int FileSize { get; set; }

        [Required]
        [MaxLength(50)]
        public string WebPath { get; set; }

        [Required]
        public string SystemPath { get; set; }
    }

    [Table("LostItems")]
    public class LostItemModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        [Required]
        public string ItemDescription { get; set; }

        [Required]
        public string LocationLost { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime DateLost { get; set; }

        [Required]
        public string TimeLost { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; }
    }

    public class Staff
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string OfficeNumber { get; set; }

        public string PhoneNumber { get; set; }

        public string Status { get; set; }

        public int? EchoDeviceId { get; set; }
    }

    public class EchoDevice
    {
        public int Id { get; set; }

        public string SerialNumber { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public string Model { get; set; }

        public int? EchoDeviceImageId { get; set; }
    }

    public class FloorDirectory
    {
        public int Id { get; set; }

        public string FacilityCode { get; set; }

        public string FacilityAbbreviation { get; set; }

        public string FacilityDescription { get; set; }
    }

    public class DirectionsStep
    {
        public int Id { get; set; }

        public int FloorDirectoryId { get; set; }

        public string StepInstructions { get; set; }

        public string StepAction { get; set; }

        public int StepActionImageId { get; set; }

        public string ContentDescription { get; set; }
    }

    public class LostItem
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string PhoneNumber { get; set; }

        public string ItemDescription { get; set; }

        public string LocationLost { get; set; }

        public DateTime DateLost { get; set; }

        public string TimeLost { get; set; }

        public string Status { get; set; }
    }

    public class JoinModelEchoDevice
    {
        public string Name { get; set; }
    }

    public class JoinModelFloorDirectory
    {
        public string FacilityCode { get; set; }
    }
}