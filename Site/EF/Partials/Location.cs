using System;
using TallyJ.CoreModels;

namespace TallyJ.EF
{
  [Serializable]
  public partial class Location : IIndexedForCaching
  {
    public bool IsTheOnlineLocation => Name == LocationModel.OnlineLocationName;
  }
}