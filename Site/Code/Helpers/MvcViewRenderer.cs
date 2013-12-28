using System;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using RazorEngine;
using System.IO;

namespace TallyJ.Code.Helpers
{
  public class MvcViewRenderer
  {
    public static string RenderRazorViewToString(string pathToView, object model)
    {
      var path = HostingEnvironment.MapPath(pathToView);
      if(path==null)
      {
        return "";
      }
      var template = File.ReadAllText(path);

      var body = Razor.Parse(template, model);

      return body;
    }
  }
}