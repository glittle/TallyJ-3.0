using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using Microsoft.Practices.Unity;

namespace TallyJ.Code.UnityRelated
{
	//public class WCFContextItemsProvider : IContextItemsProvider
	//{
	//  public IDictionary Items
	//  {
	//    get { return WCFContext.Current.Items; }
	//  }
	//}

  public class ContextLifetimeManager : LifetimeManager
	{

		/// Context collection key, the value of which is a list of ContextLifetimeManager

		private static ListDictionary allInstances;

		static ContextLifetimeManager()
		{
			allInstances = new ListDictionary();
		}

		/// The key of the instance being managed in the context provider
		private Guid contextKey = Guid.NewGuid();

    
		public ContextLifetimeManager()
		{

			if (allInstances.Contains(ItemsProvider) == false)
			{
				allInstances.Add(ItemsProvider, new List<ContextLifetimeManager>());
			}

			(allInstances[ItemsProvider] as List<ContextLifetimeManager>).Add(this);
		}

		public static IDictionary ItemsProvider
		{
			get
			{
				return HttpContext.Current.Items;
			}

		}

		/// Retrieves all manages from the current HttpContext and disposes of them
		public static void ReleaseAndDisposeAll()
		{
			if (allInstances.Contains(ItemsProvider))
			{
				foreach (ContextLifetimeManager manager in
						  allInstances[ItemsProvider] as List<ContextLifetimeManager>)
				{
					manager.ReleaseAndDispose();
				}
			}
		}

		protected IDictionary Items
		{
			get
			{
				return ItemsProvider;
			}
		}

		/// Retrieves the managed object
		public override object GetValue()
		{
			if (Items.Contains(contextKey) == false)
			{
				return null;
			}

			return Items[contextKey];
		}

		/// Removes the reference to the managed object.
		/// Note that this does NOT dispose the object
		public override void RemoveValue()
		{
			if (Items.Contains(contextKey))
			{
				Items.Remove(contextKey);
			}
		}

		/// Sets the object to be managed
		public override void SetValue(object instance)
		{
			Items[contextKey] = instance;
		}

		/// Disposes the object if it can be disposed and removes any references to it
		private void ReleaseAndDispose()
		{
			IDisposable disposable = GetValue() as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
			RemoveValue();
		}
	}

}
