namespace TallyJ.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Nov30 : DbMigration
    {
        public override void Up()
        {
            DropColumn("tj.vVoteInfo", "ResultId");
        }
        
        public override void Down()
        {
            AddColumn("tj.vVoteInfo", "ResultId", c => c.Int());
        }
    }
}
