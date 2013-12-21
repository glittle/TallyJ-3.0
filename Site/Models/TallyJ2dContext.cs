using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Reflection;
using TallyJ.Models.Mapping;

namespace TallyJ.Models
{
    public partial class TallyJ2dContext : DbContext, IDbContext
    {
        static TallyJ2dContext()
        {
            Database.SetInitializer<TallyJ2dContext>(null);
        }

        public TallyJ2dContext()
            : base("Name=MainConnection")
        {
        }

        public DbSet<C_Log> C_Log { get; set; }
        public DbSet<Ballot> Ballots { get; set; }
        public DbSet<Computer> Computers { get; set; }
        public DbSet<Election> Elections { get; set; }
        public DbSet<ImportFile> ImportFiles { get; set; }
        public DbSet<JoinElectionUser> JoinElectionUsers { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<ResultSummary> ResultSummaries { get; set; }
        public DbSet<ResultTie> ResultTies { get; set; }
        public DbSet<Teller> Tellers { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<vBallotInfo> vBallotInfoes { get; set; }
        public DbSet<vElectionListInfo> vElectionListInfoes { get; set; }
        public DbSet<vImportFileInfo> vImportFileInfoes { get; set; }
        public DbSet<vLocationInfo> vLocationInfoes { get; set; }
//        public DbSet<vResultInfo> vResultInfoes { get; set; }
        public DbSet<vVoteInfo> vVoteInfoes { get; set; }

        private void ConfigureModel(DbModelBuilder modelBuilder)
        {
            var types = Assembly.GetAssembly(typeof (BaseEntity)).GetTypes();

            var entityTypes = types
                .Where(x => x.IsSubclassOf(typeof(BaseEntity)) && !x.IsAbstract);

            var entityMethod = typeof(DbModelBuilder).GetMethod("Entity");

            foreach (var type in entityTypes)
            {
                entityMethod.MakeGenericMethod(type).Invoke(modelBuilder, new object[] { });
            }

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
          modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

          ConfigureModel(modelBuilder);

          modelBuilder.Configurations.Add(new UserMap());
          modelBuilder.Configurations.Add(new C_LogMap());
          modelBuilder.Configurations.Add(new BallotMap());
          modelBuilder.Configurations.Add(new ComputerMap());
          modelBuilder.Configurations.Add(new ElectionMap());
          modelBuilder.Configurations.Add(new ImportFileMap());
          modelBuilder.Configurations.Add(new JoinElectionUserMap());
          modelBuilder.Configurations.Add(new LocationMap());
          modelBuilder.Configurations.Add(new MessageMap());
          modelBuilder.Configurations.Add(new PersonMap());
          modelBuilder.Configurations.Add(new ResultMap());
          modelBuilder.Configurations.Add(new ResultSummaryMap());
          modelBuilder.Configurations.Add(new ResultTieMap());
          modelBuilder.Configurations.Add(new TellerMap());
          modelBuilder.Configurations.Add(new VoteMap());
          modelBuilder.Configurations.Add(new vBallotInfoMap());
          modelBuilder.Configurations.Add(new vElectionListInfoMap());
          modelBuilder.Configurations.Add(new vImportFileInfoMap());
          modelBuilder.Configurations.Add(new vLocationInfoMap());
          //modelBuilder.Configurations.Add(new vResultInfoMap());
          modelBuilder.Configurations.Add(new vVoteInfoMap());

          Database.SetInitializer(new MigrateDatabaseToLatestVersion<TallyJ2dContext, Migrations.Configuration>());

          //var mg = new DbMigrator(new Migrations.Configuration());
          //var scriptor = new MigratorScriptingDecorator(mg);
          //string script = scriptor.ScriptUpdate(sourceMigration: null, targetMigration: null);
          //throw new Exception(script);

          base.OnModelCreating(modelBuilder);

        }
    
    }
}
