using System;
using TallyJ.CoreModels;

namespace TallyJ.EF
{
  [Serializable]
  public partial class Location : IIndexedForCaching
  {
    public bool IsTheOnlineLocation => Name == LocationModel.OnlineLocationName;
    public bool IsTheImportedLocation => Name == LocationModel.ImportedLocationName;
    public bool IsVirtual => Name == LocationModel.OnlineLocationName || Name == LocationModel.ImportedLocationName;
  }
}