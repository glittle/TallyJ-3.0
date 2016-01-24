using EntityFramework.BulkInsert.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TallyJ.EF
{
  public partial class TallyJ2dEntities : ITallyJDbContext
  {
    public void BulkInsert<T>(IEnumerable<T> entities)
    {
      this.BulkInsert(entities, 500);
    }
  }
}
