using LiteInvest.Entity.Helpers;
using LiteInvest.Entity.ServerEntity;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvestServerDll
{
    public class CommissionManager: NotifyPropertyBase
    {
        /// <summary>
        /// Комиссия брокера - для всех трейдеров одинаковая, для всех рынков разная, может быть в % от сделки  или абсолютная на сделку
        /// key - тип рынка
        /// </summary>
        ConcurrentDictionary<BoardEnum, CommissionBase> CommissionBroker;

        /// <summary>
        /// Размер пэйаут - для каждого трейдера свой %  для любого рынка. Значение в % от прибыли (20% по умолчанию). 
        /// key - userName
        /// </summary>
        ConcurrentDictionary<string, CommissionBase> CommissionPayOut;

        /// <summary>
        /// Комиссия пропа - для всех трейдеров одинаковая, для разных рынков разная. Может быть абсолютная или % от сделки. 
        /// key - тип рынка
        /// </summary>
        ConcurrentDictionary<BoardEnum, CommissionBase> CommissionProp;

        /// <summary>
        /// Dependence Injection
        /// </summary>
        /// <param name="_serverService"></param>
        /// <param name="_serviceDB"></param>
        public static void Open(ServiceDB _serviceDB)
        {
            serviceDB = _serviceDB;

            var com = serviceDB.GetComission();
            Instance.CommissionBroker = com.CommissionBroker;
            Instance.CommissionPayOut = com.CommissionPayOut;
            Instance.CommissionProp = com.CommissionProp;

            serviceDB.CommissionEventHandler(Instance);
        }

        private static CommissionManager Instance;
        private static ServiceDB serviceDB;

        public CommissionManager()
        {
            Instance = this;
            
        }


        /// <summary>
        /// Получить комиссию Брокера
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public static CommissionBase GetCommissionBroker(BoardEnum board)
        {
            var res =  Instance.CommissionBroker.GetValueOrDefault(board);
            if (res == null)
            {
                if (board == BoardEnum.FortsMoex)
                {
                    res = new CommissionBase() { Value = 0.45m, ValueType = ValueTypeEnum.Absolut };
                }
                else
                {
                    res = new CommissionBase() { Value = 0.01m, ValueType = ValueTypeEnum.Percent};
                }
            }
            return res;
        }

        /// <summary>
        /// Установить комиссию Брокера
        /// </summary>
        /// <param name="board">Биржа</param>
        /// <param name="commission">Параметры комиссии</param>
        public static void SetCommissionBroker(BoardEnum board, CommissionBase commission)
        {
            if (!Instance.CommissionBroker.ContainsKey(board))
            {
                Instance.CommissionBroker[board] = new CommissionBase();
            }
            Instance.CommissionBroker[board].Value = commission.Value;
            Instance.CommissionBroker[board].ValueType = commission.ValueType;
            Instance.OnPropertyChanged("CommissionBroker");
        }


        /// <summary>
        /// Получить значение PauOut
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public static CommissionBase GetCommissionPayOut(string userName)
        {
            CommissionBase? res= Instance.CommissionPayOut.GetValueOrDefault(userName);
            if (res == null)
            {
                res = new CommissionBase() { Value = 20, ValueType = ValueTypeEnum.Percent };
            }
            return res;
        }

        /// <summary>
        /// Установить значение PauOut
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="commission"></param>
        public static void  SetCommissionPayOut(string userName, CommissionBase commission)
        {
            if (!Instance.CommissionPayOut.ContainsKey(userName))
            {
                Instance.CommissionPayOut[userName] = new CommissionBase();
            }
            Instance.CommissionPayOut[userName].Value = commission.Value;
            Instance.CommissionPayOut[userName].ValueType = commission.ValueType;
            Instance.OnPropertyChanged("CommissionPayOut");
        }

        /// <summary>
        /// Получить комиссию Пропа
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public static CommissionBase GetCommissionProp(BoardEnum board)
        {
            var res = Instance.CommissionProp.GetValueOrDefault(board);
            if (res == null)
            {
                if (board == BoardEnum.FortsMoex)
                {
                    res = new CommissionBase() { Value = 0.45m, ValueType = ValueTypeEnum.Absolut };
                }
                else
                {
                    res = new CommissionBase() { Value = 0.01m, ValueType = ValueTypeEnum.Percent };
                }
            }
            return res;
        }

        /// <summary>
        /// Установить комиссию Пропа
        /// </summary>
        /// <param name="board"></param>
        /// <param name="commission"></param>
        public static void SetCommissionProp(BoardEnum board, CommissionBase commission)
        {
            if (!Instance.CommissionProp.ContainsKey(board))
            {
                Instance.CommissionProp[board] = new CommissionBase();
            }
            Instance.CommissionProp[board].Value = commission.Value;
            Instance.CommissionProp[board].ValueType = commission.ValueType;
            Instance.OnPropertyChanged("CommissionProp");
        }

    }


    /// <summary>
    /// Комиссия 
    /// </summary>
    public class CommissionBase
    {
        /// <summary>
        /// Значение комиссии
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Тип комиссии
        /// </summary>
        public ValueTypeEnum ValueType { get; set; }
    }


    public enum ValueTypeEnum
    {
        Percent,
        Absolut
    }
}
