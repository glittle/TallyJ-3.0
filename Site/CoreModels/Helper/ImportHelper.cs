using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  public class ImportHelper
  {

    public static Dictionary<int, string> Encodings
    {
      get
      {
        return new Dictionary<int, string>
                 {
                   {65001, "UTF-8"},
                   {1200, "UTF-16"},
                   {1252, "Code page 1252"},
                 };
      }
    }

    public static void ExtraProcessingIfMultipartEncoded(ImportFile record)
    {
      if (record.CodePage == null)
      {
        return;
      }

      const string multipartDividerPrefix = "-----------------------------";
      // foreach (var codePage in Encodings.Select(encoding => encoding.Key))
      {
        var textReader = new StringReader(record.Contents.AsString(record.CodePage));
        var line = textReader.ReadLine();
        if (line == null)
        {
          textReader.Dispose();
          return;
        }
        if (!line.StartsWith(multipartDividerPrefix))
        {
          textReader.Dispose();
          return;
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
          return;
        }

        record.Contents = Encoding.GetEncoding(record.CodePage.Value).GetBytes(lines.JoinedAsString("\r\n", false));
        // record.CodePage = codePage;

        return;
      }
    }

    /// <summary>
    /// Improved method
    /// </summary>
    /// <remarks>Added to StackOverflow: https://stackoverflow.com/a/67154016/32429 </remarks>
    /// <param name="contents"></param>
    /// <returns></returns>
    public static Encoding DetectCodePage(byte[] contents)
    {
      if (contents == null || contents.Length == 0)
      {
        return Encoding.Default;
      }

      return TestCodePage(Encoding.UTF8, contents)
             ?? TestCodePage(Encoding.Unicode, contents)
             ?? TestCodePage(Encoding.BigEndianUnicode, contents)
             ?? TestCodePage(Encoding.GetEncoding(1252), contents) // Western European
             ?? TestCodePage(Encoding.GetEncoding(28591), contents) // ISO Western European
             ?? TestCodePage(Encoding.ASCII, contents)
             ?? TestCodePage(Encoding.Default, contents); // likely Unicode
    }


    private static Encoding TestCodePage(Encoding testCode, byte[] byteArray)
    {
      try
      {
        var encoding = Encoding.GetEncoding(testCode.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        var a = encoding.GetCharCount(byteArray);
        return testCode;
      }
      catch (Exception e)
      {
        return null;
      }
    }

    // public static string ConvertToString(byte[] importFileContents)
    // {
    //   if (importFileContents == null || importFileContents.Length == 0)
    //   {
    //     return null;
    //   }
    //
    //   return ConvertWithCodePage(Encoding.UTF8, importFileContents)
    //          ?? ConvertWithCodePage(Encoding.Unicode, importFileContents)
    //          ?? ConvertWithCodePage(Encoding.ASCII, importFileContents)
    //          ?? ConvertWithCodePage(Encoding.BigEndianUnicode, importFileContents)
    //          ?? ConvertWithCodePage(Encoding.Default, importFileContents);
    // }
    //
    // private static string ConvertWithCodePage(Encoding testCode, byte[] byteArray)
    // {
    //   try
    //   {
    //     var encoding = Encoding.GetEncoding(testCode.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    //     var s = encoding.GetString(byteArray);
    //     return s;
    //   }
    //   catch (Exception e)
    //   {
    //     Console.WriteLine(e);
    //     return null;
    //   }
    //
    // } 

  }
}