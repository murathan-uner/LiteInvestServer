using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ServerEntity;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvestServerDll
{
    public class WorkerDB : AWorkerDB
    {
        private IDbContextFactory<AppDBContext> dbContextFactory;

        public WorkerDB(IDbContextFactory<AppDBContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory;
           // using (var db = dbContextFactory.CreateDbContext()) { } ;

        }

        public override void Open()
        {
            throw new NotImplementedException();
        }

        internal override void Close()
        {
            throw new NotImplementedException();
        }

        internal override ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>> GetClosedPositions()
        {
            throw new NotImplementedException();
        }

        internal override (ConcurrentDictionary<BoardEnum, CommissionBase> CommissionBroker, ConcurrentDictionary<string, CommissionBase> CommissionPayOut, ConcurrentDictionary<BoardEnum, CommissionBase>? CommissionProp) GetComission()
        {
            throw new NotImplementedException();
        }

        internal override ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>> GetOpenedPositions()
        {
            throw new NotImplementedException();
        }

        internal override ConcurrentDictionary<string, ConcurrentDictionary<int, Order>> GetOrders()
        {
            throw new NotImplementedException();
        }

        internal override ConcurrentDictionaryDB<SecurityApi> GetSecuritis()
        {
            if (Securities == null)
            {
                Securities = new ConcurrentDictionaryDB<SecurityApi>();
            }
            return Securities;
        }

        internal override ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>> GetTrades()
        {
            throw new NotImplementedException();
        }

        internal override ConcurrentBag<Transaction> GetTransaction()
        {
            throw new NotImplementedException();
        }

        internal override List<AccountTrade> GetAccountsTrade()
        {
            throw new NotImplementedException();
        }

        internal override ConcurrentDictionaryDB<User> GetUsersContext()
        {
            if (UsersContext == null)
            {
                UsersContext = new ConcurrentDictionaryDB<User>();
            }
            return UsersContext;
        }

        internal override void SaveComission()
        {
            throw new NotImplementedException();
        }

        internal override void SaveTransaction()
        {
            throw new NotImplementedException();
        }

        internal override void SaveAccountsTrade()
        {
            throw new NotImplementedException();
        }

        internal override List<AccountCapex> GetAccountsCapex()
        {
            throw new NotImplementedException();
        }

        internal override void SaveAccountsCapex()
        {
            throw new NotImplementedException();
        }

        internal override ConcurrentDictionary<DateOnly, List<ClearingResult>> GetClearing()
        {
            throw new NotImplementedException();
        }

        internal override void SaveClearing()
        {
            throw new NotImplementedException();
        }
    }
}
