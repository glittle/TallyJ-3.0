using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.Models
{
	public class HomeViewModel : BaseViewModel
	{
		public IEnumerable<Election> MyElections
		{
			get { return DbContext.Elections.ToList(); }
		}
	}
}