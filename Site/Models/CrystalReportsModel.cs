using System.Data.SqlClient;
using System.IO;
using System.Web.Mvc;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.Models
{
  public class CrystalReportsModel : DataConnectedModel
  {
    private readonly string _reportFilepath;
    private ReportDocument _rpt;

    public CrystalReportsModel(string reportFilepath)
    {
      _reportFilepath = reportFilepath;
    }

    public ActionResult PdfResult
    {
      get { return new FileStreamResult(MakeReport(_reportFilepath), "application/pdf"); }
    }

    private void ConnectReportToDatabase()
    {
      var dbConnection = Db.Database.Connection;
      var connectionStringBuilder = new SqlConnectionStringBuilder(dbConnection.ConnectionString);

      var connectionInfo = new ConnectionInfo
                             {
                               ServerName = dbConnection.DataSource,
                               DatabaseName = dbConnection.Database,
                               Type = ConnectionInfoType.SQL,
                               IntegratedSecurity = true
                             };

      if (!connectionStringBuilder.IntegratedSecurity)
      {
        connectionInfo.IntegratedSecurity = false;
        connectionInfo.UserID = connectionStringBuilder.UserID;
        connectionInfo.Password = connectionStringBuilder.Password;
      }

      ApplyToTables(connectionInfo, _rpt);
    }

    private void ApplyToTables(ConnectionInfo connectionInfo, ReportDocument reportDocument)
    {
      foreach (Table table in reportDocument.Database.Tables)
      {
        var newLoginInfo = table.LogOnInfo;
        newLoginInfo.ConnectionInfo = connectionInfo;
        table.ApplyLogOnInfo(newLoginInfo);
      }
      //foreach (ReportDocument subreport in reportDocument.Subreports)
      //{
      //  ApplyToTables(connectionInfo, subreport);
      //}
    }

    public Stream MakeReport(string reportFilepath)
    {
      _rpt = new ReportDocument();
      _rpt.Load(reportFilepath);

      ConnectReportToDatabase();

      var electionGuidForCrystal = UserSession.CurrentElectionGuid.ToString().SurroundWith("{", "}");

      //_rpt.SetParameterValue("ElectionGuid", electionGuidForCrystal);
      // _rpt.SetParameterValue("NumToShow", UserSession.CurrentElection.NumberToElect);


      // {ResultSummary.ElectionGuid} = "{042C3C9D-9359-4B14-9057-323C27CD28E2}"
      _rpt.DataDefinition.RecordSelectionFormula = "{ResultSummary.ElectionGuid} = \"" + electionGuidForCrystal + "\"";


      //_rpt.RecordSelectionFormula = "";

      //foreach (ParameterField parameterField in _rpt.ParameterFields)
      //{
      //  if (parameterField.Name=="NumToShow")
      //  {
      //    parameterField.CurrentValues.AddValue(UserSession.CurrentElection.NumberToElect);
      //  }
      //  if (parameterField.Name=="ElectionGuid")
      //  {
      //    parameterField.CurrentValues.AddValue(UserSession.CurrentElectionGuid.ToString().SurroundWith("{","}"));
      //  }
      //}

      _rpt.Refresh();

      return _rpt.ExportToStream(ExportFormatType.PortableDocFormat);
    }
  }
}