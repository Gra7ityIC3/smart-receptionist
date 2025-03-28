using DataTables;
using SR_WebApp.Models;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using Database = DataTables.Database;

namespace SR_WebApp.Controllers
{
    public class LostItemsController : ApiController
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: api/LostItemsTable
        [Route("api/LostItemsTable")]
        [HttpGet]
        [HttpPost]
        public IHttpActionResult LostItemsTable()
        {
            var request = HttpContext.Current.Request;
            var settings = Properties.Settings.Default;

            using (var db = new Database(settings.DbType, settings.DbConnection))
            {
                var response = new Editor(db, "LostItems", "Id")
                    .Model<LostItem>()
                    .Field(new Field("Name")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Name is required" }
                        ))
                    )
                    .Field(new Field("PhoneNumber")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Phone number is required" }
                        ))
                        .Validator(Validation.MinMaxLen(8, 15,
                            new ValidationOpts { Message = "This phone number format is not recognized. Please check the number." }
                        ))
                    )
                    .Field(new Field("ItemDescription")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Item description is required" }
                        ))
                    )
                    .Field(new Field("LocationLost")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Location lost is required" }
                        ))
                    )
                    .Field(new Field("DateLost")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Date lost is required" }
                        ))
                        .Validator(Validation.DateFormat(
                            Format.DATE_ISO_8601,
                            new ValidationOpts { Message = "Please enter a date in the format YYYY-MM-DD" }
                        ))
                        .GetFormatter(Format.DateSqlToFormat(Format.DATE_ISO_8601))
                        .SetFormatter(Format.DateFormatToSql(Format.DATE_ISO_8601))
                    )
                    .Field(new Field("TimeLost")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Time lost is required" }
                        ))
                    )
                    .Field(new Field("Status")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Status is required" }
                        ))
                        .Validator(Validation.MaxLen(20,
                            new ValidationOpts { Message = "The status must not exceed 20 characters" }
                        ))
                    )
                    .Process(request)
                    .Data();

                return Json(response);
            }
        }

        // GET: api/LostItems/5
        [ResponseType(typeof(LostItemModel))]
        public async Task<IHttpActionResult> GetLostItemModel(int id)
        {
            LostItemModel lostItemModel = await _db.LostItemModels.FindAsync(id);
            if (lostItemModel == null)
            {
                return NotFound();
            }

            return Ok(lostItemModel);
        }

        // POST: api/LostItems
        [ResponseType(typeof(LostItemModel))]
        public async Task<IHttpActionResult> PostLostItemModel(LostItemModel lostItemModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _db.LostItemModels.Add(lostItemModel);
            await _db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = lostItemModel.Id }, lostItemModel);
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
}