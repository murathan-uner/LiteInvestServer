using Azure.Core.GeoJson;

using LiteInvest.Entity.ReportEntity;
using LiteInvest.Entity.ServerEntity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

using System.Collections.Generic;
using System.Data;

namespace LiteInvestServerDll
{
    public class DBManager
    {
        private IDbContextFactory<AppDBContext> dbContextFactory { get; }

        private static DBManager Instance;

        public DBManager(IDbContextFactory<AppDBContext> dbContextFactory)
        {
            if (Instance != null)
            {
                    throw new Exception("Сервис сохранения данных уже создан. Нельзя создавать несколько экземпляров.");

            }

            this.dbContextFactory = dbContextFactory;

            Instance = this;

        }

        public static bool TryAddObjectToDB<T>(T newobject) where T : ADataBaseClass
        {
            using (var db = Instance.dbContextFactory.CreateDbContext())
            {
                try
                {
                    var dbSet = db.Set<T>();
                    if (dbSet.Any(o => o.Guid == newobject.Guid))
                    {
                        Console.WriteLine($"Объект {newobject.GetType()} guid={newobject.Guid} уже есть в базе данных");
                        dbSet.Update(newobject);
                        return true;
                    }
                    else
                    {
                        dbSet.Add(newobject);
                    }
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            return true;
        }

        public static void AddOrUpdateObjectToDB<T>(T newobject) where T : ADataBaseClass
        {
            using (var db = Instance.dbContextFactory.CreateDbContext())
            {
                try
                {
                    var dbSet = db.Set<T>();
                    var foundObject =  dbSet.Any(o=> o.Guid == newobject.Guid); 

                    if (!foundObject)
                    {
                        dbSet.Add(newobject);       
                    }
                    else
                    {
                        dbSet.Update(newobject);
                    }
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public static void GetDBContext(Action<AppDBContext> action)
        {
            using (var db = Instance.dbContextFactory.CreateDbContext())
            {
                try
                {
                    action(db);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public static List<T> GetObjectFromDB<T>(Func<IQueryable<T>, IQueryable<T>> queryBuilder) where T : ADataBaseClass  // Ограничение: T должен быть ADataBaseClass
        {
            try
            {
                using (var db = Instance.dbContextFactory.CreateDbContext())
                {
                    IQueryable<T> query = db.Set<T>();
                    query = queryBuilder(query);

                    return query.ToList();
                }
            }
            catch (Exception ex)
            {
                return new List<T>();
            }

            /*
             Пример использования:
             var res = DBManager.GetObjectFromDB<AccountCapex>(builder => builder.Where(account => account.DepositCompany > 0));
            */
        }




    }
}
