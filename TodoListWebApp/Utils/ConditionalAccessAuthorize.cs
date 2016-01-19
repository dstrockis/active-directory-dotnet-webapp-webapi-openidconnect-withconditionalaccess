using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;

namespace TodoListWebApp.Utils
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ConditionalAccessAuthorize : System.Web.Mvc.AuthorizeAttribute
    {
        public string Resource { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Make sure the user is signed in before trying to get tokens on their behalf
            if (!base.AuthorizeCore(httpContext))
            {
                return false;
            }

            //
            // WithConditionalAccess:
            //
            // Ensure that you can get a token for the necessary resouce in your controller.
            // If you can't, trigger an authorize reqeust using the HandleUnauthorizedRequest below.
            //
            try
            {
                string userObjectID = ClaimsPrincipal.Current.FindFirst(Startup.ObjectIdClaimType).Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority, new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.appKey);
                AuthenticationResult result = authContext.AcquireTokenSilent(Resource, credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
            }
            catch (AdalSilentTokenAcquisitionException ex)
            {
                return false;
            }

            return true;
        }

        protected override void HandleUnauthorizedRequest(System.Web.Mvc.AuthorizationContext filterContext)
        {
            base.HandleUnauthorizedRequest(filterContext);

            //
            // WithConditionalAccess:
            //
            // Include the Resource ID for which a token is being requested in the authorize request.
            //
            filterContext.HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties(
                    new Dictionary<string, string> { { Startup.ResourceKey, Resource } }
                ),
                OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }

        
    }
}