using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TallyJ.EF
{
  public interface ITallyJDbContext
  {
    DbSet<Users> Users { get; set; }
    DbSet<C_Log> C_Log { get; set; }
    DbSet<Election> Election { get; set; }
    DbSet<ImportFile> ImportFile { get; set; }
    DbSet<JoinElectionUser> JoinElectionUser { get; set; }
    DbSet<Message> Message { get; set; }
    DbSet<Result> Result { get; set; }
    DbSet<ResultSummary> ResultSummary { get; set; }
    DbSet<ResultTie> ResultTie { get; set; }
    DbSet<Vote> Vote { get; set; }
    DbSet<Ballot> Ballot { get; set; }
    DbSet<Location> Location { get; set; }
    DbSet<Person> Person { get; set; }
    DbSet<Teller> Teller { get; set; }


    int SaveChanges();
    long CurrentRowVersion();
    void BulkInsert<T>(IEnumerable<T> entities);
    void Dispose();
  }
}
