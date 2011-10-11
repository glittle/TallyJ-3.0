using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
  public static class Extensions
  {
    public static void ShouldEqual<T>(this T actual, T expected)
    {
      ShouldEqual<T>(actual, expected, null);
    }

    public static void ShouldEqual<T>(this T actual, T expected, string comment)
    {
      Assert.AreEqual(expected, actual, comment);
    }
  }
}