using System.Web.Http;
using Swashbuckle.Application;
using System.Reflection;

[assembly: WebActivator.PreApplicationStartMethod(typeof(TallyJ.SwaggerConfig), "Register")]

namespace TallyJ
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "TallyJ API")
                        .Description("TallyJ Election Management System REST API")
                        .TermsOfService("Some terms")
                        .Contact(cc => cc
                            .Name("TallyJ Team")
                            .Url("https://github.com/glittle/TallyJ-3.0")
                            .Email("support@tallyj.com"))
                        .License(lic => lic
                            .Name("Apache 2.0")
                            .Url("http://www.apache.org/licenses/LICENSE-2.0.html"));

                    c.IncludeXmlComments(GetXmlCommentsPath());

                    // Custom schema mappings if needed
                    c.DescribeAllEnumsAsStrings();

                    // Group actions by controller
                    c.GroupActionsBy(apiDesc => apiDesc.ActionDescriptor.ControllerDescriptor.ControllerName);

                    // Set the default tag
                    c.EnableAnnotations();
                })
                .EnableSwaggerUi(c =>
                {
                    c.DocExpansion(DocExpansion.List);
                    c.DocumentTitle("TallyJ API Documentation");
                });
        }

        private static string GetXmlCommentsPath()
        {
            return System.String.Format(@"{0}\TallyJ.xml", System.AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}