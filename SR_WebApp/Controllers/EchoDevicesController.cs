using DataTables;
using SR_WebApp.Models;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Http;

namespace SR_WebApp.Controllers
{
    public class EchoDevicesController : ApiController
    {
        [Route("api/EchoDevices")]
        [HttpGet]
        [HttpPost]
        public IHttpActionResult GetEchoDevices()
        {
            var request = HttpContext.Current.Request;
            var settings = Properties.Settings.Default;

            using (var db = new Database(settings.DbType, settings.DbConnection))
            {
                var response = new Editor(db, "EchoDevices", "Id")
                    .Model<EchoDevice>()
                    .Field(new Field("SerialNumber")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Serial number is required" }
                        ))
                        .Validator(Validation.MinMaxLen(16, 16,
                            new ValidationOpts { Message = "The serial number must be 16 characters" }
                        ))
                        .Validator(Validation.Unique(
                            new ValidationOpts { Message = "That serial number is already being used" }
                        ))
                    )
                    .Field(new Field("Name")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Device name is required" }
                        ))
                        .Validator(Validation.Unique(
                            new ValidationOpts { Message = "That device name is taken. Try another." }
                        ))
                    )
                    .Field(new Field("Location")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Device location is required" }
                        ))
                    )
                    .Field(new Field("Model")
                        .Validator(Validation.Required(
                            new ValidationOpts { Message = "Device model is required" }
                        ))
                    )
                    .Field(new Field("EchoDeviceImageId")
                        .Upload(
                            new Upload(request.PhysicalApplicationPath + @"assets\images\echodevices\__ID____EXTN__")
                                .Db("EchoDevicesImages", "Id", new Dictionary<string, object>
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
                    )
                    .Process(request)
                    .Data();

                return Json(response);
            }
        }
    }
}
