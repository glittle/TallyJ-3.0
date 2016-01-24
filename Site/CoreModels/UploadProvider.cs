using System;
using System.IO;
using Krystalware.SlickUpload;
using Krystalware.SlickUpload.Configuration;
using Krystalware.SlickUpload.Storage;
using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;

using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class UploadProvider : UploadStreamProviderBase
  {
    private ITallyJDbContext _db;

    /// <summary>
    ///     Access to the database
    /// </summary>
    protected ITallyJDbContext Db
    {
      get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
    }

    public UploadProvider(UploadStreamProviderElement settings) : base(settings)
    {
    }

    /// <Summary>Save file</Summary>
    public override Stream GetWriteStream(UploadedFile file)
    {
      //
      //  Not working... file.ServerLocation is empty, however, the HttpRequest has the file contents
      //
      //
      //
      //
      //
      //




      var longLength = file.ContentLength;
      var contentLength = longLength > Int32.MaxValue ? 0 : (int)longLength;

      var buffer = new byte[contentLength];
      var start = 0;
      using (var fileStream = File.OpenRead(file.ServerLocation))
      {
        int count;
        while ((count = fileStream.Read(buffer, start, contentLength - start)) > 0)
        {
          start += count;
        }
      }

      var record = new ImportFile
                     {
                       Contents = buffer,
                       FileSize = contentLength,
                       OriginalFileName = file.ClientName,
                       UploadTime = DateTime.Now,
                       ProcessingStatus = "Imported"
                     };

      Db.ImportFile.Add(record);
      Db.SaveChanges();

      return null;
    }

    public override void RemoveOutput(UploadedFile file)
    {
      throw new NotImplementedException();
    }

    /// <Summary>Read file</Summary>
    public override Stream GetReadStream(UploadedFile file)
    {
      throw new NotImplementedException();
    }
  }
}