using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using NLog.LayoutRenderers.Wrappers;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ImportCsvModel : DataConnectedModel
  {
    private const string FileTypeCsv = "CSV";
    private string MappingSymbol = char.ConvertFromUtf32(29); // random unusual character - also in JS

    private IEnumerable<string> DbFieldsList
    {
      get
      {
        var list = new List<string>
                     {
                       // same list repeated below
                       "FirstName",
                       "LastName",
                       "IneligibleReasonGuid",
                       "Area",
                       "Email",
                       "Phone",
                       "BahaiId",
                       "OtherLastNames",
                       "OtherNames",
                       "OtherInfo",
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

      record.CodePage = DetectCodePage(record.Contents);

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
      var importFileCodePage = importFile.CodePage ?? DetectCodePage(importFile.Contents);
      var textReader = new StringReader(importFile.Contents.AsString(importFileCodePage));
      var csv = new CsvReader(textReader, true) { SkipEmptyLines = true };
      var csvHeaders = csv.GetFieldHeaders();

      //mapping:   csv->db,csv->db
      var currentMappings =
        importFile.ColumnsToRead.DefaultTo("").SplitWithString(",").Select(s => s.SplitWithString(MappingSymbol));
      var dbFields = DbFieldsList;

      const int numSampleLinesWanted = 5;
      var numSampleLinesFound = numSampleLinesWanted;
      var sampleValues = new Dictionary<string, List<string>>();

      for (var i = 0; i < numSampleLinesFound; i++)
      {
        if (!csv.ReadNextRecord())
        {
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
            sampleValues.Add(csvHeader, new List<string> { csv[csvHeader] });
          }
          else
          {
            sampleValues[csvHeader].Add(csv[csvHeader]);
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

    private int? DetectCodePage(byte[] importFileContents)
    {
      if (importFileContents.Length > 3)
      {
        if(importFileContents[0] == 0xEF && importFileContents[1] == 0xBB && importFileContents[2] == 0xBF)
        {
          return 65001;
        }
      }

      return null;
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
      var csv = new CsvReader(textReader, true)
      {
        SkipEmptyLines = false,
        MissingFieldAction = MissingFieldAction.ReplaceByEmpty,
        SupportsMultiline = false,
      };

      //mapping:   csv->db,csv->db
      var currentMappings =
        columnsToRead.DefaultTo("").SplitWithString(",").Select(s => s.SplitWithString(MappingSymbol)).ToList();
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
      if (!mappedFields.Contains("FirstName"))
      {
        return new
        {
          failed = true,
          result = new[] { "First Name must be mapped" }
        }.AsJsonResult();
      }

      var phoneNumberChecker = new Regex(@"\+[0-9]{4,15}");
      var phoneNumberCleaner = new Regex(@"[^\+0-9]");
      var emailChecker = new Regex(@".*@.*\..*");

      var currentPeople = new PersonCacher(Db).AllForThisElection.ToList();
      currentPeople.ForEach(p => p.TempImportLineNum = -1);

      var knownEmails = currentPeople.Where(p => p.Email != null).Select(p => p.Email.ToLower()).ToList();
      var knownPhones = currentPeople.Where(p => p.Phone != null).Select(p => p.Phone).ToList();

      var personModel = new PeopleModel();
      // var defaultReason = new ElectionModel().GetDefaultIneligibleReason();

      var currentLineNum = 1; // including header row
      var rowsWithErrors = 0;
      var peopleAdded = 0;
      var peopleSkipped = 0;
      // var peopleSkipWarningGiven = false;

      var hub = new ImportHub();
      var peopleToLoad = new List<Person>();
      var result = new List<string>();

      var unexpectedReasons = new Dictionary<string, int>();
      // var validReasons = 0;
      var continueReading = true;

      hub.StatusUpdate("Processing", true);

      while (csv.ReadNextRecord() && continueReading)
      {
        currentLineNum++;

        var valuesSet = false;
        var namesFoundInRow = 0;
        var errorInRow = false;

        var duplicateInFileSearch = currentPeople.AsQueryable();

        var person = new Person
        {
          TempImportLineNum = currentLineNum
        };

        IneligibleReasonEnum reason = null;

        foreach (var currentMapping in validMappings)
        {
          var dbFieldName = currentMapping[1];
          string value;
          try
          {
            value = csv[currentMapping[0]];
          }
          catch (Exception e)
          {
            result.Add($"~E Line {currentLineNum} - {e.Message.Split('\r')[0]}. Are there \"\" marks missing?");
            errorInRow = true;
            continueReading = false;
            break;
          }
          var rawValue = HttpUtility.HtmlEncode(value);
          var originalValue = value;


          switch (dbFieldName)
          {
            case "IneligibleReasonGuid":
              // match value to the list of Enums
              value = value.Trim();
              if (value.HasContent())
              {
                if (value == "Eligible")
                {
                  // leave as null
                }
                else
                {
                  var match = IneligibleReasonEnum.GetFor(value);
                  if (match != null)
                  {
                    person.IneligibleReasonGuid = match;
                    personModel.ApplyVoteReasonFlags(person);
                  }
                  else
                  {
                    // tried but didn't match a valid reason!
                    errorInRow = true;

                    result.Add($"~E Line {currentLineNum} - Invalid Eligibility Status reason: {rawValue}");

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
              }
              break;
            default:
              person.SetPropertyValue(dbFieldName, value);
              break;
          };

          valuesSet = valuesSet || value.HasContent();

          if (value.HasContent())
          {
            switch (dbFieldName)
            {
              case "LastName":
                duplicateInFileSearch = duplicateInFileSearch.Where(p => p.LastName == value);
                namesFoundInRow++;
                break;
              case "FirstName":
                duplicateInFileSearch = duplicateInFileSearch.Where(p => p.FirstName == value);
                namesFoundInRow++;
                break;
              case "OtherLastNames":
                duplicateInFileSearch = duplicateInFileSearch.Where(p => p.OtherLastNames == value);
                break;
              case "OtherNames":
                duplicateInFileSearch = duplicateInFileSearch.Where(p => p.OtherNames == value);
                break;
              case "OtherInfo":
                duplicateInFileSearch = duplicateInFileSearch.Where(p => p.OtherInfo == value);
                break;
              case "Area":
                duplicateInFileSearch = duplicateInFileSearch.Where(p => p.Area == value);
                break;
              case "BahaiId":
                duplicateInFileSearch = duplicateInFileSearch.Where(p => p.BahaiId == value);
                break;
              case "Email":
                if (value.HasContent())
                {
                  value = value.ToLower();
                  if (!emailChecker.IsMatch(value))
                  {
                    result.Add($"~E Line {currentLineNum} - Invalid email: {rawValue}");
                    errorInRow = true;
                  }
                  else if (knownEmails.Contains(value))
                  {
                    result.Add($"~E Line {currentLineNum} - Duplicate email: {rawValue}");
                    errorInRow = true;
                  }

                  if (!errorInRow)
                  {
                    knownEmails.Add(value);
                  }
                }
                break;
              case "Phone":
                if (value.HasContent())
                {
                  value = phoneNumberCleaner.Replace(value, "");

                  if (!value.StartsWith("+") && value.Length == 10)
                  {
                    // assume north american
                    value = "+1" + value;
                  }

                  if (!phoneNumberChecker.IsMatch(value))
                  {
                    result.Add($"~E Line {currentLineNum} - Invalid phone number: {rawValue}");
                    errorInRow = true;
                  }
                  else if (originalValue != value)
                  {
                    result.Add($"~W Line {currentLineNum} - Phone number adjusted from {rawValue} to {value} ");
                  }

                  if (value.HasContent())
                  {
                    if (knownPhones.Contains(value))
                    {
                      result.Add($"~E Line {currentLineNum} - Duplicate phone number: {value}");
                      errorInRow = true;
                    }
                 
                    knownPhones.Add(value);
                  }

                  // update with the cleaned phone number
                  person.SetPropertyValue(dbFieldName, value.HasContent() ? value : null);
                }

                break;
              case "IneligibleReasonGuid":
                break;
              default:
                throw new ApplicationException("Unexpected: " + dbFieldName);
            }
          }
        }

        var addRow = true;

        if (!valuesSet)
        {
          // don't count it as an error
          result.Add($"~I Line {currentLineNum} - Ignoring blank line");
          addRow = false;
        }
        else if (namesFoundInRow != 2 || errorInRow)
        {
          addRow = false;
          rowsWithErrors++;

          if (namesFoundInRow != 2)
          {
            result.Add($"~E Line {currentLineNum} - First or last name missing");
          }
        }

        var duplicates = duplicateInFileSearch.Select(p => p.TempImportLineNum).ToList();
        if (duplicates.Any())
        {
          addRow = false;
          peopleSkipped++;
          var dupInfo = duplicates.Select(n => n == -1 ? "in existing records" : "on line " + n);
          result.Add($"~E Line {currentLineNum} - Matching identifying names found {dupInfo.JoinedAsString(", ")}");
        }



        if (addRow)
        { //get ready for DB
          person.ElectionGuid = UserSession.CurrentElectionGuid;
          person.PersonGuid = Guid.NewGuid();

          personModel.SetCombinedInfoAtStart(person);

          peopleToLoad.Add(person);

          peopleAdded++;
        }

        currentPeople.Add(person);

        if (currentLineNum % 100 == 0)
        {
          hub.ImportInfo(currentLineNum, peopleAdded);
        }

      }

      var abort = rowsWithErrors > 0 || peopleSkipped > 0;

      if (!abort && peopleToLoad.Count != 0)
      {
        hub.StatusUpdate("Saving");

        var error = BulkInsert_CheckErrors(peopleToLoad);
        if (error != null)
        {
          abort = true;
          result.Add(error);
        }
        else
        {
          result.Add("Saved to database");
        }
      }

      file.ProcessingStatus = abort ? "Import aborted" : "Imported";

      Db.SaveChanges();

      new PersonCacher().DropThisCache();

      result.AddRange(new[]
      {
        "---------",
        $"Processed {currentLineNum:N0} data line{currentLineNum.Plural()}",
      });
      // if (peopleSkipped > 0)
      // {
      //   result.Add($"{peopleSkipped:N0} duplicate{peopleSkipped.Plural()} ignored.");
      // }
      if (rowsWithErrors > 0)
      {
        result.Add($"{rowsWithErrors:N0} line{rowsWithErrors.Plural("s had errors or were", "had errors or was")} blank.");
      }
      // if (validReasons > 0)
      // {
      //   result.Add($"{validReasons:N0} {validReasons.Plural("people", "person")} with recognized Eligibility Status Reason{validReasons.Plural()}.");
      // }
      if (unexpectedReasons.Count > 0)
      {
        result.Add($"{unexpectedReasons.Count:N0} Eligibility Status Reason{unexpectedReasons.Count.Plural()} not recognized: ");
        foreach (var r in unexpectedReasons)
        {
          result.Add("&nbsp; &nbsp; \"{0}\"{1}".FilledWith(r.Key, r.Value == 1 ? "" : " x" + r.Value));
        }
      }

      result.Add("---------");

      if (abort)
      {
        result.Add($"Import aborted due to errors in file. Please correct and try again.");
      }
      else
      {
        result.Add($"Added {peopleAdded:N0} {peopleAdded.Plural("people", "person")}.");
        result.Add($"Import completed in {(DateTime.Now - start).TotalSeconds:N1} s.");
      }

      var resultsForLog = result.Where(s => !s.StartsWith("~"));
      new LogHelper().Add("Imported file #" + rowId + ": " + resultsForLog.JoinedAsString("\r"), true);

      return new
      {
        result,
        count = NumberOfPeople
      }.AsJsonResult();
    }

    private string BulkInsert_CheckErrors(List<Person> peopleToLoad)
    {
      try
      {
        Db.BulkInsert(peopleToLoad);
        return null;
      }
      catch (Exception e)
      {
        var msg = e.GetBaseException().Message;

        if (msg.Contains("IX_PersonEmail"))
        {
          var regMatch = Regex.Match(msg, @"\((.*), (.*)\)");

          var result = regMatch.Success ? $"An email address ({regMatch.Groups[2].Value}) is duplicated in the import file. Import halted."
            : $"An email address is duplicated in the import file. Import halted.";

          return result;
        }

        if (msg.Contains("IX_PersonPhone"))
        {
          var regMatch = Regex.Match(msg, @"\((.*), (.*)\)");

          var result = regMatch.Success ? $"A phone number ({regMatch.Groups[2].Value}) is duplicated in the import file. Import halted."
            : $"A phone number is duplicated in the import file. Import halted.";

          return result;
        }


        return msg;
      }
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