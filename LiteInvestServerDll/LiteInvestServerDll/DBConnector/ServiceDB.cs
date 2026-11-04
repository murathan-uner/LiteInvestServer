using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ServerEntity;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace LiteInvestServerDll
{
    /// <summary>
    /// Сервис сохранения данных, он должен быть Singleton,
    /// повторные эксземпляры не создавать
    /// </summary>
    public class ServiceDB
    {
        private static ServiceDB? Instance;

        private AWorkerDB workerDBXML;
        private AWorkerDB workerDBSQL;

        private IDbContextFactory<AppDBContext> dbContextFactory;

        public ServiceDB(IDbContextFactory<AppDBContext> _dbContextFactory)
        {
            if (Instance != null)
            {
                throw new Exception("Сервис сохранения данных уже создан. Нельзя создавать несколько экземпляров.");
            }
            //if (PlazaOptions.MyDBSaver == DBSaver.XML)
            {
                workerDBXML = new WorkerDBXML();
            }
            //else if (PlazaOptions.MyDBSaver == DBSaver.DB)
            {
                dbContextFactory = _dbContextFactory;
                workerDBSQL = new WorkerDB(dbContextFactory);
            }
            Instance = this;
            

        }

        /// <summary>
        /// Подключиться к базе данных
        /// </summary>
        public void Open()
        {
            workerDBXML.Open();
        }

        public void Close()
        {
            workerDBXML.Close();
        }

        public ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>> GetClosedPositions()
        {
            return workerDBXML.GetClosedPositions();
        }

        public ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>> GetOpenedPositions()
        {
            return workerDBXML.GetOpenedPositions();
        }

        public ConcurrentDictionary<string, ConcurrentDictionary<int, Order>> GetOrders()
        {
            return workerDBXML.GetOrders();
        }

        public ConcurrentDictionaryDB<SecurityApi> GetSecuritys()
        {
            return workerDBSQL.GetSecuritis();
            //return workerDBXML.GetSecuritis();
        }

        public ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>> GetTrades()
        {
            return workerDBXML.GetTrades();
        }

        public ConcurrentDictionaryDB<User> GetUsersContext()
        {
            return workerDBSQL.GetUsersContext();
            //return workerDBXML.GetUsersContext();
        }


        #region Accounts

        public List<AccountTrade> GetAccountsTrade()
        {
            return workerDBXML.GetAccountsTrade();
        }


        /// <summary>
        /// Привязка 
        /// </summary>
        /// <param name="accountManager"></param>
        public void AccountsTradeEventHandler(AccountManager accountManager)
        {
            accountManager.PropertyChanged += AccountManager_PropertyChanged; ;
        }

        private void AccountManager_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // необходимо сохранить UserAccount
            workerDBXML.SaveAccountsTrade();
            workerDBXML.SaveAccountsCapex();
        }

        

        public List<AccountCapex> GetAccountsCapex()
        {
            return workerDBXML.GetAccountsCapex();
        }


        

        #endregion Accounts

        #region Transaction

        internal ConcurrentBag<Transaction> GetTransactions()
        {
            return workerDBXML.GetTransaction();
        }

        internal void TransactionEventHandler(TransactionManager transactionManager)
        {
            transactionManager.PropertyChanged += TransactionManager_PropertyChanged;
        }

        private void TransactionManager_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // необходимо сохранить Transaction
            workerDBXML.SaveTransaction();
        }
        #endregion Transaction

        #region Commission

        internal (ConcurrentDictionary<BoardEnum, CommissionBase> CommissionBroker, ConcurrentDictionary<string, CommissionBase> CommissionPayOut, ConcurrentDictionary<BoardEnum, CommissionBase> CommissionProp) GetComission()
        {
           return workerDBXML.GetComission();
        }

        internal void CommissionEventHandler(CommissionManager commissionManager)
        {
            commissionManager.PropertyChanged += CommissionManager_PropertyChanged;
        }

        private void CommissionManager_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            workerDBXML.SaveComission();
        }

        #endregion Commission

        #region Clearing   


        internal ConcurrentDictionary<DateOnly, List<ClearingResult>> GetClearingList()
        {
            return new ConcurrentDictionary<DateOnly, List<ClearingResult>>();
           // return workerDB.GetClearing();
        }

        /// <summary>
        /// Привязка 
        /// </summary>
        /// <param name="accountManager"></param>
        public void ClearingEventHandler(AccountManager accountManager)
        {
            accountManager.PropertyChanged += Clearing_PropertyChanged; ;
        }

        private void Clearing_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            return;
            if (e.PropertyName == "Clearing")
            {
                workerDBXML.SaveClearing();
            }
        }

        #endregion Clearing
    }
}
