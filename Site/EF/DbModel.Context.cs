﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using TallyJ.CoreModels.Account2Models;

namespace TallyJ.EF
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using Microsoft.AspNet.Identity.EntityFramework;
    
    public partial class TallyJ2dEntities : IdentityDbContext<ApplicationUser>
    {
        public TallyJ2dEntities()
            : base("name=TallyJ2dEntities")
        {
           Database.SetInitializer<TallyJ2dEntities>(null);
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Users> Users { get; set; }
        public virtual DbSet<ImportFile> ImportFile { get; set; }
        public virtual DbSet<JoinElectionUser> JoinElectionUser { get; set; }
        public virtual DbSet<Message> Message { get; set; }
        public virtual DbSet<Result> Result { get; set; }
        public virtual DbSet<ResultTie> ResultTie { get; set; }
        public virtual DbSet<Vote> Vote { get; set; }
        public virtual DbSet<Ballot> Ballot { get; set; }
        public virtual DbSet<Location> Location { get; set; }
        public virtual DbSet<Person> Person { get; set; }
        public virtual DbSet<Teller> Teller { get; set; }
        public virtual DbSet<OnlineVotingInfo> OnlineVotingInfo { get; set; }
        public virtual DbSet<ResultSummary> ResultSummary { get; set; }
        public virtual DbSet<OnlineVoter> OnlineVoter { get; set; }
        public virtual DbSet<C_Log> C_Log { get; set; }
        public virtual DbSet<Election> Election { get; set; }
        public virtual DbSet<Memberships> Memberships { get; set; }
        public virtual DbSet<AspNetRoles> AspNetRoles { get; set; }
        public virtual DbSet<AspNetUserClaims> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogins> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUsers> AspNetUsers { get; set; }
    }
}
