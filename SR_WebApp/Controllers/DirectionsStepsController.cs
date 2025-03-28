using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using DataTables;
using SR_WebApp.Models;
using SR_WebApp.Properties;
using Database = DataTables.Database;

namespace SR_WebApp.Controllers
{
    public class DirectionsStepsController : ApiController
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: api/FloorDirectory/DirectionsSteps/1
        [Route("api/FloorDirectory/DirectionsSteps/{floorDirectoryId}")]
        [HttpGet]
        [HttpPost]
        public IHttpActionResult GetDirectionsStep(int floorDirectoryId)
        {
            var request = HttpContext.Current.Request;
            var settings = Settings.Default;

            using (var db = new Database(settings.DbType, settings.DbConnection))
            {
                var response = new Editor(db, "DirectionsSteps", "Id")
                    .Model<Models.DirectionsStep>("DirectionsSteps")
                    .Model<JoinModelFloorDirectory>("FloorDirectory")
                    .Field(new Field("DirectionsSteps.FloorDirectoryId")
                        .Options(new Options()
                            .Table("FloorDirectory")
                            .Value("Id")
                            .Label(new[] { "FacilityCode", "FacilityDescription" })
                            .Where(q => q.Where("Id", floorDirectoryId))
                            .Render(row => $"{row["FacilityCode"]} - {row["FacilityDescription"]}")
                        )
                        .Validator(Validation.DbValues())
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Step instrutions is required" }
                        ))
                    )
                    .Field(new Field("DirectionsSteps.StepInstructions")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Step instrutions is required" }
                        ))
                        .Validator(Validation.MaxLen(67,
                            new ValidationOpts { Message = "Step instructions must not exceed 67 characters" }
                        ))
                    )
                    .Field(new Field("DirectionsSteps.StepAction")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Step action is required" }
                        ))
                        .Validator(Validation.MaxLen(17,
                            new ValidationOpts { Message = "Step action must not exceed 17 characters" }
                        ))
                    )
                    .Field(new Field("DirectionsSteps.StepActionImageId")
                        .Upload(
                            new Upload(request.PhysicalApplicationPath + @"assets\images\stepactions\__ID____EXTN__")
                                .Db("StepActionImages", "Id", new Dictionary<string, object>
                                {
                                    {"FileName", Upload.DbType.FileName},
                                    {"FileSize", Upload.DbType.FileSize},
                                    {"WebPath", Upload.DbType.WebPath},
                                    {"SystemPath", Upload.DbType.SystemPath}
                                })
                                .DbClean(data =>
                                {
                                    foreach (var row in data)
                                    {
                                        File.Delete(row["SystemPath"].ToString());
                                    }

                                    return true;
                                })
                                .Validator(Validation.FileExtensions(
                                    new[] { "png", "jpg" },
                                    "Only image files can be uploaded (png and jpg)"
                                ))
                        )
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Step action image is required" }
                        ))
                    )
                    .Field(new Field("DirectionsSteps.ContentDescription")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Content description is required" }
                        ))
                    )
                    .LeftJoin("FloorDirectory", "DirectionsSteps.FloorDirectoryId", "=", "FloorDirectory.Id")
                    .Where("FloorDirectoryId", floorDirectoryId)
                    .Process(request)
                    .Data();

                return Json(response);
            }
        }

        // GET: api/DirectionsSteps/L322
        [Route("api/DirectionsSteps/{facilityCode}")]
        [ResponseType(typeof(DirectionsStepModel))]
        public async Task<IHttpActionResult> GetDirectionsStepModel(string facilityCode)
        {
            List<DirectionsStepModel> directionsStepModel = await _db.DirectionsStepModels
                .Where(s => s.FloorDirectory.FacilityCode == facilityCode).ToListAsync();

            if (!directionsStepModel.Any())
            {
                return NotFound();
            }

            var directionsSteps = new List<DirectionsStep>();
            foreach (var t in directionsStepModel)
            {
                var directionsStep = new DirectionsStep
                {
                    StepInstructions = t.StepInstructions,
                    StepAction = t.StepAction,
                    StepActionImage = t.StepActionImages.WebPath.Replace(@"\", @"/"),
                    ContentDescription = t.ContentDescription
                };
                directionsSteps.Add(directionsStep);
            }

            return Json(directionsSteps);
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

    public class DirectionsStep
    {
        public string StepInstructions { get; set; }

        public string StepAction { get; set; }

        public string StepActionImage { get; set; }

        public string ContentDescription { get; set; }
    }
}