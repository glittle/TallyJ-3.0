using System.Web;
using System.Web.Mvc;
using TallyJ.Code.Data;
using TallyJ.Code.Resources;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Code
{
	public abstract class BaseView : BaseView<string>
	{
		// use string as the model??
		// is there a better way to have two base classes?
	}

	public abstract class BaseView<TModel> : WebViewPage<TModel>
	{
		tallyj2dEntities _db;
		IViewResourcesHelper _viewResourcesHelper;

		/// <summary>Access to the database</summary>
		public tallyj2dEntities DbContext
		{
			get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
		}

		public IHtmlString AddResourceFilesForViews(string extension)
		{
			return _viewResourcesHelper.CreateTagsToReferenceClientResourceFiles(extension);
		}

		protected override void InitializePage()
		{
			base.InitializePage();

			_viewResourcesHelper = UnityInstance.Resolve<IViewResourcesHelper>();
			_viewResourcesHelper.Register(this);
		}
	}
}