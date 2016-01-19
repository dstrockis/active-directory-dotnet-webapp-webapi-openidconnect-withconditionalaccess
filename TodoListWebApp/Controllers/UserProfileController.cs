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
using System.Web.Mvc;

// The following using statements were added for this sample.
using System.Threading.Tasks;
using TodoListWebApp.Models;
using System.Security.Claims;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using TodoListWebApp.Utils;
using System.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;


namespace TodoListWebApp.Controllers
{
    //
    // WithConditionalAccess:
    //
    // Use this custom attribute to ensure that the user is signed in and a token can be acquired for the provided resource.
    // 
    [ConditionalAccessAuthorize(Resource = Startup.graphResourceId)]
    public class UserProfileController : Controller
    {
        private const string graphUserUrl = "https://graph.windows.net/{0}/me?api-version=2013-11-08";
        
        //
        // GET: /UserProfile/
        public async Task<ActionResult> Index()
        {
            //
            // Retrieve the user's name, tenantID, and access token since they are parameters used to query the Graph API.
            //
            UserProfile profile;
            AuthenticationResult result = null;

            try
            {
                string tenantId = ClaimsPrincipal.Current.FindFirst(Startup.TenantIdClaimType).Value;
                string userObjectID = ClaimsPrincipal.Current.FindFirst(Startup.ObjectIdClaimType).Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority, new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.appKey);
                result = authContext.AcquireTokenSilent(Startup.graphResourceId, credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                //
                // Call the Graph API and retrieve the user's profile.
                //
                string requestUrl = String.Format(
                    CultureInfo.InvariantCulture,
                    graphUserUrl,
                    HttpUtility.UrlEncode(tenantId));
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = await client.SendAsync(request);

                //
                // Return the user's profile in the view.
                //
                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    profile = JsonConvert.DeserializeObject<UserProfile>(responseString);
                }
                else
                {
                    //
                    // If the call failed due to authorization, then drop the current access token and show the user an error indicating they might need to sign-in again.
                    //
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        var todoTokens = authContext.TokenCache.ReadItems().Where(a => a.Resource == Startup.graphResourceId);
                        foreach (TokenCacheItem tci in todoTokens)
                            authContext.TokenCache.DeleteItem(tci);

                        profile = new UserProfile();
                        profile.DisplayName = " ";
                        profile.GivenName = " ";
                        profile.Surname = " ";
                        ViewBag.ErrorMessage = "AuthorizationRequired";
                        return View(profile);
                    }
                }

            }
            catch (Exception ex)
            {
                //
                // If the call failed for any other reason, show the user an error.
                //
            }

            profile = new UserProfile();
            profile.DisplayName = " ";
            profile.GivenName = " ";
            profile.Surname = " ";
            ViewBag.ErrorMessage = "UnexpectedError";
            return View(profile);
        }
    }
}