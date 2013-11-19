namespace TallyJ.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Newcolumns : DbMigration
    {
        public override void Up()
        {
            AddColumn("tj.Election", "MaskVotingMethod", c => c.Boolean());
//            AlterColumn("tj.ImportFile", "FileSize", c => c.Int());
//            AlterColumn("tj.ImportFile", "HasContent", c => c.Boolean());
        }
        
        public override void Down()
        {
//            AlterColumn("tj.ImportFile", "HasContent", c => c.Boolean());
//            AlterColumn("tj.ImportFile", "FileSize", c => c.Int());
            DropColumn("tj.Election", "MaskVotingMethod");
        }
    }
}
