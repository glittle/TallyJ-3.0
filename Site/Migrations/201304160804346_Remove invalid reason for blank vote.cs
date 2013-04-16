namespace TallyJ.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Removeinvalidreasonforblankvote : DbMigration
    {
        public override void Up()
        {
            // remove all votes that claimed to be for a blank line
            Sql("delete from tj.Vote where InvalidReasonGuid = 'DA27534D-D7E8-E011-A095-002269C41D11'");
        }
        
        public override void Down()
        {
        }
    }
}
