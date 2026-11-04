using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ServerEntity;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvestServerDll
{

    public abstract class AWorkerDB
    {
        internal ConcurrentDictionaryDB<SecurityApi>? Securities;
        internal ConcurrentDictionaryDB<User>? UsersContext;
        internal ConcurrentDictionary<string, ConcurrentDictionary<int, Order>>? Orders;
        internal ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>> Trades;
        internal ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>>? OpenedPositions;
        internal ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>>? ClosedPositions;
        
        internal List<AccountTrade>? AccountsTrade;
        internal List<AccountCapex>? AccountsCapex;


        internal ConcurrentBag<Transaction> TransactionAccounts;

        internal ConcurrentDictionary<BoardEnum, CommissionBase>? CommissionBroker;
        internal ConcurrentDictionary<string, CommissionBase>? CommissionPayOut;
        internal ConcurrentDictionary<BoardEnum, CommissionBase>? CommissionProp;

        internal ConcurrentDictionary<DateOnly, List<ClearingResult>> Clearing;


        public AWorkerDB()
        {

        }

        public abstract void Open();
        internal abstract void Close();
        internal abstract ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>> GetClosedPositions();
        internal abstract ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>> GetOpenedPositions();
        internal abstract ConcurrentDictionary<string, ConcurrentDictionary<int, Order>> GetOrders();
        internal abstract ConcurrentDictionaryDB<SecurityApi> GetSecuritis();
        internal abstract ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>> GetTrades();
        internal abstract ConcurrentDictionaryDB<User> GetUsersContext();


        internal abstract List<AccountTrade> GetAccountsTrade();
        internal abstract void SaveAccountsTrade();

        internal abstract List<AccountCapex> GetAccountsCapex();
        internal abstract void SaveAccountsCapex();


        internal abstract ConcurrentBag<Transaction> GetTransaction();
        internal abstract void SaveTransaction();

        internal abstract (ConcurrentDictionary<BoardEnum, CommissionBase> CommissionBroker, ConcurrentDictionary<string, CommissionBase> CommissionPayOut, ConcurrentDictionary<BoardEnum, CommissionBase> CommissionProp) GetComission();

        internal abstract void SaveComission();
        internal abstract ConcurrentDictionary<DateOnly, List<ClearingResult>> GetClearing();

        internal abstract void SaveClearing();
    }
}
