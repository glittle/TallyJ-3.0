using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Xml;
using TallyJ.Code.Session;

namespace TallyJ.Code.Resources
{
  public class XmlHelper
  {
    public virtual XmlDocument GetCachedXmlFile(string sPath)
    {
      return GetCachedXmlFile(sPath, false, false);
    }

    public virtual XmlDocument GetCachedXmlFile(string sPath, bool bForceReload)
    {
      return GetCachedXmlFile(sPath, bForceReload, false);
    }

    public virtual XmlDocument GetCachedXmlFile(string path, bool forceReload, bool isCachedByUser)
    {
      //' check the cache for the desired document... if not there, get it from file
      XmlDocument doc = null;
      string nameInCache = "XML." + path;

      //' try to get it from cache if not forcing a reload
      if (!forceReload)
      {
        if (isCachedByUser)
        {
          doc = (XmlDocument)UserSession.CurrentContext.Session[nameInCache];
        }
        else
        {
          doc = (XmlDocument)HttpContext.Current.Cache[nameInCache];
        }
      }

      if (forceReload || doc == null)
      {
        //' don't have it yet... get it
        doc = GetXmlFile(path);

        if (doc == null)
        {
          if (isCachedByUser)
          {
            UserSession.CurrentContext.Session.Remove(nameInCache); // 'okay if not there...
          }
          else
          {
            HttpContext.Current.Cache.Remove(nameInCache); //'okay if not there...
          }
        }
        else
        {
          //'got it, store it in cache
          if (isCachedByUser)
          {
            UserSession.CurrentContext.Session[nameInCache] = doc; //  ', New System.Web.Caching.CacheDependency(sPath))
          }
          else
          {
            HttpContext.Current.Cache.Insert(nameInCache, doc, new CacheDependency(path));
          }
        }
      }

      return doc;
    }

    /// <summary>directly read the XML file into a document</summary>
    /// <param name="sFullPath"></param>
    public virtual XmlDocument GetXmlFile(string sFullPath)
    {
      StreamReader sReader = null;
      XmlDocument doc = new XmlDocument();

      if (File.Exists(sFullPath))
      {
        try
        {
          sReader = new StreamReader(sFullPath, Encoding.GetEncoding(1252));

          doc.Load(sReader);
          sReader.Close();
        }
        catch (Exception ex)
        {
          if (sReader != null)
          {
            sReader.Close();
          }

          throw ex; //  ' tell the caller we failed!
        }
      }
      else //' file was not found
      {
        throw new FileNotFoundException("Configuration file not found: " + Path.GetFileName(sFullPath));
      }

      return doc;
    }
  }
}