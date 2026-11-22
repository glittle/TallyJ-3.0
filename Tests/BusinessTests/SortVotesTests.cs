using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.CoreModels;
using TallyJ.EF;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class SortVotesTests
  {
    [TestMethod]
    public void SortVotes_null_id_list_returns_success_without_throwing()
    {
      var model = new BallotNormalModel();

      model.SortVotes(null, null).ShouldEqual(true);
    }

    [TestMethod]
    public void SortVotes_empty_id_list_returns_success_without_throwing()
    {
      var model = new BallotNormalModel();

      model.SortVotes(new List<int>(), null).ShouldEqual(true);
    }

    [TestMethod]
    public void Posted_order_renumbers_matching_votes_and_skips_unknown_ids()
    {
      var ballot = Guid.NewGuid();
      var otherBallot = Guid.NewGuid();
      var first = Vote(10, ballot, 1);
      var second = Vote(20, ballot, 2);
      var third = Vote(30, ballot, 3);
      var elsewhere = Vote(40, otherBallot, 7);
      var votes = new List<Vote> { first, second, third, elsewhere };

      var written = BallotModelCore.WritePositionsInPostedOrder(
        votes, ballot, new List<int> { 30, 99, 10, 20 }, null);

      written.ShouldEqual(3);
      third.PositionOnBallot.ShouldEqual(1);
      first.PositionOnBallot.ShouldEqual(2);
      second.PositionOnBallot.ShouldEqual(3);
      elsewhere.PositionOnBallot.ShouldEqual(7);
    }

    [TestMethod]
    public void Posted_order_changes_nothing_when_the_id_list_or_votes_are_missing()
    {
      var vote = Vote(10, Guid.NewGuid(), 4);
      var votes = new List<Vote> { vote };

      BallotModelCore.WritePositionsInPostedOrder(votes, vote.BallotGuid, null, null).ShouldEqual(0);
      BallotModelCore.WritePositionsInPostedOrder(votes, vote.BallotGuid, new List<int>(), null).ShouldEqual(0);
      BallotModelCore.WritePositionsInPostedOrder(null, vote.BallotGuid, new List<int> { 10 }, null).ShouldEqual(0);

      vote.PositionOnBallot.ShouldEqual(4);
    }

    [TestMethod]
    public void Empty_json_id_list_binds_as_null()
    {
      Assert.IsNull(BindIdList("{\"idList\":[]}"));
    }

    [TestMethod]
    public void Json_id_list_with_values_binds_in_posted_order()
    {
      var bound = BindIdList("{\"idList\":[30,10,20]}");

      Assert.IsNotNull(bound);
      bound.Count.ShouldEqual(3);
      bound[0].ShouldEqual(30);
      bound[1].ShouldEqual(10);
      bound[2].ShouldEqual(20);
    }

    private static Vote Vote(int id, Guid ballotGuid, int position)
    {
      return new Vote
      {
        C_RowId = id,
        BallotGuid = ballotGuid,
        PositionOnBallot = position
      };
    }

    private static List<int> BindIdList(string json)
    {
      var request = new JsonBodyRequest(json);
      var controllerContext = new ControllerContext
      {
        HttpContext = new JsonHttpContext(request)
      };
      var provider = new JsonValueProviderFactory().GetValueProvider(controllerContext);
      Assert.IsNotNull(provider, "JSON value provider was not created");

      var bindingContext = new ModelBindingContext
      {
        ModelName = "idList",
        FallbackToEmptyPrefix = true,
        ValueProvider = provider,
        ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, typeof(List<int>))
      };

      return new DefaultModelBinder().BindModel(controllerContext, bindingContext) as List<int>;
    }

    private class JsonHttpContext : HttpContextBase
    {
      public JsonHttpContext(HttpRequestBase request)
      {
        Request = request;
      }

      public override HttpRequestBase Request { get; }
    }

    private class JsonBodyRequest : HttpRequestBase
    {
      private readonly MemoryStream _body;

      public JsonBodyRequest(string json)
      {
        _body = new MemoryStream(Encoding.UTF8.GetBytes(json));
      }

      public override string ContentType => "application/json; charset=utf-8";

      public override Stream InputStream => _body;
    }
  }
}
