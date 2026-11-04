using LiteInvest.Entity.Helpers;
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
    public class WorkerDBXML : AWorkerDB
    {

        static string data;
        DirectoryInfo directoryInfo;
        string securitiesDBName;
        string userDBName;
        string ordersDBName;
        string tradesDBName;
        string openedPositionsDBname;
        string closedPositionsDBname;
        
        string AccountsTradeDBname;
        string AccountsCapexDBname;

        string transactionAccountDBName;
        
        string commissionBrokerDBName;
        string commissionPayOutDBName;
        string commissionPropDBName;
        string clearingDBName;

        public WorkerDBXML() : base()
        {
            data = "C:\\ServerData";
            directoryInfo = new DirectoryInfo(data);
            securitiesDBName = $"{data}\\securities.xml";
            userDBName = $"{data}\\users.xml";
            ordersDBName = $"{data}\\orders.xml";
            tradesDBName = $"{data}\\trades.xml";
            transactionAccountDBName = $"{data}\\transactionAccount.xml";
            openedPositionsDBname = $"{data}\\openedpositions.xml";
            closedPositionsDBname = $"{data}\\closedpositions.xml";

            AccountsTradeDBname = $"{data}\\AccountsTrade.xml";
            AccountsCapexDBname = $"{data}\\AccountsCapex.xml";

            commissionBrokerDBName = $"{data}\\commissionBroker.xml";
            commissionPayOutDBName = $"{data}\\commissionPayOut.xml";
            commissionPropDBName = $"{data}\\commissionProp.xml";

            clearingDBName = $"{data}\\Clearing.xml";
        }

        System.Timers.Timer saveTimer;
        public override void Open()
        {
            if (!Directory.Exists(data))
                System.IO.Directory.CreateDirectory(directoryInfo.ToString());

            //if (File.Exists(securitiesDBName))
            //    Securities = Helper.ReadXml<ConcurrentDictionary<string, SecurityApi>>(securitiesDBName);

            if (File.Exists(userDBName))
            {
          //      UsersContext = (ConcurrentDictionaryDB<User>?)Helper.ReadXml<ConcurrentDictionary<string, User>>(userDBName);
            }

            if (File.Exists(ordersDBName))
                Orders = Helper.ReadXml<ConcurrentDictionary<string, ConcurrentDictionary<int, Order>>>(ordersDBName);

            if (File.Exists(tradesDBName))
                Trades = Helper.ReadXml<ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>>>(tradesDBName);

            if (File.Exists(openedPositionsDBname))
                OpenedPositions = Helper.ReadXml<ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>>>(openedPositionsDBname);

            if (File.Exists(closedPositionsDBname))
            {
                ClosedPositions = Helper.ReadXml<ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>>>(closedPositionsDBname);
            }

           

           



            saveTimer = Helper.CreateTimerAndStart(SaveXML, 10000, true);
        }

        bool needSave = true;

        private void SaveXML()
        {
            if (!needSave)
            {
                return;
            }
            else
            {
                // needSave = false;    // ToDo Надо реализовать потребность сохранения

                if (!PlazaOptions.Simulations && (Securities?.Count ?? 0) > 0)
                    Helper.SaveXml(Securities, securitiesDBName);

                if ((Orders?.Count ?? 0) > 0)
                    Helper.SaveXml(Orders, ordersDBName);

                if ((UsersContext?.Count ?? 0) > 0)
                    Helper.SaveXml(UsersContext, userDBName);

                //if ((OpenedPositions?.Count ?? 0) > 0)
                    Helper.SaveXml(OpenedPositions, openedPositionsDBname);

                if ((ClosedPositions?.Count ?? 0) > 0)
                    Helper.SaveXml(ClosedPositions, closedPositionsDBname);

                if ((Trades?.Count ?? 0) > 0)
                    Helper.SaveXml(Trades, tradesDBName);
            }

        }


        internal override void Close()
        {

        }

        internal override ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>> GetClosedPositions()
        {
            if (ClosedPositions == null)
            {
                ClosedPositions = new();
            }
            return ClosedPositions;
        }

        internal override ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>> GetOpenedPositions()
        {
            if (OpenedPositions == null)
            {
                OpenedPositions = new();
            }
            return OpenedPositions;
        }

        internal override ConcurrentDictionary<string, ConcurrentDictionary<int, Order>> GetOrders()
        {
            if (Orders == null)
            {
                Orders = new();
            }
            return Orders;
        }

        internal override ConcurrentDictionaryDB<SecurityApi> GetSecuritis()
        {
            //if (Securities == null)
            //{
            //    Securities = new();
            //}
            //return Securities;
            return null;
        }

        internal override ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>> GetTrades()
        {
            if (Trades == null)
            {
                Trades = new();
            }
            return Trades;
        }

        internal override ConcurrentDictionaryDB<User> GetUsersContext()
        {
            return null;
            //if (UsersContext == null)
            //{
            //    UsersContext = new();
            //}
            //return (ConcurrentDictionaryDB<User>)UsersContext;
        }

        internal override List<AccountTrade> GetAccountsTrade()
        {
            if (AccountsTrade == null)
            {
                
                if (File.Exists(AccountsTradeDBname))
                {
                    AccountsTrade = Helper.ReadXml<List<AccountTrade>>(AccountsTradeDBname);
                }
                else
                {
                    AccountsTrade = new();
                }
            }
            return AccountsTrade;
        }

        internal override void SaveAccountsTrade()
        {
            if ((AccountsTrade?.Count ?? 0) > 0)
                Helper.SaveXml(AccountsTrade, AccountsTradeDBname);
        }


        internal override List<AccountCapex> GetAccountsCapex()
        {
            if (AccountsCapex == null)
            {

                if (File.Exists(AccountsCapexDBname))
                {
                    AccountsCapex = Helper.ReadXml<List<AccountCapex>>(AccountsCapexDBname);
                }
                else
                {
                    AccountsCapex = new();
                }
            }
            return AccountsCapex;
        }

        internal override void SaveAccountsCapex()
        {
            if ((AccountsCapex?.Count ?? 0) > 0)
                Helper.SaveXml(AccountsCapex, AccountsCapexDBname);
        }

        internal override ConcurrentBag<Transaction> GetTransaction()
        {
            if (TransactionAccounts == null)
            {
                if (File.Exists(transactionAccountDBName))
                {
                    TransactionAccounts = Helper.ReadXml<ConcurrentBag<Transaction>>(transactionAccountDBName);
                }
                else
                {
                    TransactionAccounts = new();
                }
                    
            }
            return TransactionAccounts;
        }


        internal override void SaveTransaction()
        {
            if ((TransactionAccounts?.Count ?? 0) > 0)
                Helper.SaveXml(TransactionAccounts, transactionAccountDBName);
        }

        internal override (ConcurrentDictionary<BoardEnum, CommissionBase> CommissionBroker, ConcurrentDictionary<string, CommissionBase> CommissionPayOut, ConcurrentDictionary<BoardEnum, CommissionBase> CommissionProp) GetComission()
        {
            if (CommissionBroker == null)
            {
                if (File.Exists(commissionBrokerDBName))
                {
                    CommissionBroker = Helper.ReadXml<ConcurrentDictionary<BoardEnum, CommissionBase>>(commissionBrokerDBName);
                }
                else
                {
                    CommissionBroker = new();
                }
            }
            if (CommissionPayOut == null)
            {
                if (File.Exists(commissionPayOutDBName))
                {
                    CommissionPayOut = Helper.ReadXml<ConcurrentDictionary<string, CommissionBase>>(commissionPayOutDBName);
                }
                else
                {
                    CommissionPayOut = new();
                }
            }
            if (CommissionProp == null)
            {
                if (File.Exists(commissionPropDBName))
                {
                    CommissionProp = Helper.ReadXml<ConcurrentDictionary<BoardEnum, CommissionBase>>(commissionPropDBName);
                }
                else
                {
                    CommissionProp = new();
                }
            }
            return (CommissionBroker, CommissionPayOut, CommissionProp);
        }

        internal override void SaveComission()
        {
            if ((CommissionBroker?.Count ?? 0) > 0)
                Helper.SaveXml(CommissionBroker, commissionBrokerDBName);

            if ((CommissionPayOut?.Count ?? 0) > 0)
                Helper.SaveXml(CommissionPayOut, commissionPayOutDBName);

            if ((CommissionProp?.Count ?? 0) > 0)
                Helper.SaveXml(CommissionProp, commissionPropDBName);
        }

        internal override ConcurrentDictionary<DateOnly, List<ClearingResult>> GetClearing()
        {
            if (Clearing == null)
            {
                if (File.Exists(clearingDBName))
                {
                    Clearing= Helper.ReadXml<ConcurrentDictionary<DateOnly, List<ClearingResult>>>(clearingDBName);
                }
                else
                {
                    Clearing = new();
                }

            }
            return Clearing;
        }

        internal override void SaveClearing()
        {
            Helper.SaveXml(Clearing, clearingDBName);
        }
    }
}
