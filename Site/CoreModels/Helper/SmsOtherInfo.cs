using Newtonsoft.Json;

namespace TallyJ.CoreModels.Helper
{
  public class SmsOtherInfo
  {
    /// <summary>
    /// Message SID received when sending a message
    /// </summary>
    public string SID { get; set; }

    /// <summary>
    /// Status after delivery callback from Twilio to our URL
    /// </summary>
    public string Status { get; set; }
    public string DateUpdated { get; set; }
    public int? ErrorCode { get; set; }
    public int? NumSegments { get; set; }

    public SmsOtherInfo(string rawData)
    {
      var info = JsonConvert.DeserializeObject<SmsOtherInfo>(rawData);
      SID = info.SID;
      Status = info.Status;
      DateUpdated = info.DateUpdated;
      ErrorCode = info.ErrorCode;
      NumSegments = info.NumSegments;
    }

    /// <summary>
    /// Compress to JSON to store in SQL
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return JsonConvert.SerializeObject(this);
    }
  }
}