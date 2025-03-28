using DataTables;
using SR_WebApp.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using Database = DataTables.Database;

namespace SR_WebApp.Controllers
{
    public class StaffController : ApiController
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: api/Staff
        [Route("api/Staff")]
        [HttpGet]
        [HttpPost]
        public IHttpActionResult GetStaffModels()
        {
            var request = HttpContext.Current.Request;
            var settings = Properties.Settings.Default;

            using (var db = new Database(settings.DbType, settings.DbConnection))
            {
                var response = new Editor(db, "Staff", "Id")
                    .Model<Models.Staff>("Staff")
                    .Model<JoinModelEchoDevice>("EchoDevices")
                    .Field(new Field("Staff.Name")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Name is required" }
                        ))
                        .Validator(Validation.MaxLen(100,
                            new ValidationOpts { Message = "Must have at most 100 characters" }
                        ))
                    )
                    .Field(new Field("Staff.Email")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Email is required" }
                        ))
                        .Validator(Validation.Email(
                            new ValidationOpts { Message = "Enter a valid email" }
                        ))
                        .Validator(Validation.Unique(
                            new ValidationOpts { Message = "That email is already being used" }
                        ))
                    )
                    .Field(new Field("Staff.OfficeNumber")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Office number is required" }
                        ))
                        .Validator(Validation.MinMaxLen(8, 15,
                            new ValidationOpts { Message = "This phone number format is not recognized. Please check the number." }
                        ))
                        .Validator(Validation.Unique(
                            new ValidationOpts { Message = "That number is already being used" }
                        ))
                    )
                    .Field(new Field("Staff.PhoneNumber")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Phone number is required" }
                        ))
                        .Validator(Validation.MinMaxLen(8, 15,
                            new ValidationOpts { Message = "This phone number format is not recognized. Please check the number." }
                        ))
                        .Validator(Validation.Unique(
                            new ValidationOpts { Message = "That number is already being used" }
                        ))
                    )
                    .Field(new Field("Staff.EchoDeviceId")
                        .Options(new Options()
                            .Table("EchoDevices")
                            .Value("Id")
                            .Label("Name")
                        )
                        .Validator(Validation.DbValues())
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Echo device is required" }
                        ))
                    )
                    .LeftJoin("EchoDevices", "Staff.EchoDeviceId", "=", "EchoDevices.Id")
                    .Process(request)
                    .Data();

                return Json(response);
            }
        }

        // GET: api/Staff/Zhang Yi
        [Route("api/Staff/{name}")]
        [ResponseType(typeof(StaffModel))]
        public async Task<IHttpActionResult> GetStaffModel(string name)
        {
            const string sql =
                "SELECT CT.[KEY], CT.RANK, Staff.Name, PhoneNumber, Status, EchoDevices.Name EchoDeviceName " +
                "FROM CONTAINSTABLE(dbo.Staff, Name, @name) CT " +
                "LEFT JOIN dbo.Staff ON CT.[KEY] = Staff.Id " +
                "LEFT JOIN dbo.EchoDevices ON Staff.EchoDeviceId = EchoDevices.Id";
            List<Staff> staff = await _db.Database.SqlQuery<Staff>(sql, new SqlParameter("name", $"\"{name}\""))
                .ToListAsync();
            if (!staff.Any())
            {
                return NotFound();
            }

            return Json(staff);
        }

        // GET: api/Staff/L.315
        [Route("api/Staff/GetStaffResult/{echoDeviceLocation}")]
        [ResponseType(typeof(StaffModel))]
        public async Task<IHttpActionResult> GetStaffResult(string echoDeviceLocation)
        {
            StaffModel staffModel = await _db.StaffModels.FirstOrDefaultAsync(s =>
                s.EchoDevices.Location == echoDeviceLocation && s.Status == "Available");
            if (staffModel == null)
            {
                return NotFound();
            }

            var staff = new Staff
            {
                EchoDeviceName = staffModel.EchoDevices.Name
            };
            return Json(staff);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class Staff
    {
        public int Key { get; set; }

        public int Rank { get; set; }

        public string Name { get; set; }

        public string PhoneNumber { get; set; }

        public string Status { get; set; }

        public string EchoDeviceName { get; set; }
    }
}