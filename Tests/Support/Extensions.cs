using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.CoreModels;
using TallyJ.EF;

namespace Tests.Support
{
  public static class Extensions
  {

    /// <summary>
    /// Asserts that the actual value is equal to the expected value and throws an exception if not.
    /// </summary>
    /// <typeparam name="T">The type of the actual and expected values.</typeparam>
    /// <param name="actual">The actual value to be compared.</param>
    /// <param name="expected">The expected value for comparison.</param>
    /// <param name="comment">A message to be included in the exception if the assertion fails.</param>
    /// <exception cref="AssertFailedException">Thrown when the actual value is not equal to the expected value.</exception>
    [DebuggerStepThrough]
    public static void ShouldEqual<T>(this T actual, T expected)
    {
      ShouldEqual<T>(actual, expected, null);
    }

    [DebuggerStepThrough]
    public static void ShouldEqual<T>(this T actual, T expected, string comment)
    {
      Assert.AreEqual(expected, actual, comment);
    }

    /// <summary>
    /// Verifies that the actual value is not equal to the expected value and throws an exception if they are equal.
    /// </summary>
    /// <typeparam name="T">The type of the actual and expected values.</typeparam>
    /// <param name="actual">The actual value to be compared.</param>
    /// <param name="expected">The expected value to be compared against.</param>
    /// <param name="comment">A message to include in the exception if the values are equal.</param>
    /// <exception cref="AssertFailedException">Thrown when the actual value is equal to the expected value.</exception>
    public static void ShouldNotEqual<T>(this T actual, T expected)
    {
      ShouldNotEqual<T>(actual, expected, null);
    }

    public static void ShouldNotEqual<T>(this T actual, T expected, string comment)
    {
      Assert.AreNotEqual(expected, actual, comment);
    }
  }
}