using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ImportCsvModel : DataConnectedModel
  {
    private const string FileTypeCsv = "CSV";

    private IEnumerable<string> DbFieldsList
    {
      get
      {
        var list = new List<string>
                     {
                       // same list repeated below
                       "LastName",
                       "FirstName",
                       "IneligibleReasonGuid",
                       "Area",
                       "BahaiId",
                       "OtherLastNames",
                       "OtherNames",
                       "OtherInfo",
                       "Email",
                     };

        // screen this hard-coded list against the Person object to ensure we aren't using old field names
        var sample = new Person();
        return list.Intersect(sample.GetAllPropertyInfos().Select(pi => pi.Name));
      }
    }

    public string ProcessUpload(out int rowId)
    {
      rowId = 0;

      var httpRequest = HttpContext.Current.Request;
      var inputStream = httpRequest.InputStream;
      if (inputStream.Length == 0)
      {
        return "No file received";
      }

      var name = HttpUtility.UrlDecode(httpRequest.Headers["X-File-Name"].DefaultTo("unknown name"));
      var fileSize = (int)inputStream.Length;

      var record = new ImportFile
      {
        ElectionGuid = UserSession.CurrentElectionGuid,
        Contents = new byte[fileSize],
        FileSize = fileSize,
        OriginalFileName = name,
        UploadTime = DateTime.Now,
        FileType = FileTypeCsv,
        ProcessingStatus = "Uploaded"
      };

      var numWritten = inputStream.Read(record.Contents, 0, fileSize);
      if (numWritten != fileSize)
      {
        return "Read {0}. Should be {1}.".FilledWith(numWritten, fileSize);
      }

      new ImportHelper().ExtraProcessingIfMultipartEncoded(record);

      Db.ImportFile.Add(record);
      Db.SaveChanges();

      rowId = record.C_RowId;

      new LogHelper().Add("Uploaded file #" + record.C_RowId);

      return "";
    }

    public object PreviousUploads()
    {
      var timeOffset = UserSession.TimeOffsetServerAhead;

      return Db.ImportFile
        .Where(vi => vi.ElectionGuid == UserSession.CurrentElectionGuid)
        .Where(vi => vi.FileType == FileTypeCsv)
        .OrderByDescending(vi => vi.UploadTime)
        .ToList()
        .Select(vi => new
        {
          vi.C_RowId,
          vi.FileSize,
          UploadTime = vi.UploadTime.GetValueOrDefault().AddMilliseconds(0 - timeOffset),
          vi.FileType,
          vi.ProcessingStatus,
          vi.OriginalFileName,
          vi.CodePage,
          vi.Messages
        })
        .ToList();
    }

    public ActionResult DeleteFile(int id)
    {
      var targetFile = Db.ImportFile.FirstOrDefault(f => f.C_RowId == id);
      if (targetFile == null)
      {
        targetFile = new ImportFile { C_RowId = id, ElectionGuid = UserSession.CurrentElectionGuid };
        Db.ImportFile.Attach(targetFile);
      }
      Db.ImportFile.Remove(targetFile);
      Db.SaveChanges();

      new LogHelper().Add("Deleted file #" + id);

      return GetUploadList();
    }

    public ActionResult GetUploadList()
    {
      return new
      {
        serverTime = DateTime.Now,
        previousFiles = PreviousUploads()
      }.AsJsonResult();
    }

    public JsonResult CopyMap(int from, int to)
    {
      var files = Db.ImportFile.Where(fi => fi.ElectionGuid == UserSession.CurrentElectionGuid
                                             && (fi.C_RowId == from || fi.C_RowId == to)).ToList();
      if (files.Count != 2)
      {
        throw new ApplicationException("File(s) not found");
      }

      var importFile = files.Single(fi => fi.C_RowId == to);

      importFile.ColumnsToRead = files.Single(fi => fi.C_RowId == from).ColumnsToRead;

      Db.SaveChanges();

      return ReadFields(importFile);
    }

    public JsonResult ReadFields(int rowId)
    {
      var importFile =
        Db.ImportFile.SingleOrDefault(
          fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == rowId);
      if (importFile == null)
      {
        throw new ApplicationException("File not found");
      }

      return ReadFields(importFile);
    }

    private JsonResult ReadFields(ImportFile importFile)
    {
      var textReader = new StringReader(importFile.Contents.AsString(importFile.CodePage));
      var csv = new CsvReader(textReader, true) { SkipEmptyLines = true };
      var csvHeaders = csv.GetFieldHeaders();

      //mapping:   csv->db,csv->db
      var currentMappings =
        importFile.ColumnsToRead.DefaultTo("").SplitWithString(",").Select(s => s.SplitWithString("->"));
      var dbFields = DbFieldsList;

      const int numSampleLinesWanted = 5;
      var numSampleLinesFound = numSampleLinesWanted;
      var sampleValues = new Dictionary<string, List<string>>();

      for (var i = 0; i < numSampleLinesFound; i++)
      {
        if (csv.EndOfStream)
        {
          numSampleLinesFound = i;
          break;
        }
        foreach (var csvHeader in csvHeaders)
        {
          if (i == 0)
          {
            if (sampleValues.ContainsKey(csvHeader))
            {
              // ignore second column with same title
              continue;
            }
            sampleValues.Add(csvHeader, new List<string> { csv[i, csvHeader] });
          }
          else
          {
            sampleValues[csvHeader].Add(csv[i, csvHeader]);
          }
        }
      }

      return new
      {
        possible = dbFields,
        csvFields = csvHeaders.Select(header => new
        {
          field = header,
          map = currentMappings.Where(cs => cs[0] == header)
                                                  .Select(cs => cs[1]).SingleOrDefault().DefaultTo(""),
          sample = sampleValues[header]
        })
      }.AsJsonResult();
    }

    public JsonResult SaveMapping(int id, List<string> mapping)
    {
      var fileInfo = Db.ImportFile.SingleOrDefault(
        fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == id);
      if (fileInfo == null)
      {
        throw new ApplicationException("File not found");
      }

      fileInfo.ColumnsToRead = mapping.JoinedAsString(",");

      fileInfo.ProcessingStatus = mapping != null && mapping.Count != 0 ? "Mapped" : "Uploaded";

      Db.SaveChanges();

      return new { Message = "", Status = fileInfo.ProcessingStatus }.AsJsonResult();
    }

    public JsonResult Import(int rowId)
    {
      var file =
        Db.ImportFile.SingleOrDefault(
          fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == rowId);
      if (file == null)
      {
        return new
        {
          failed = true,
          result = new[] { "File not found" }
        }.AsJsonResult();
      }

      var columnsToRead = file.ColumnsToRead;
      if (columnsToRead == null)
      {
        return new
        {
          failed = true,
          result = new[] { "Mapping not defined" }
        }.AsJsonResult();
      }

      var start = DateTime.Now;
      var textReader = new StringReader(file.Contents.AsString(file.CodePage));
      var csv = new CsvReader(textReader, true) { SkipEmptyLines = true };

      //mapping:   csv->db,csv->db
      var currentMappings =
        columnsToRead.DefaultTo("").SplitWithString(",").Select(s => s.SplitWithString("->")).ToList();
      var dbFields = DbFieldsList.ToList();
      var validMappings = currentMappings.Where(mapping => dbFields.Contains(mapping[1])).ToList();

      if (validMappings.Count == 0)
      {
        return new
        {
          failed = true,
          result = new[] { "Mapping not defined" }
        }.AsJsonResult();
      }

      var mappedFields = dbFields.Where(f => validMappings.Select(m => m[1]).Contains(f)).ToList();
      if (!mappedFields.Contains("LastName"))
      {
        return new
        {
          failed = true,
          result = new[] { "Last Name must be mapped" }
        }.AsJsonResult();
      }

      var currentPeople = new PersonCacher(Db).AllForThisElection.ToList();
      var personModel = new PeopleModel();
      var defaultReason = new ElectionModel().GetDefaultIneligibleReason();

      var rowsProcessed = 0;
      var rowsSkipped = 0;
      var peopleAdded = 0;
      var peopleSkipped = 0;
      var peopleSkipWarningGiven = false;

      var hub = new ImportHub();
      var peopleToLoad = new List<Person>();
      var result = new List<string>();

      var unexpectedReasons = new Dictionary<string, int>();
      var validReasons = 0;

      csv.ReadNextRecord();
      while (!csv.EndOfStream)
      {
        rowsProcessed++;

        var valuesSet = false;
        var namesFoundInRow = false;

        var query = currentPeople.AsQueryable();

        var person = new Person();
        var reason = defaultReason;

        foreach (var currentMapping in validMappings)
        {
          var dbFieldName = currentMapping[1];
          var value = csv[currentMapping[0]];

          switch (dbFieldName)
          {
            case "IneligibleReasonGuid":
              // match value to the list of Enums
              value = value.Trim();
              if (value.HasContent())
              {
                var match = IneligibleReasonEnum.GetFor(value);
                if (match != null)
                {
                  reason = match;
                  validReasons += 1;
                }
                else
                {
                  // tried but didn't match a valid reason!
                  reason = defaultReason;
                  value = HttpUtility.HtmlEncode(value);
                  result.Add("Invalid Eligibility Status reason on line {0}: {1}".FilledWith(rowsProcessed + 1, value));

                  if (unexpectedReasons.ContainsKey(value))
                  {
                    unexpectedReasons[value] += 1;
                  }
                  else
                  {
                    unexpectedReasons.Add(value, 1);
                  }
                }
              }
              break;
            default:
              person.SetPropertyValue(dbFieldName, value);
              break;
          };
          valuesSet = true;

          switch (dbFieldName)
          {
            case "LastName":
              query = query.Where(p => p.LastName == value);
              namesFoundInRow = namesFoundInRow || value.HasContent();
              break;
            case "FirstName":
              query = query.Where(p => p.FirstName == value);
              namesFoundInRow = namesFoundInRow || value.HasContent();
              break;
            case "OtherLastNames":
              query = query.Where(p => p.OtherLastNames == value);
              break;
            case "OtherNames":
              query = query.Where(p => p.OtherNames == value);
              break;
            case "OtherInfo":
              query = query.Where(p => p.OtherInfo == value);
              break;
            case "Area":
              query = query.Where(p => p.Area == value);
              break;
            case "BahaiId":
              query = query.Where(p => p.BahaiId == value);
              break;
            case "Email":
              query = query.Where(p => p.Email == value);
              break;
            case "IneligibleReasonGuid":
              //if (reason != defaultReason)
              //{
              //  query = query.Where(p => p.IneligibleReasonGuid == reason.Value);
              //}
              break;
            default:
              throw new ApplicationException("Unexpected: " + dbFieldName);
          }
        }

        if (!valuesSet || !namesFoundInRow)
        {
          rowsSkipped++;
          result.Add("Skipping line " + rowsProcessed);
        }
        else if (query.Any())
        {
          peopleSkipped++;
          if (peopleSkipped < 10)
          {
            result.Add("Duplicate on line " + (rowsProcessed + 1));
          }
          else {
            if (!peopleSkipWarningGiven)
            {
              result.Add("More duplicates... (Only the first 10 are noted.)");
              peopleSkipWarningGiven = true;
            }
          }
        }
        else
        {
          //get ready for DB
          person.ElectionGuid = UserSession.CurrentElectionGuid;
          person.PersonGuid = Guid.NewGuid();

          personModel.SetCombinedInfoAtStart(person);
          personModel.SetInvolvementFlagsToDefault(person, reason);

          //Db.Person.Add(person);
          currentPeople.Add(person);
          peopleToLoad.Add(person);

          peopleAdded++;

          if (peopleToLoad.Count >= 500)
          {
            //Db.SaveChanges();

            Db.BulkInsert(peopleToLoad);
            peopleToLoad.Clear();
          }
        }

        if (rowsProcessed % 100 == 0)
        {
          hub.ImportInfo(rowsProcessed, peopleAdded);
        }

        csv.ReadNextRecord();
      }

      if (peopleToLoad.Count != 0)
      {
        Db.BulkInsert(peopleToLoad);
      }

      file.ProcessingStatus = "Imported";

      Db.SaveChanges();

      new PersonCacher().DropThisCache();

      result.AddRange(new[]
      {
        "Processed {0} data line{1}".FilledWith(rowsProcessed, rowsProcessed.Plural()),
        "Added {0} {1}.".FilledWith(peopleAdded, peopleAdded.Plural("people", "person"))
      });
      if (peopleSkipped > 0)
      {
        result.Add("{0} duplicate{1} ignored.".FilledWith(peopleSkipped, peopleSkipped.Plural()));
      }
      if (rowsSkipped > 0)
      {
        result.Add("{0} line{1} skipped or blank.".FilledWith(rowsSkipped, rowsSkipped.Plural()));
      }
      if (validReasons > 0)
      {
        result.Add("{0} {1} with recognized Eligibility Status Reasons.".FilledWith(validReasons, validReasons.Plural("people", "person")));
      }
      if (unexpectedReasons.Count > 0)
      {
        result.Add("{0} Eligibility Status Reason{1} not recognized: ".FilledWith(unexpectedReasons.Count, unexpectedReasons.Count.Plural()));
        foreach (var r in unexpectedReasons)
        {
          result.Add("&nbsp; &nbsp; \"{0}\"{1}".FilledWith(r.Key, r.Value == 1 ? "" : " x" + r.Value));
        }
      }

      result.Add("Import completed in " + (DateTime.Now - start).TotalSeconds.ToString("0.0") + " s.");

      new LogHelper().Add("Imported file #" + rowId + ": " + result.JoinedAsString(" "), true);

      return new
      {
        result,
        count = NumberOfPeople
      }.AsJsonResult();
    }

    public int NumberOfPeople
    {
      get { return new PersonCacher(Db).AllForThisElection.Count(); }
    }

    public JsonResult SaveCodePage(int id, int codepage)
    {
      var fileInfo = Db.ImportFile.SingleOrDefault(
        fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == id);
      if (fileInfo == null)
      {
        throw new ApplicationException("File not found");
      }

      fileInfo.CodePage = codepage;

      Db.SaveChanges();

      return new { Message = "" }.AsJsonResult();
    }
  }
}