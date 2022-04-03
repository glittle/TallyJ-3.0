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
          && context.HttpContext.Request.HttpMethod.ToUpperInvariant() == "GET")
      {
        base.ExecuteResult(context); // Delegate back to allow the default exception to be thrown
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