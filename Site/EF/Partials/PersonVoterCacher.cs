// using System.Linq;
// using TallyJ.Code.Session;
//
// namespace TallyJ.EF
// {
//   public class PersonVoterCacher : CacherBase<Voter>
//   {
//     public override IQueryable<Voter> MainQuery()
//     {
//       return CurrentDb.Voter.Where(p => p.ElectionGuid == CurrentPeopleElectionGuid);
//     }
//
//     protected override void ItemChanged()
//     {
//       new ResultSummaryCacher(CurrentDb).VoteOrPersonChanged();
//     }
//
//     private static object _lockObject;
//
//     public PersonVoterCacher(ITallyJDbContext dbContext) : base(dbContext)
//     {
//     }
//     public PersonVoterCacher() : base(UserSession.GetNewDbContext)
//     {
//     }
//
//
//     protected override object LockCacheBaseObject => _lockObject ??= new object();
//   }
// }