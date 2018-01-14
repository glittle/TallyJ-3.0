using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ImportHelper
  {

    public static Dictionary<int, string> Encodings
    {
      get
      {
        return new Dictionary<int, string>
                 {
                   {1252, "English & European"},
                   {65001, "UTF-8"},
                   {1200, "UTF-16"},
                 };
      }
    }

    public void ExtraProcessingIfMultipartEncoded(ImportFile record)
    {
      const string multipartDividerPrefix = "-----------------------------";
      foreach (var codePage in Encodings.Select(encoding => encoding.Key))
      {
        var textReader = new StringReader(record.Contents.AsString(codePage));
        var line = textReader.ReadLine();
        if (line == null)
        {
          textReader.Dispose();
          continue;
        }
        if (!line.StartsWith(multipartDividerPrefix))
        {
          textReader.Dispose();
          continue;
        }

        // this file is encoded...
        //line1	"-----------------------------7dc1372120770"	
        //line2	"Content-Disposition: form-data; name=\"qqfile\"; filename=\"C:\\Temp\\sampleCommunity.csv\""	
        //line3	"Content-Type: application/vnd.ms-excel"	
        //line4	""	
        //line5	"Given,Surname,Other,ID,Group,Email,Phone"	

        line = textReader.ReadLine();

        try
        {
          var split = line.Split(new[] { "filename=" }, StringSplitOptions.None);
          record.OriginalFileName = Path.GetFileName(split[1].Replace("\"", ""));
        }
        catch
        {
          // swallow it and move on
        }
        textReader.ReadLine(); // 3

        var lines = new List<string>();

        line = textReader.ReadLine();
        while (line != null)
        {
          if (!line.StartsWith(multipartDividerPrefix))
          {
            lines.Add(line);
          }
          line = textReader.ReadLine();
        }

        if (lines.Count == 0)
        {
          textReader.Dispose();
          continue;
        }

        record.Contents = Encoding.GetEncoding(codePage).GetBytes(lines.JoinedAsString("\r\n", false));
        record.CodePage = codePage;

        return;
      }
    }

  }
}