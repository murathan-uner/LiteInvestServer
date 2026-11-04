using LiteInvest.Entity.Helpers;
using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ServerEntity;

using System.Collections.Concurrent;

namespace LiteInvestServerDll
{
	
	public class ConcurrentDictionaryDB<T> : ConcurrentDictionary<string, T> where T : ADataBaseClass
	{

		const int SaveAllToDataBasePeriod = 5 * 1000;


        public ConcurrentDictionaryDB() : base()
		{
			List<T>? contextList = DBManager.GetObjectFromDB<T>(q => q.Where(u => true));

			contextList.ForEach(u =>
			{
				if (typeof(T) == typeof(User))
				{
                    (u as User).SelfXMLDeSerializer();
					u.PropertyChanged += (s, e) => Base_PropertyChanged(s as T, e);
					base.TryAdd((u as User).Login, u);
				}
				else if (typeof(T) == typeof(SecurityApi))
				{
                    u.PropertyChanged += (s, e) => Base_PropertyChanged(s as T, e);
                    base.TryAdd((u as SecurityApi).id, u);
                }
			});
			Helper.CreateTimerAndStart(SavetoDataBase, SaveAllToDataBasePeriod, true);

		}

        private void SavetoDataBase()
        {
			if (needSave)
			{
                needSave = false;
                foreach (var item in this)
                {
                    DBManager.AddOrUpdateObjectToDB(item.Value);
                }
                needSave = true;
            }
            
        }

		bool needSave = true;

        public new bool TryAdd(string key, T value)
		{
			try
			{
				bool result = base.TryAdd(key, value);
				if (result)
				{
					var resDB = DBManager.TryAddObjectToDB(value);
					if (resDB)
					{
						value.PropertyChanged += (s, e) => Base_PropertyChanged(s as T, e);

						return true;
					}
					else
					{
						base.TryRemove(key, out var v);
						v?.ClearEventPropertyChanged();
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				ServerLogger.Log(ex.Message);
				Console.WriteLine(ex);
			}
			return false;
		}
        
        public new T this[string key]
		{
			get => (T)base[key];
			set
			{
				if (base.ContainsKey(key)) 
				{
                    base[key].ClearEventPropertyChanged();
                    base[key] = value;
                    value.PropertyChanged += (s, e) => Base_PropertyChanged(s as T, e);
                    DBManager.AddOrUpdateObjectToDB(value);
                }
				else
				{
                    TryAdd(key, value);
                }
               
            }
			
		}

		private void Base_PropertyChanged(T sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
			Task.Run(() =>
			{
				DBManager.AddOrUpdateObjectToDB(sender);
			});
        }
    }
}
