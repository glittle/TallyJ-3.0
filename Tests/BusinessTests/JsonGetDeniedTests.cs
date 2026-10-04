using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Helpers;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class JsonGetDeniedTests
  {
    [TestMethod]
    public void Get_on_a_deny_get_result_returns_405_without_throwing()
    {
      var response = new RecordingResponse();
      var result = new NewtonSoftBasedJsonResult
      {
        Data = new { Open = true },
        JsonRequestBehavior = JsonRequestBehavior.DenyGet
      };

      result.ExecuteResult(Context("GET", response));

      response.StatusCode.ShouldEqual(405);
      response.HeaderValues["Allow"].ShouldEqual("POST");
      response.TrySkipIisCustomErrors.ShouldEqual(true);
      response.ContentType.ShouldEqual("text/plain");
      response.Body.ShouldEqual("Method Not Allowed");
    }

    [TestMethod]
    public void Post_on_a_deny_get_result_still_writes_json()
    {
      var response = new RecordingResponse();
      var result = new NewtonSoftBasedJsonResult
      {
        Data = new { Ok = true },
        JsonRequestBehavior = JsonRequestBehavior.DenyGet
      };

      result.ExecuteResult(Context("POST", response));

      response.StatusCode.ShouldEqual(200);
      Assert.IsTrue(response.Body.Contains("\"Ok\":true"), response.Body);
      Assert.IsNull(response.HeaderValues["Allow"]);
    }

    [TestMethod]
    public void Get_is_still_allowed_when_the_action_uses_allow_get()
    {
      var response = new RecordingResponse();
      var result = new NewtonSoftBasedJsonResult
      {
        Data = new { Open = true },
        JsonRequestBehavior = JsonRequestBehavior.AllowGet
      };

      result.ExecuteResult(Context("GET", response));

      response.StatusCode.ShouldEqual(200);
      Assert.IsTrue(response.Body.Contains("\"Open\":true"), response.Body);
      Assert.IsNull(response.HeaderValues["Allow"]);
    }

    private static ControllerContext Context(string method, RecordingResponse response)
    {
      return new ControllerContext
      {
        HttpContext = new RecordingContext(method, response)
      };
    }

    private class RecordingContext : HttpContextBase
    {
      public RecordingContext(string method, RecordingResponse response)
      {
        Request = new RecordingRequest(method);
        Response = response;
      }

      public override HttpRequestBase Request { get; }
      public override HttpResponseBase Response { get; }
    }

    private class RecordingRequest : HttpRequestBase
    {
      public RecordingRequest(string method)
      {
        HttpMethod = method;
      }

      public override string HttpMethod { get; }
    }

    private class RecordingResponse : HttpResponseBase
    {
      private readonly StringWriter _body = new StringWriter();

      public override int StatusCode { get; set; } = 200;
      public override string ContentType { get; set; }
      public override bool TrySkipIisCustomErrors { get; set; }
      public NameValueCollection HeaderValues { get; } = new NameValueCollection();
      public string Body => _body.ToString();

      public override void Write(string s)
      {
        _body.Write(s);
      }

      public override void AppendHeader(string name, string value)
      {
        HeaderValues[name] = value;
      }
    }
  }
}
