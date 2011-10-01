using System.Collections;
using System.Collections.Generic;
using TallyJ.Code;
using TallyJ.EF;
using System.Linq;

namespace TallyJ.Models
{
	public class HomeViewModel : BaseViewModel
	{
		public IEnumerable<Election> ElectionList()
		{
			return DbContext.Elections.ToList();
		}
	}
}