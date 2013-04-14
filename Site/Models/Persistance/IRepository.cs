using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TallyJ.Models.Persistance
{
    public interface IRepository
    {
        T Get<T>(int id) where T : Entity;
        IQueryable<T> GetAll<T>() where T : Entity;
        void SaveOrUpdate<T>(T entity) where T : Entity;
        void Delete<T>(T entity) where T : Entity;
    }
}