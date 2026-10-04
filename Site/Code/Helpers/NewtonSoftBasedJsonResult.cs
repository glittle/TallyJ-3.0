using System;
using System.IO;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace TallyJ.Code.Helpers
{
  public class NewtonSoftBasedJsonResult : JsonResult
  {
    // override and use NewtonSoft
    public override void ExecuteResult(ControllerContext context)
    {
      if (context == null)
      {
        throw new ArgumentNullException(nameof(context));
      }

      if (JsonRequestBehavior == JsonRequestBehavior.DenyGet
          && string.Equals(context.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
      {
        // A scanner GET must not throw. The exception became a 500, and Application_Error
        // logged it and sent IFTTT. 405 ends the request with no exception to handle.
        var denied = context.HttpContext.Response;
        denied.StatusCode = 405;
        denied.TrySkipIisCustomErrors = true;
        denied.AppendHeader("Allow", "POST");
        denied.ContentType = "text/plain";
        denied.Write("Method Not Allowed");
        return;
      }

      var response = context.HttpContext.Response;
      response.ContentType = string.IsNullOrEmpty(ContentType) ? "application/json" : ContentType;

      if (ContentEncoding != null)
      {
        response.ContentEncoding = ContentEncoding;
      }

      if (Data == null)
      {
        return;
      }

      response.Write(JsonConvert.SerializeObject(Data, Formatting.None, new JsonSerializerSettings
      {
        ReferenceLoopHandling = ReferenceLoopHandling.Error,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
      }));

      // var scriptSerializer = JsonSerializer.Create(Settings);
      // using (var sw = new StringWriter())
      // {
      //   scriptSerializer.Serialize(sw, Data);
      //   response.Write(sw.ToString());
      // }
    }
  }
}