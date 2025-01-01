using System;

namespace TallyJ.Code.Session;

public class GuidStringPair(Guid guid, string name)
{
  public string Name { get; } = name;
  public Guid Guid { get; } = guid;
}