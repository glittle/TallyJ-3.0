using System;
using System.Configuration;
using System.Web.Hosting;
using System.Web.Providers.Entities;

namespace TallyJ.Code.Session
{
	/// <summary>
	///   Information about this web site
	/// </summary>
	public class SiteInfo
	{
		string _rootPath;
		string _rootUrl;

		/// <summary>
		///   Default constructor.
		/// </summary>
		public SiteInfo()
		{
		}

		/// <summary>
		///   For testing only...
		/// </summary>
		/// <param name = "rootUrl"></param>
		/// <param name = "rootPath"></param>
		public SiteInfo(string rootUrl, string rootPath)
		{
			_rootUrl = rootUrl;
			_rootPath = rootPath;
		}

		/// <summary>
		///   Exact Site Mode code as a string.
		/// </summary>
		/// <remarks>
		///   <para>If the Path has a dash, this is everything after the first dash.</para>
		///   <para>If not, check the parent path for a dash in the name.</para>
		///   <para>If not, check the web site URL for a dash in the name.</para>
		///   <para>If none found, return empty string.</para>
		/// </remarks>
		public string CurrentEnvironment
		{
			get { return ConfigurationManager.AppSettings["Environment"].DefaultTo(""); }
		}

	  public string ServerName
	  {
      get { return Environment.MachineName; }
	  }

	  /// <summary>
		///   Root path as defined in Global.asax
		/// </summary>
		public string RootPath
		{
			get { return _rootPath ?? (_rootPath = HostingEnvironment.ApplicationPhysicalPath); }
		}

		/// <summary>
		///   Root rul path as defined in Global.asax
		/// </summary>
		public string RootUrl
		{
			get { return _rootUrl ?? (_rootUrl = HostingEnvironment.ApplicationVirtualPath + "/"); }
		}
	}
}