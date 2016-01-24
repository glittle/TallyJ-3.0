using System;
using Microsoft.Practices.Unity;
using UnityContainerExtensions = Microsoft.Practices.Unity.Configuration.UnityContainerExtensions;

namespace TallyJ.Code.UnityRelated
{
	///<summary>Service locator for dependency injection .</summary>
	public class UnityInstance
	{
		///<summary>Dpendency Injection container.</summary>
		static UnityInstance()
		{
			Container = new UnityContainer();
			try
			{
				UnityContainerExtensions.LoadConfiguration(Container);
			}
			catch (ArgumentNullException e)
			{
				if (e.ParamName.Equals("section", StringComparison.OrdinalIgnoreCase))
				{
					throw new Exception("The neccesary configuration for Unity has not been added to the web.config file.");
				}
			}
		}

		/// <summary>
		/// Current container
		/// </summary>
		public static UnityContainer Container { get; private set; }

		/// <summary>Get the implementer of the named interface</summary>
		/// <typeparam name="TService">Interface</typeparam>
		public static TService Resolve<TService>()
		{
			return Container.Resolve<TService>();
		}

		/// <summary>Get the implementer of the named interface</summary>
		/// <typeparam name="TService">Interface</typeparam>
		public static TService Resolve<TService>(params ResolverOverride[] overrides)
		{
			return Container.Resolve<TService>(overrides);
		}

    public static void Offer<TService>(TService service) {
      Container.RegisterInstance(service);
    }
	}
}