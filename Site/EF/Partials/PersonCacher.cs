using EntityFramework.Caching;
using EntityFramework.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class PersonCacher : CacherBase<Person>
  {
    private bool hasLoadedVoters = false;

    public override IQueryable<Person> MainQuery()
    {
      return CurrentDb.Person.Where(p => p.ElectionGuid == CurrentPeopleElectionGuid);
    }

    private const int CacheMinutes = 30; // long enough for a reasonable gap in usage

    public override List<Person> AllForThisElection
    {
      get
      {
        var people = base.AllForThisElection;

        if (!hasLoadedVoters)
        {
          List<Voter> voters;
          lock (LockCacheBaseObject)
          {
            var voterElection = CurrentElectionGuid;
            voters = CurrentDb.Voter.Where(p => p.ElectionGuid == voterElection)
              .FromCache(CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(CacheMinutes)),
                new[] { CacheKeyRaw, voterElection.ToString() }).ToList();
          }
          foreach (var voter in voters)
          {
            var person = people.FirstOrDefault(p => p.PersonGuid == voter.PersonGuid);
            if (person != null)
            {
              person.Voter = voter;
            }
            else
            {
              throw new ApplicationException("Voter without person");
            }
          }
        }

        return people;
      }
    }

    protected override void ItemChanged()
    {
      new ResultSummaryCacher(CurrentDb).VoteOrPersonChanged();
    }

    private static object _lockObject;

    public PersonCacher(ITallyJDbContext dbContext) : base(dbContext)
    {
    }

    public PersonCacher() : base(UserSession.GetNewDbContext)
    {
    }


    protected override object LockCacheBaseObject => _lockObject ??= new object();
  }
}