// Decompiled with JetBrains decompiler
// Type: Microsoft.Owin.Security.Facebook.CustomFacebookAuthenticationMiddleware
// Assembly: Microsoft.Owin.Security.Facebook, Version=4.0.1.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: D5BAD26C-C419-49CE-8B60-8FBB5C47DDF6
// Assembly location: C:\Users\LSA Secretary\Documents\Bahai\Glen\TallyJ\V2d\Server\Site\packages\Microsoft.Owin.Security.Facebook.4.0.1\lib\net45\Microsoft.Owin.Security.Facebook.dll

using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Security;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

namespace TallyJ.Code.OwinRelated
{
    /// <summary>
    /// OWIN middleware for authenticating users using Facebook
    /// </summary>
    public class CustomFacebookAuthenticationMiddleware : AuthenticationMiddleware<FacebookAuthenticationOptions>
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a <see cref="T:Microsoft.Owin.Security.Facebook.CustomFacebookAuthenticationMiddleware" />
        /// </summary>
        /// <param name="next">The next middleware in the OWIN pipeline to invoke</param>
        /// <param name="app">The OWIN application</param>
        /// <param name="options">Configuration options for the middleware</param>
        public CustomFacebookAuthenticationMiddleware(
            OwinMiddleware next,
            IAppBuilder app,
            FacebookAuthenticationOptions options)
            : base(next, options)
        {
            if (string.IsNullOrWhiteSpace(this.Options.AppId))
                throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.CurrentCulture, "Exception_OptionMustBeProvided", (object)"AppId"));
            if (string.IsNullOrWhiteSpace(this.Options.AppSecret))
                throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.CurrentCulture, "Exception_OptionMustBeProvided", (object)"AppSecret"));
            this._logger = app.CreateLogger<CustomFacebookAuthenticationMiddleware>();
            if (this.Options.Provider == null)
                this.Options.Provider = (IFacebookAuthenticationProvider)new FacebookAuthenticationProvider();
            if (this.Options.StateDataFormat == null)
                this.Options.StateDataFormat = (ISecureDataFormat<AuthenticationProperties>)new PropertiesDataFormat(app.CreateDataProtector(typeof(CustomFacebookAuthenticationMiddleware).FullName, this.Options.AuthenticationType, "v1"));
            if (string.IsNullOrEmpty(this.Options.SignInAsAuthenticationType))
                this.Options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            this._httpClient = new HttpClient(CustomFacebookAuthenticationMiddleware.ResolveHttpMessageHandler(this.Options));
            this._httpClient.Timeout = this.Options.BackchannelTimeout;
            this._httpClient.MaxResponseContentBufferSize = 10485760L;
        }

        /// <summary>
        /// Provides the <see cref="T:Microsoft.Owin.Security.Infrastructure.AuthenticationHandler" /> object for processing authentication-related requests.
        /// </summary>
        /// <returns>An <see cref="T:Microsoft.Owin.Security.Infrastructure.AuthenticationHandler" /> configured with the <see cref="T:Microsoft.Owin.Security.Facebook.FacebookAuthenticationOptions" /> supplied to the constructor.</returns>
        protected override AuthenticationHandler<FacebookAuthenticationOptions> CreateHandler()
        {
            return (AuthenticationHandler<FacebookAuthenticationOptions>)new CustomFacebookAuthenticationHandler(this._httpClient, this._logger);
        }

        private static HttpMessageHandler ResolveHttpMessageHandler(
            FacebookAuthenticationOptions options)
        {
            HttpMessageHandler httpMessageHandler = options.BackchannelHttpHandler ?? (HttpMessageHandler)new WebRequestHandler();
            if (options.BackchannelCertificateValidator != null)
            {
                WebRequestHandler webRequestHandler = httpMessageHandler as WebRequestHandler;
                if (webRequestHandler == null)
                    throw new InvalidOperationException("Exception_ValidatorHandlerMismatch");
                webRequestHandler.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(options.BackchannelCertificateValidator.Validate);
            }
            return httpMessageHandler;
        }
    }
}
