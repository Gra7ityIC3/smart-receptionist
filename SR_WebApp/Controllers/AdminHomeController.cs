using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using SR_WebApp.Models;
using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SR_WebApp.Controllers
{
    [Authorize]
    public class AdminHomeController : Controller
    {
        private readonly string _clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private readonly string _appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        private readonly string _aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private const string GraphResourceId = "https://graph.windows.net";

        // GET: AdminHome
        public async Task<ActionResult> Index()
        {
            string tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            try
            {
                Uri servicePointUri = new Uri(GraphResourceId);
                Uri serviceRoot = new Uri(servicePointUri, tenantId);
                ActiveDirectoryClient activeDirectoryClient = new ActiveDirectoryClient(serviceRoot,
                    async () => await GetTokenForApplication());

                // use the token for querying the graph to get the user details

                var result = await activeDirectoryClient.Users
                    .Where(u => u.ObjectId.Equals(userObjectId))
                    .ExecuteAsync();
                IUser user = result.CurrentPage.ToList().First();

                return View(user);
            }
            catch (AdalException)
            {
                // Return to error page.
                return View("Error");
            }
            // if the above failed, the user needs to explicitly re-authenticate for the app to obtain the required token
            catch (Exception)
            {
                return View("~/Views/Account/Relogin.cshtml");
            }
        }

        public void RefreshSession()
        {
            HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties { RedirectUri = "/" },
                OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }

        public async Task<string> GetTokenForApplication()
        {
            string signedInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
            ClientCredential clientcred = new ClientCredential(_clientId, _appKey);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's database
            AuthenticationContext authenticationContext = new AuthenticationContext(_aadInstance + tenantId, new ADALTokenCache(signedInUserId));
            AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenSilentAsync(GraphResourceId, clientcred, new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
            return authenticationResult.AccessToken;
        }

        // GET: AdminHome/EchoDevices
        public ActionResult EchoDevices()
        {
            return View();
        }

        // GET: AdminHome/Staff
        public ActionResult Staff()
        {
            return View();
        }

        // GET: AdminHome/FloorDirectory
        public ActionResult FloorDirectory()
        {
            return View();
        }

        //GET: AdminHome/FloorDirectory/DirectionsSteps/1
        [Route("AdminHome/FloorDirectory/DirectionsSteps/{id}")]
        public ActionResult DirectionsSteps(int id)
        {
            var floorDirectoryModel = new FloorDirectoryModel {Id = id};
            return View(floorDirectoryModel);
        }

        // GET: AdminHome/LostAndFound
        public ActionResult LostAndFound()
        {
            return View();
        }
    }
}