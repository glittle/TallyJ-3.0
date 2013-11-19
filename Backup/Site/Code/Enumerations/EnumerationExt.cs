//using System;
//using System.Linq;
//using System.Web;

//namespace TallyJ.Code.Enumerations
//{
//  public static class EnumerationExt
//  {
//    public static HtmlString ForHtmlSelect<T, TV>(this IEnumeration<T, TV> input, string selected = "")
//      where T : BaseEnumeration<T, TV> where TV : IEquatable<TV>
//    {
//      return
//        input.Items
//          .Select(bi => "<option value='{0}'{2}>{1}</option>"
//                          .FilledWith(bi.Value, bi.Text, bi.Value.ToString() == selected ? " selected" : ""))
//          .JoinedAsString()
//          .AsRawHtml();
//    }
//  }
//}