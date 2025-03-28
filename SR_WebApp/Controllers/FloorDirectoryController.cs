using DataTables;
using SR_WebApp.Models;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using Database = DataTables.Database;

namespace SR_WebApp.Controllers
{
    public class FloorDirectoryController : ApiController
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: api/FloorDirectory
        [HttpGet]
        [HttpPost]
        public IHttpActionResult GetFloorDirectoryModels()
        {
            var request = HttpContext.Current.Request;
            var settings = Properties.Settings.Default;

            using (var db = new Database(settings.DbType, settings.DbConnection))
            {
                var response = new Editor(db, "FloorDirectory", "Id")
                    .Model<FloorDirectory>()
                    .Field(new Field("FacilityCode")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Facility code is required" }
                        ))
                        .Validator(Validation.MinLen(4,
                            new ValidationOpts { Message = "The facility code must be at least 4 characters" }
                        ))
                        .Validator(Validation.Unique(
                            new ValidationOpts { Message = "That facility code already exists" }
                        ))
                    )
                    .Field(new Field("FacilityAbbreviation")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Facility abbreviation is required" }
                        ))
                    )
                    .Field(new Field("FacilityDescription")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Facility description is required" }
                        ))
                    )
                    .Process(request)
                    .Data();

                return Json(response);
            }
        }

        // GET: api/FloorDirectory/L322
        [Route("api/FloorDirectory/{facilityCode}")]
        [ResponseType(typeof(FloorDirectoryModel))]
        public async Task<IHttpActionResult> GetFloorDirectoryResult(string facilityCode)
        {
            bool result = await _db.FloorDirectoryModels.AnyAsync(f => f.FacilityCode == facilityCode);
            if (!result)
            {
                return NotFound();
            }

            return Json(true);
        }
    }
}
