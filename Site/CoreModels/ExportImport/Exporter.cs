using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using TallyJ.Code;

namespace TallyJ.CoreModels.ExportImport
{
  public class Exporter : ActionResult
  {
    public const string XmlNameSpace = "urn:tallyj.bahai:v2";

    private readonly object _blob;
    private readonly string _exportName;
    private readonly string _rootName;
    private XmlWriter _writer;

    public Exporter(object blob, string rootName, string exportName)
    {
      _blob = blob;
      _rootName = rootName;
      _exportName = exportName;
    }

    public override void ExecuteResult(ControllerContext context)
    {
      var response = context.HttpContext.Response;
      response.ClearContent();
      response.ContentType = "text/xml";
      response.AddHeader("Content-Disposition", string.Format("attachment; filename={0}.xml", _exportName));

      var settings = new XmlWriterSettings
        {
          Indent = true,
          Encoding = Encoding.UTF8,
          ConformanceLevel = ConformanceLevel.Document,
          CheckCharacters = true,
          NamespaceHandling = NamespaceHandling.OmitDuplicates
        };
      _writer = XmlWriter.Create(response.OutputStream, settings);
      _writer.WriteStartDocument(true);

      AddItem(_blob, _rootName, true);

      _writer.WriteEndDocument();

      _writer.Flush();
      _writer = null;
    }

    private void AddItem(object blob, string name, bool addNameSpace = false)
    {
      var started = false;

      foreach (
        var property in
          blob.GetAllProperties().Where(property => property.Value != null && property.Value.ToString() != string.Empty)
        )
      {
        if (!started)
        {

          if (addNameSpace)
          {
            _writer.WriteStartElement(name, XmlNameSpace);
          }
          else
          {
            _writer.WriteStartElement(name);
          }
          started = true;
        }

        var value = property.Value;
        if (value is String || value is Int32|| value is Guid)
        {
          _writer.WriteAttributeString(property.Key, value.ToString());
        }
        else if (value is DateTime)
        {
          _writer.WriteAttributeString(property.Key, ((DateTime) value).ToString("o"));
        }
        else if (value is Boolean)
        {
          _writer.WriteAttributeString(property.Key, (bool) value ? "true" : "false");
        }
        else if (value is IList)
        {
          foreach (var item in (IList) value)
          {
            AddItem(item, property.Key);
          }
        }
        else
        {
          AddItem(value, property.Key);
        }
      }

      if (started)
      {
        _writer.WriteEndElement();
      }
    }
  }
}