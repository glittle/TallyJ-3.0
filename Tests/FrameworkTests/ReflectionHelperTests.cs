using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code;
using Tests.Support;

namespace Tests.FrameworkTests
{
  [TestClass]
  public class ReflectionHelperTests
  {

    /// <summary>
    /// Tests the GetNameForObject method of the ReflectionHelper class.
    /// </summary>
    /// <remarks>
    /// This method tests the GetNameForObject method of the ReflectionHelper class by creating an instance of the TestPropertyNames class and calling the GetName method with different property accessors to verify that the correct property names are returned.
    /// </remarks>
    [TestMethod]
    public void GetNameForObject_Test()
    {
      var sut = new TestPropertyNames();

      ReflectionHelper.GetName(() => sut.D2).ShouldEqual("D2");
      ReflectionHelper.GetName(() => sut.S2).ShouldEqual("S2");
      ReflectionHelper.GetName(() => sut.I2).ShouldEqual("I2");
    }

    /// <summary>
    /// Tests the functionality of the GetName method in the ReflectionHelper class by retrieving the name of properties from the TestPropertyNames class.
    /// </summary>
    /// <remarks>
    /// This test method initializes an instance of the TestPropertyNames class and then uses the ReflectionHelper.GetName method to retrieve the names of its properties.
    /// The expected property names are "D2", "S2", and "I2" for the respective properties in the TestPropertyNames class.
    /// The test asserts that the retrieved property names match the expected values.
    /// </remarks>
    [TestMethod]
    public void GetNameForClass_Test()
    {
      var sut = default(TestPropertyNames);
      ReflectionHelper.GetName(() => sut.D2).ShouldEqual("D2");
      ReflectionHelper.GetName(() => sut.S2).ShouldEqual("S2");
      ReflectionHelper.GetName(() => sut.I2).ShouldEqual("I2");
    }

    /// <summary>
    /// Tests the GetNameExt method by asserting that it returns the expected property name.
    /// </summary>
    /// <remarks>
    /// This test method validates the functionality of the GetNameExt method by asserting that it returns the expected property name for the specified property.
    /// It tests both the scenario where the input object is null and when it is not null.
    /// </remarks>
    [TestMethod]
    public void GetNameExt_Test()
    {
      ((TestPropertyNames)null).GetPropertyName(x => x.D2).ShouldEqual("D2");

      var obj = new TestPropertyNames();
      obj.GetPropertyName(x => x.D2).ShouldEqual("D2");
    }


  }

  internal class TestPropertyNames
  {
#pragma warning disable 0649
    public string S1;
    public string S2;
    public int I2;
    public DateTime D2;
#pragma warning restore 0649
  }
}