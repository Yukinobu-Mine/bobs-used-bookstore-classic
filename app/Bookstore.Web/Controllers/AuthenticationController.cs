using System;
using BobsBookstoreClassic.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers
{
    public class AuthenticationController : Controller
    {
        public IActionResult Login(string redirectUri = null)
        {
            if (string.IsNullOrWhiteSpace(redirectUri)) return RedirectToAction("Index", "Home");
            return Redirect(redirectUri);
        }

        public IActionResult LogOut()
        {
            return BookstoreConfiguration.GetSetting("Services/Authentication") == "aws"
                ? CognitoSignOut()
                : LocalSignOut();
        }

        private IActionResult LocalSignOut()
        {
            if (Request.Cookies.ContainsKey("LocalAuthentication"))
            {
                Response.Cookies.Append("LocalAuthentication", string.Empty, new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(-1)
                });
            }
            return RedirectToAction("Index", "Home");
        }

        private IActionResult CognitoSignOut()
        {
            if (Request.Cookies.ContainsKey(".AspNet.Cookies"))
            {
                Response.Cookies.Append(".AspNet.Cookies", string.Empty, new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(-1)
                });
            }

            var domain = BookstoreConfiguration.GetSetting("Authentication/Cognito/CognitoDomain");
            var clientId = BookstoreConfiguration.GetSetting("Authentication/Cognito/LocalClientId");
            var logoutUri = $"{Request.Scheme}://{Request.Host}/";

            return Redirect($"{domain}/logout?client_id={clientId}&logout_uri={logoutUri}");
        }
    }
}
