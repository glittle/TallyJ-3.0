using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using CsvReader;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels;

public class ImportCsvModel : DataConnectedModel
{
  private const string FileTypeCsv = "CSV";
  private readonly string _mappingSymbol = char.ConvertFromUtf32(29); // random unusual character - also in JS
  private readonly Dictionary<string, int> _dict;

  public ImportCsvModel()
  {
    _dict = new Dictionary<string, int>
    {
      // same list is used in a switch below
      { "FirstName", 50 },
      { "LastName", 50 },
      { "IneligibleReasonGuid", 100 }, // guid, but we read the description as text
      { "Area", 50 },
      { "Email", 250 },
      { "Phone", 25 },
      { "BahaiId", 20 },
      { "OtherLastNames", 100 },
      { "OtherNames", 100 },
      { "OtherInfo", 150 }
    };
  }

  private List<string> DbFieldsList
  {
    get
    {
      var list = _dict.Keys.ToList();

      // filter this hard-coded list against the Person object to ensure we aren't using old field names
      var sample = new Person();
      return list.Intersect(sample.GetAllPropertyInfos().Select(pi => pi.Name)).ToList();
    }
  }


  public string ProcessUpload(out int rowId)
  {
    rowId = 0;

    var httpRequest = HttpContext.Current.Request;
    var inputStream = httpRequest.InputStream;
    if (inputStream.Length == 0) return "No file received";

    var name = HttpUtility.UrlDecode(httpRequest.Headers["X-File-Name"].DefaultTo("unknown name"));
    var fileSize = (int)inputStream.Length;

    var record = new ImportFile
    {
      ElectionGuid = UserSession.CurrentElectionGuid,
      Contents = new byte[fileSize],
      FileSize = fileSize,
      OriginalFileName = name,
      UploadTime = DateTime.UtcNow,
      FileType = FileTypeCsv,
      ProcessingStatus = "Uploaded"
    };

    var numWritten = inputStream.Read(record.Contents, 0, fileSize);
    if (numWritten != fileSize) return "Read {0}. Should be {1}.".FilledWith(numWritten, fileSize);

    record.CodePage = ImportHelper.DetectCodePage(record.Contents).CodePage;

    ImportHelper.ExtraProcessingIfMultipartEncoded(record);

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
        UploadTime = vi.UploadTime.HasValue ? vi.UploadTime.AsUtc() : null,
        vi.FileType,
        vi.ProcessingStatus,
        vi.OriginalFileName,
        vi.FirstDataRow,
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
    if (files.Count != 2) throw new ApplicationException("File(s) not found");

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
    if (importFile == null) throw new ApplicationException("File not found");

    return ReadFields(importFile);
  }

  private JsonResult ReadFields(ImportFile importFile)
  {
    var importFileCodePage = importFile.CodePage ?? ImportHelper.DetectCodePage(importFile.Contents)?.CodePage;
    var fileString = importFile.Contents.AsString(importFileCodePage);

    var firstDataRow = importFile.FirstDataRow.AsInt();
    if (firstDataRow > 2)
    {
      // 1-based... headers on line 1, data on line 2. If 2 or less, ignore it.
      fileString = fileString.GetLinesAfterSkipping(firstDataRow - 2);
    }

    var textReader = new StringReader(fileString);
    var csv = new CsvReader.CsvReader(textReader, true)
    {
      MissingFieldAction = MissingFieldAction.ReplaceByEmpty
    };

    var csvHeaders = csv.GetFieldHeaders();

    if (csvHeaders.Length == 1)
      // likely failed to parse in this codepage
      return new
      {
        Success = false,
        Message = "Unable to parse headers. Are the headers on the line indicated? Is this the correct content encoding?"
      }.AsJsonResult();

    //mapping:   csv->db,csv->db

    var currentMappings =
      importFile.ColumnsToRead
        .DefaultTo("")
        .SplitWithString(",")
        .Select(s => s.SplitWithString(_mappingSymbol))
        .ToList();


    const int numSampleLinesWanted = 5;
    var numSampleLinesFound = numSampleLinesWanted;
    var sampleValues = new Dictionary<string, List<string>>();

    // read first few lines to get sample values
    for (var i = 0; i < numSampleLinesFound; i++)
    {
      if (!csv.ReadNextRecord()) break;
      foreach (var csvHeader in csvHeaders)
        if (i == 0)
        {
          if (sampleValues.ContainsKey(csvHeader))
            // ignore second column with same title
            continue;
          sampleValues.Add(csvHeader, new List<string> { csv[csvHeader] });
        }
        else
        {
          sampleValues[csvHeader].Add(csv[csvHeader]);
        }
    }

    var dbFields = DbFieldsList;

    var doAutoMapping = importFile.ColumnsToRead == null;
    if (doAutoMapping)
    {
      foreach (var csvHeader in csvHeaders)
        // look for exact match or match with spaces removed
        if (dbFields.Contains(csvHeader.Replace(" ", "")))
          currentMappings.Add(new[] { csvHeader, csvHeader });

      // if we found any, save them
      if (currentMappings.Any())
      {
        importFile.ColumnsToRead = currentMappings.Select(cs => cs[0] + _mappingSymbol + cs[1]).JoinedAsString(",");
        Db.SaveChanges();
      }
    }

    return new
    {
      Success = true,
      possible = dbFields,
      csvFields = csvHeaders.Select(header => new
      {
        field = header,
        map = currentMappings
          .Where(cs => cs[0] == header)
          .Select(cs => cs[1])
          .SingleOrDefault()
          .DefaultTo(""),
        sample = sampleValues[header]
      })
    }.AsJsonResult();
  }


  public JsonResult SaveMapping(int id, List<string> mapping)
  {
    var fileInfo = Db.ImportFile.SingleOrDefault(
      fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == id);
    if (fileInfo == null) throw new ApplicationException("File not found");

    fileInfo.ColumnsToRead = mapping.JoinedAsString(",");

    fileInfo.ProcessingStatus = mapping != null && mapping.Count != 0 ? "Mapped" : "Uploaded";

    Db.SaveChanges();

    return new { Message = "", Status = fileInfo.ProcessingStatus }.AsJsonResult();
  }

  /// <summary>
  /// Imports data from a specified file and processes it into the database.
  /// </summary>
  /// <param name="rowId">The identifier of the row in the import file to be processed.</param>
  /// <returns>A JsonResult containing the result of the import operation, including any error messages or success notifications.</returns>
  /// <remarks>
  /// This method retrieves an import file based on the provided row ID and the current election context. 
  /// It checks for the existence of the file and the necessary column mappings required for processing. 
  /// The method reads the contents of the file, skipping any specified header rows, and processes each record in the CSV format. 
  /// It validates required fields such as "FirstName" and "LastName", checks for duplicates, and ensures that phone numbers and emails are correctly formatted. 
  /// If any errors are encountered during processing, they are collected and returned in the result. 
  /// The method also handles bulk insertion of valid records into the database and logs the import process.
  /// The final result includes a summary of processed records, any errors encountered, and the total time taken for the import.
  /// </remarks>
  public JsonResult Import(int rowId)
  {
    var file =
      Db.ImportFile.SingleOrDefault(
        fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == rowId);
    if (file == null)
      return new
      {
        failed = true,
        result = new[] { "File not found" }
      }.AsJsonResult();

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
    var fileString = file.Contents.AsString(file.CodePage);

    var firstDataRow = file.FirstDataRow.AsInt();
    var numFirstRowsToSkip = firstDataRow - 2;
    if (numFirstRowsToSkip > 0)
    {
      // 1 based... headers on line 1, data on line 2. If 2 or less, ignore it.
      fileString = fileString.GetLinesAfterSkipping(numFirstRowsToSkip);
    }
    else
    {
      numFirstRowsToSkip = 1;
    }

    // for some files, CRLF is seen as two lines
    fileString = fileString.Replace("\r\n", "\r");

    var textReader = new StringReader(fileString);
    var csv = new CsvReader.CsvReader(textReader, true, ',', '"', '"', '#', ValueTrimmingOptions.All, 4096)
    {
      // had to provide all parameters in order to set ValueTrimmingOption.All
      SkipEmptyLines = false,
      MissingFieldAction = MissingFieldAction.ReplaceByEmpty,
      SupportsMultiline = false
    };

    //mapping:   csv->db,csv->db
    var currentMappings =
      columnsToRead.DefaultTo("").SplitWithString(",").Select(s => s.SplitWithString(_mappingSymbol)).ToList();
    var dbFields = DbFieldsList.ToList();
    var validMappings = new List<string[]>();
    try
    {
      validMappings = currentMappings.Where(mapping => dbFields.Contains(mapping[1])).ToList();
    }
    catch (Exception) {
      return new
      {
        failed = true,
        result = new[] { "Unable to read headings. Are they on the line in the file?" }
      }.AsJsonResult();
    }

    if (validMappings.Count == 0)
      return new
      {
        failed = true,
        result = new[] { "Mapping not defined" }
      }.AsJsonResult();

    var mappedFields = dbFields.Where(f => validMappings.Select(m => m[1]).Contains(f)).ToList();
    if (!mappedFields.Contains("LastName"))
    {
      return new
      {
        failed = true,
        result = new[] { "Last Name must be included" }
      }.AsJsonResult();
    }
    if (!mappedFields.Contains("FirstName"))
    {
      return new
      {
        failed = true,
        result = new[] { "First Name must be included" }
      }.AsJsonResult();
    }

    var phoneNumberChecker = new Regex(@"\+[0-9]{4,15}");
    var phoneNumberCleaner = new Regex(@"[^\+0-9]");
    var emailChecker = new Regex(@".*@.*\..*");
    var numRequiredFields = 2;

    var currentPeople = new PersonCacher(Db).AllForThisElection.ToList();
    currentPeople.ForEach(p => p.TempImportLineNum = -1);

    var knownEmails = currentPeople.Where(p => p.Email != null).Select(p => p.Email.ToLower()).ToList();
    var knownPhones = currentPeople.Where(p => p.Phone != null).Select(p => p.Phone).ToList();

    var personModel = new PeopleModel();
    // var defaultReason = new ElectionModel().GetDefaultIneligibleReason();

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
    var currentLineNum = 0;

    hub.StatusUpdate("Processing", true);

    while (csv.ReadNextRecord() && continueReading)
    {
      if (csv.GetCurrentRawData() == null) continue;

      currentLineNum = numFirstRowsToSkip + (int)csv.CurrentRecordIndex + 1;

      var valuesSet = false;
      var requiredFieldsFound = 0;
      var requiredWarningGiven = false;
      var errorInRow = false;

      var duplicateInFileSearch = currentPeople.AsQueryable();
      var doDupQuery = false;

      var person = new Person
      {
        TempImportLineNum = currentLineNum
      };

      foreach (var currentMapping in validMappings)
      {
        var csvColumnName = currentMapping[0];
        var dbFieldName = currentMapping[1];

        string value;
        try
        {
          // remove any hard spaces and trim
          value = (csv[csvColumnName] ?? "").Replace('\u00A0', ' ').Trim();
        }
        catch (Exception e)
        {
          result.Add($"~E Line {currentLineNum} - {e.Message.Split('\r')[0]}. Are there \"\" marks missing?");
          errorInRow = true;
          continueReading = false;
          break;
        }

        _dict.TryGetValue(dbFieldName, out var maxSize);
        if (value.Length > maxSize)
        {
          result.Add($"~E Line {currentLineNum} - {dbFieldName} is too long. Max length is {maxSize}.");
          requiredWarningGiven = true;
          errorInRow = true;
          // continueReading = false;
          break;
        }

        var rawValue = HttpUtility.HtmlEncode(value);
        var originalValue = value;

        switch (dbFieldName)
        {
          case "IneligibleReasonGuid":
            // match value to the list of Enums
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
                  person.IneligibleReasonGuid = match.Value;
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
          case "FirstName":
          case "LastName":
            if (value == "")
            {
              result.Add($"~E Line {currentLineNum} - \"{csvColumnName}\" is required");
              requiredWarningGiven = true;
            }
            else
            {
              person.SetPropertyValue(dbFieldName, value);
            }

            break;

          default:
            person.SetPropertyValue(dbFieldName, value);
            break;
        }

        valuesSet = valuesSet || value.HasContent();

        if (value.HasContent())
        {
          doDupQuery = true;
          switch (dbFieldName)
          {
            case "LastName":
              duplicateInFileSearch = duplicateInFileSearch.Where(p => p.LastName == value);
              requiredFieldsFound++;
              break;
            case "FirstName":
              duplicateInFileSearch = duplicateInFileSearch.Where(p => p.FirstName == value);
              requiredFieldsFound++;
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

                if (!errorInRow) knownEmails.Add(value);
              }

              break;
            case "Phone":
              if (value.HasContent())
              {
                value = phoneNumberCleaner.Replace(value, "");

                if (!value.StartsWith("+") && value.Length == 10)
                  // assume north american
                  value = "+1" + value;

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
        if (!requiredWarningGiven)
        {
          // don't count it as an error
          result.Add($"~I Line {currentLineNum} - Ignoring blank line");
          addRow = false;
        }
      }
      else if (requiredFieldsFound != numRequiredFields || errorInRow)
      {
        addRow = false;
        rowsWithErrors++;

        if (requiredFieldsFound != numRequiredFields && !requiredWarningGiven)
        {
          result.Add($"~E Line {currentLineNum} - A required field is missing");
          requiredWarningGiven = true;
        }
      }

      if (doDupQuery)
      {
        var duplicates = duplicateInFileSearch.Select(p => p.TempImportLineNum).Distinct().ToList();
        if (duplicates.Any())
        {
          addRow = false;

          if (duplicates.All(n => n == -1))
          {
            result.Add(
              $"~I Line {currentLineNum} - {person.FirstName} {person.LastName} - skipped - Matching person found in existing records");
          }
          else
          {
            peopleSkipped++;
            foreach (var n in duplicates.Where(n => n > 0))
              result.Add(
                $"~E Line {currentLineNum} - {person.FirstName} {person.LastName} - Duplicate person found on line {n}");
          }
        }
      }


      if (addRow)
      {
        //get ready for DB
        person.ElectionGuid = UserSession.CurrentElectionGuid;
        person.PersonGuid = Guid.NewGuid();

        personModel.SetCombinedInfoAtStart(person);
        personModel.ApplyVoteReasonFlags(person);

        peopleToLoad.Add(person);

        // result.Add($"~I Line {currentLineNum} - {person.FirstName} {person.LastName}");  -- not good for large lists!

        peopleAdded++;
      }

      currentPeople.Add(person);

      if (currentLineNum % 100 == 0) hub.ImportInfo(currentLineNum, peopleAdded);

      if (result.Count(s => s.StartsWith("~E")) == 10)
      {
        result.Add("~E Import aborted after 10 errors");
        break;
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

    hub.ImportInfo(currentLineNum, peopleAdded);

    result.AddRange(new[]
    {
      "---------",
      $"Processed {currentLineNum:N0} data line{currentLineNum.Plural()}"
    });
    // if (peopleSkipped > 0)
    // {
    //   result.Add($"{peopleSkipped:N0} duplicate{peopleSkipped.Plural()} ignored.");
    // }
    if (rowsWithErrors > 0)
      result.Add(
        $"{rowsWithErrors:N0} line{rowsWithErrors.Plural("s had errors or were", " had errors or was")} blank.");
    // if (validReasons > 0)
    // {
    //   result.Add($"{validReasons:N0} {validReasons.Plural("people", "person")} with recognized Eligibility Status Reason{validReasons.Plural()}.");
    // }
    if (unexpectedReasons.Count > 0)
    {
      result.Add(
        $"{unexpectedReasons.Count:N0} Eligibility Status Reason{unexpectedReasons.Count.Plural()} not recognized: ");
      foreach (var r in unexpectedReasons)
        result.Add("&nbsp; &nbsp; \"{0}\"{1}".FilledWith(r.Key, r.Value == 1 ? "" : " x" + r.Value));
    }

    result.Add("---------");

    if (abort)
    {
      result.Add("Import aborted due to errors in file. Please correct and try again.");
    }
    else
    {
      result.Add($"Added {peopleAdded:N0} {peopleAdded.Plural("people", "person")}.");
      result.Add($"Import completed in {(DateTime.Now - start).TotalSeconds:N1} s.");
    }

    var resultsForLog = result.Where(s => !s.StartsWith("~"));
    new LogHelper().Add("Imported file #" + rowId + ":\r" + resultsForLog.JoinedAsString("\r"), true);

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

        var result = regMatch.Success
          ? $"An email address ({regMatch.Groups[2].Value}) is duplicated in the import file. Import halted."
          : "An email address is duplicated in the import file. Import halted.";

        return result;
      }

      if (msg.Contains("IX_PersonPhone"))
      {
        var regMatch = Regex.Match(msg, @"\((.*), (.*)\)");

        var result = regMatch.Success
          ? $"A phone number ({regMatch.Groups[2].Value}) is duplicated in the import file. Import halted."
          : "A phone number is duplicated in the import file. Import halted.";

        return result;
      }


      return msg;
    }
  }

  public int NumberOfPeople => new PersonCacher(Db).AllForThisElection.Count;

  public JsonResult SaveCodePage(int id, int codepage)
  {
    var fileInfo = Db.ImportFile.SingleOrDefault(
      fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == id);
    if (fileInfo == null) throw new ApplicationException("File not found");

    fileInfo.CodePage = codepage;

    Db.SaveChanges();

    return new { Message = "" }.AsJsonResult();
  }

  public JsonResult SaveDataRow(int id, int firstDataRow)
  {
    var fileInfo = Db.ImportFile.SingleOrDefault(
      fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == id);
    if (fileInfo == null) throw new ApplicationException("File not found");

    fileInfo.FirstDataRow = firstDataRow;

    Db.SaveChanges();

    return new { Message = "" }.AsJsonResult();
  }
}