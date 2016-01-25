using System;
using System.Collections.Generic;
using System.Data.Entity;
using TallyJ.EF;

namespace Tests.BusinessTests
{
  public class TestDbContext : ITallyJDbContext
  {
    public TestDbContext()
    {
      this.Users = new TestDbSet<Users>();
      this.C_Log = new TestDbSet<C_Log>();
      this.Election = new TestDbSet<Election>();
      this.ImportFile = new TestDbSet<ImportFile>();
      this.JoinElectionUser = new TestDbSet<JoinElectionUser>();
      this.Message = new TestDbSet<Message>();
      this.Result = new TestDbSet<Result>();
      this.ResultSummary = new TestDbSet<ResultSummary>();
      this.ResultTie = new TestDbSet<ResultTie>();
      this.Vote = new TestDbSet<Vote>();
      this.Ballot = new TestDbSet<Ballot>();
      this.Location = new TestDbSet<Location>();
      this.Person = new TestDbSet<Person>();
      this.Teller = new TestDbSet<Teller>();
    }

    public int SaveChangesCount { get; private set; }
    public int SaveChanges()
    {
      this.SaveChangesCount++;
      return 1;
    }

    public void BulkInsert<T>(IEnumerable<T> entities)
    {
      // ignore
    }

    public void Dispose()
    {
      throw new NotImplementedException();
    }

    public long CurrentRowVersion()
    {
      return 1;
    }

    public DbSet<Users> Users { get; set; }
    public DbSet<C_Log> C_Log { get; set; }
    public DbSet<Election> Election { get; set; }
    public DbSet<ImportFile> ImportFile { get; set; }
    public DbSet<JoinElectionUser> JoinElectionUser { get; set; }
    public DbSet<Message> Message { get; set; }
    public DbSet<Result> Result { get; set; }
    public DbSet<ResultSummary> ResultSummary { get; set; }
    public DbSet<ResultTie> ResultTie { get; set; }
    public DbSet<Vote> Vote { get; set; }
    public DbSet<Ballot> Ballot { get; set; }
    public DbSet<Location> Location { get; set; }
    public DbSet<Person> Person { get; set; }
    public DbSet<Teller> Teller { get; set; }

  }
}
