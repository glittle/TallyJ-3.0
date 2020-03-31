// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using System.Net.Http;
// using System.Security.Claims;
// using System.Security.Cryptography;
// using System.Text;
// using System.Threading.Tasks;
// using Microsoft.Owin;
// using Microsoft.Owin.Infrastructure;
// using Microsoft.Owin.Logging;
// using Microsoft.Owin.Security;
// using Microsoft.Owin.Security.Facebook;
// using Microsoft.Owin.Security.Infrastructure;
// using Newtonsoft.Json.Linq;
//
// namespace TallyJ.Code.OwinRelated
// {
//   internal class CustomFacebookAuthenticationHandler : AuthenticationHandler<FacebookAuthenticationOptions>
//   {
//     private const string XmlSchemaString = "http://www.w3.org/2001/XMLSchema#string";
//     private readonly ILogger _logger;
//     private readonly HttpClient _httpClient;
//
//     public CustomFacebookAuthenticationHandler(HttpClient httpClient, ILogger logger)
//     {
//       this._httpClient = httpClient;
//       this._logger = logger;
//     }
//
//     protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
//     {
//       CustomFacebookAuthenticationHandler authenticationHandler = this;
//       AuthenticationProperties properties = (AuthenticationProperties)null;
//       try
//       {
//         string stringToEscape1 = (string)null;
//         string protectedText = (string)null;
//         IReadableStringCollection query = authenticationHandler.Request.Query;
//         IList<string> values1 = query.GetValues("error");
//         if (values1 != null && values1.Count >= 1)
//           authenticationHandler._logger.WriteVerbose("Remote server returned an error: " + (object)authenticationHandler.Request.QueryString);
//         IList<string> values2 = query.GetValues("code");
//         if (values2 != null && values2.Count == 1)
//           stringToEscape1 = values2[0];
//         IList<string> values3 = query.GetValues("state");
//         if (values3 != null && values3.Count == 1)
//           protectedText = values3[0];
//         properties = authenticationHandler.Options.StateDataFormat.Unprotect(protectedText);
//         if (properties == null)
//           return (AuthenticationTicket)null;
//         if (!authenticationHandler.ValidateCorrelationId(authenticationHandler.Options.CookieManager, properties, authenticationHandler._logger))
//           return new AuthenticationTicket((ClaimsIdentity)null, properties);
//         if (stringToEscape1 == null)
//           return new AuthenticationTicket((ClaimsIdentity)null, properties);
//         string stringToEscape2 = authenticationHandler.Request.Scheme + "://" + (object)authenticationHandler.Request.Host + (object)authenticationHandler.Request.PathBase + (object)authenticationHandler.Options.CallbackPath;
//         string str1 = "grant_type=authorization_code&code=" + Uri.EscapeDataString(stringToEscape1) + "&redirect_uri=" + Uri.EscapeDataString(stringToEscape2) + "&client_id=" + Uri.EscapeDataString(authenticationHandler.Options.AppId) + "&client_secret=" + Uri.EscapeDataString(authenticationHandler.Options.AppSecret);
//         HttpResponseMessage async1 = await authenticationHandler._httpClient.GetAsync(authenticationHandler.Options.TokenEndpoint + "?" + str1, authenticationHandler.Request.CallCancelled);
//         async1.EnsureSuccessStatusCode();
//         JObject jobject = JObject.Parse(await async1.Content.ReadAsStringAsync());
//         string accessToken = jobject.Value<string>((object)"access_token");
//         if (string.IsNullOrWhiteSpace(accessToken))
//         {
//           authenticationHandler._logger.WriteWarning("Access token was not found");
//           return new AuthenticationTicket((ClaimsIdentity)null, properties);
//         }
//         string expires = jobject.Value<string>((object)"expires_in");
//         string str2 = WebUtilities.AddQueryString(authenticationHandler.Options.UserInformationEndpoint, "access_token", accessToken);
//         if (authenticationHandler.Options.SendAppSecretProof)
//           str2 = WebUtilities.AddQueryString(str2, "appsecret_proof", authenticationHandler.GenerateAppSecretProof(accessToken));
//         if (authenticationHandler.Options.Fields.Count > 0)
//           str2 = WebUtilities.AddQueryString(str2, "fields", string.Join(",", (IEnumerable<string>)authenticationHandler.Options.Fields));
//         HttpResponseMessage async2 = await authenticationHandler._httpClient.GetAsync(str2, authenticationHandler.Request.CallCancelled);
//         async2.EnsureSuccessStatusCode();
//         JObject user = JObject.Parse(await async2.Content.ReadAsStringAsync());
//         FacebookAuthenticatedContext context = new FacebookAuthenticatedContext(authenticationHandler.Context, user, accessToken, expires);
//         context.Identity = new ClaimsIdentity(authenticationHandler.Options.AuthenticationType, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
//         if (!string.IsNullOrEmpty(context.Id))
//           context.Identity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", context.Id, "http://www.w3.org/2001/XMLSchema#string", authenticationHandler.Options.AuthenticationType));
//         if (!string.IsNullOrEmpty(context.UserName))
//           context.Identity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", context.UserName, "http://www.w3.org/2001/XMLSchema#string", authenticationHandler.Options.AuthenticationType));
//         if (!string.IsNullOrEmpty(context.Email))
//           context.Identity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", context.Email, "http://www.w3.org/2001/XMLSchema#string", authenticationHandler.Options.AuthenticationType));
//         if (!string.IsNullOrEmpty(context.Name))
//         {
//           context.Identity.AddClaim(new Claim("urn:facebook:name", context.Name, "http://www.w3.org/2001/XMLSchema#string", authenticationHandler.Options.AuthenticationType));
//           if (string.IsNullOrEmpty(context.UserName))
//             context.Identity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", context.Name, "http://www.w3.org/2001/XMLSchema#string", authenticationHandler.Options.AuthenticationType));
//         }
//         if (!string.IsNullOrEmpty(context.Link))
//           context.Identity.AddClaim(new Claim("urn:facebook:link", context.Link, "http://www.w3.org/2001/XMLSchema#string", authenticationHandler.Options.AuthenticationType));
//         context.Properties = properties;
//         await authenticationHandler.Options.Provider.Authenticated(context);
//         return new AuthenticationTicket(context.Identity, context.Properties);
//       }
//       catch (Exception ex)
//       {
//         authenticationHandler._logger.WriteError("Authentication failed", ex);
//         return new AuthenticationTicket((ClaimsIdentity)null, properties);
//       }
//     }
//
//     protected override Task ApplyResponseChallengeAsync()
//     {
//       if (this.Response.StatusCode != 401)
//         return (Task)Task.FromResult<object>((object)null);
//       AuthenticationResponseChallenge responseChallenge = this.Helper.LookupChallenge(this.Options.AuthenticationType, this.Options.AuthenticationMode);
//       if (responseChallenge != null)
//       {
//         string str1 = this.Request.Scheme + Uri.SchemeDelimiter + (object)this.Request.Host + (object)this.Request.PathBase;
//         string str2 = str1 + (object)this.Request.Path + (object)this.Request.QueryString;
//         string stringToEscape1 = str1 + (object)this.Options.CallbackPath;
//         AuthenticationProperties properties = responseChallenge.Properties;
//         if (string.IsNullOrEmpty(properties.RedirectUri))
//           properties.RedirectUri = str2;
//         this.GenerateCorrelationId(this.Options.CookieManager, properties);
//         string stringToEscape2 = string.Join(",", (IEnumerable<string>)this.Options.Scope);
//         string stringToEscape3 = this.Options.StateDataFormat.Protect(properties);
//         string redirectUri = this.Options.AuthorizationEndpoint + "?auth_type=rerequest&response_type=code&client_id=" + Uri.EscapeDataString(this.Options.AppId) + "&redirect_uri=" + Uri.EscapeDataString(stringToEscape1) + "&scope=" + Uri.EscapeDataString(stringToEscape2) + "&state=" + Uri.EscapeDataString(stringToEscape3);
//         this.Options.Provider.ApplyRedirect(new FacebookApplyRedirectContext(this.Context, this.Options, properties, redirectUri));
//       }
//       return (Task)Task.FromResult<object>((object)null);
//     }
//
//     public override async Task<bool> InvokeAsync()
//     {
//       return await this.InvokeReplyPathAsync();
//     }
//
//     private async Task<bool> InvokeReplyPathAsync()
//     {
//       CustomFacebookAuthenticationHandler authenticationHandler = this;
//       if (!authenticationHandler.Options.CallbackPath.HasValue || !(authenticationHandler.Options.CallbackPath == authenticationHandler.Request.Path))
//         return false;
//       AuthenticationTicket ticket = await authenticationHandler.AuthenticateAsync();
//       if (ticket == null)
//       {
//         authenticationHandler._logger.WriteWarning("Invalid return state, unable to redirect.");
//         authenticationHandler.Response.StatusCode = 500;
//         return true;
//       }
//       FacebookReturnEndpointContext context = new FacebookReturnEndpointContext(authenticationHandler.Context, ticket);
//       context.SignInAsAuthenticationType = authenticationHandler.Options.SignInAsAuthenticationType;
//       context.RedirectUri = ticket.Properties.RedirectUri;
//       await authenticationHandler.Options.Provider.ReturnEndpoint(context);
//       //            if (context.SignInAsAuthenticationType != null && context.Identity != null)
//       //            {
//       //                ClaimsIdentity claimsIdentity = context.Identity;
//       //                if (!string.Equals(claimsIdentity.AuthenticationType, context.SignInAsAuthenticationType, StringComparison.Ordinal))
//       //                    claimsIdentity = new ClaimsIdentity(claimsIdentity.Claims, context.SignInAsAuthenticationType, claimsIdentity.NameClaimType, claimsIdentity.RoleClaimType);
//       //                authenticationHandler.Context.Authentication.SignIn(context.Properties, claimsIdentity);
//       //            }
//       if (!context.IsRequestCompleted && context.RedirectUri != null)
//       {
//         string str = context.RedirectUri;
//         if (context.Identity == null)
//           str = WebUtilities.AddQueryString(str, "error", "access_denied");
//         authenticationHandler.Response.Redirect(str);
//         context.RequestCompleted();
//       }
//       return context.IsRequestCompleted;
//     }
//
//     private string GenerateAppSecretProof(string accessToken)
//     {
//       using (HMACSHA256 hmacshA256 = new HMACSHA256(Encoding.ASCII.GetBytes(this.Options.AppSecret)))
//       {
//         byte[] hash = hmacshA256.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
//         StringBuilder stringBuilder = new StringBuilder();
//         for (int index = 0; index < hash.Length; ++index)
//           stringBuilder.Append(hash[index].ToString("x2", (IFormatProvider)CultureInfo.InvariantCulture));
//         return stringBuilder.ToString();
//       }
//     }
//   }
// }
