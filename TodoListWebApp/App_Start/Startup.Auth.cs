//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//----------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// The following using statements were added for this sample.
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Globalization;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using TodoListWebApp.Utils;
using System.Security.Claims;

namespace TodoListWebApp
{
    public partial class Startup
    {
        public static string clientId = "[Enter your clientID from the Azure Management Portal, e.g. b1132c6b-fbf8-43b3-a9d8-329be1c87fcb]";
        public static string appKey = "[Enter your key from the Azure Management Portal, e.g. TpNUr1CrYMP5bkvXKwmRKQvINuTp2nyp4kIzoabgZC0=]";
        public static string tenant = "[Enter the name of the tenant where you registered your app, e.g. mytenant.onmicrosoft.com]";
        public const string todoListResourceId = "[Enter your App ID URI from the Azure Management Portal, e.g. https://mytenant.onmicrosoft.com/todolistservice]";

        //
        // The graphResourceId is needed to request a token to call the Graph API.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        //
        public const string graphResourceId = "https://graph.microsoft.com";
        public static string aadInstance = "https://login.microsoftonline.com/{0}";

        //
        // The Redirect Uri is the URL where the user will be redirected after sign in and sign out.
        // The Authority is the sign-in URL of the tenant.
        //
        public static string redirectUri = "https://localhost:44322/";
        public static readonly string Authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

        public const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        public const string ObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        public const string ResourceKey = "resourceid";

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = Authority,
                    PostLogoutRedirectUri = redirectUri,
                    RedirectUri = redirectUri,

                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        //
                        // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.
                        //
                        AuthorizationCodeReceived = (context) =>
                        {
                            var code = context.Code;

                            //
                            // WithConditionalAccess:
                            //
                            // If this value is not present in the dictionary, it means the authorize request was issued without a resource
                            // in the request.  You must include a resource in the request in order to guarantee you can exchange the code for a token here.
                            //
                            var resourceId = context.AuthenticationTicket.Properties.Dictionary[ResourceKey];

                            ClientCredential credential = new ClientCredential(clientId, appKey);
                            string userObjectID = context.AuthenticationTicket.Identity.FindFirst(Startup.ObjectIdClaimType).Value;
                            AuthenticationContext authContext = new AuthenticationContext(Authority, new NaiveSessionCache(userObjectID));
                            AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, resourceId);

                            return Task.FromResult(0);
                        },

                        //
                        // Handle errors in OpenID Connect responses.
                        //
                        AuthenticationFailed = (context) =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/Home/Error?message=" + context.Exception.Message);
                            return Task.FromResult(0);
                        },

                        //
                        // WithConditionalAccess:
                        //
                        // If the request is for a specific resource, add the resource parameter here.
                        //
                        RedirectToIdentityProvider = (context) =>
                        {
                            if (context.OwinContext.Authentication.AuthenticationResponseChallenge != null)
                            {
                                if (context.OwinContext.Authentication.AuthenticationResponseChallenge.Properties.Dictionary.ContainsKey(ResourceKey))
                                {
                                    context.ProtocolMessage.Resource = context.OwinContext.Authentication.AuthenticationResponseChallenge.Properties.Dictionary[ResourceKey];
                                }
                            }
                            return Task.FromResult(0);
                        }

                    }

                });
        }
    }
}