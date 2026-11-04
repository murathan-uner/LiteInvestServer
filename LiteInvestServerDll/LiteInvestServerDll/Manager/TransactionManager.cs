using LiteInvest.Entity.Helpers;
using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ReportEntity;
using LiteInvest.Entity.ServerEntity;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvestServerDll
{
    public class TransactionManager :NotifyPropertyBase
    {
        private static ServerService serverService;

        private static ServiceDB serviceDB;

        private static TransactionManager Instance;

        /// <summary>
        /// Dependence Injection
        /// </summary>
        /// <param name="_serverService"></param>
        /// <param name="_serviceDB"></param>
        public static  void Open(ServerService _serverService, ServiceDB _serviceDB)
        {
            serverService = _serverService;
            serviceDB = _serviceDB;

            TransactionManager.transactions = serviceDB.GetTransactions();
            serviceDB.TransactionEventHandler(Instance);

        }

        /// <summary>
        /// key = username
        /// </summary>
        private static ConcurrentBag<Transaction> transactions { get; set; }

        /// <summary>
        /// key = userName
        /// </summary>
        private static ConcurrentBag<Transaction> Transactions
        {
            get => TransactionManager.transactions;

            set { }
        }

        /// <summary>
        /// Создать сервис
        /// </summary>
        public TransactionManager()
        {
            Instance = this;
        }

        public static async Task AddTransaction(Transaction _transaction)
        {
            await Task.Run(() =>
            {
                Transactions.Add(_transaction);
                
                ServerLogger.Log(_transaction.ToString(), "TransactionAccount");
                Instance.OnPropertyChanged();
            });
            
        }


    }
}
