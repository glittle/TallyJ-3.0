using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;
using TallyJ.Models;

namespace TallyJ.Code
{
	public abstract class DataConnectedModel
	{
		TallyJ2Entities _db;

		/// <summary>Access to the database</summary>
		public TallyJ2Entities Db
		{
			get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
		}
	}
}