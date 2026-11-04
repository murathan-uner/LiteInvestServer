using LiteInvest.Entity.ServerEntity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LiteInvest.Entity.Server2;
using LiteInvest.Entity.PlazaEntity;

namespace LiteInvestServerDll
{
    public class PositionManager
    {
        private static ServerService serverService;

        public PositionManager(ServerService _serverService)
        {
            serverService = _serverService;
            serverService.MarginClosePosition += ServerService_MarginClosePosition;
        }

        /// <summary>
        /// Принудительное закрытие позиций
        /// </summary>
        /// <param name="user"></param>
        private void ServerService_MarginClosePosition(User user)
        {
            try
            {
                serverService?.CloseAllPosition(user.Login);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Margin Commission – total commission and margin profit for open and closed trades in the current trading session
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        public static (decimal Margin, decimal Commission) GetSummaMarginAndCommission(string userName, BoardEnum board)
        {
            decimal margin = 0;
            decimal commission = 0;
            List<Pos>? positions = serverService.GetAllPositionsForUser(userName).Where(p=> p.Cleared == false && p.Board == board).ToList();
                                                                // all open and closed positions that have not been cleared
            for (int i = 0; i < positions.Count; i++)
            {
                Pos pos = positions[i];
                margin = margin + pos.RealizedPnl + pos.UnRealizedPnl;
                commission = commission + pos.Commission;
            }


            return (margin, commission);
        }


        /// <summary>
        /// Расчет нереализованной прибыли
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="tickprice"></param>
        public static void CalculateUnrealizedPnl(Pos pos, decimal tickprice)
        {

            //как бы если поза не открыта, мы уже ниче не делаем.
            if (pos.CurrentPos == 0 || !pos.Open)
            {
                pos.UnRealizedPnlPoints = 0;
                pos.UnRealizedPnl = 0;
                return;
            }

            var posopened = pos.OpenTrades.Volume;
            var posclosed = pos.CloseTrades.Volume;

            SecurityApi? sec = serverService.GetSecurity(pos.SecId);

            pos.UnRealizedPnlPoints = (tickprice - pos.OpenTrades.AveragePrice) * pos.multDirection;
            pos.UnRealizedPnl = pos.UnRealizedPnlPoints * (posopened - posclosed) / (sec?.PriceStep ?? 1) * (sec?.PriceStepCost ?? 1);// - pos.Comission;

        }

        /// <summary>
        /// Расчет реализованной прибыли
        /// </summary>
        /// <param name="pos"></param>
        public static void CalculateRealizedPnl(Pos pos )
        {
            SecurityApi? sec = serverService.GetSecurity(pos.SecId);
            
            ReCalculateCommission(pos);

            var posclosed = pos.CloseTrades.Volume;
            pos.RealizedPnlPoints = (pos.CloseTrades.AveragePrice - pos.OpenTrades.AveragePrice) * pos.multDirection;
            pos.RealizedPnl = pos.RealizedPnlPoints * posclosed / (sec?.PriceStep ?? 1) * (sec?.PriceStepCost ?? 1);// - pos.Comission;
        }

        /// <summary>
        /// Расчет комисии по позиции
        /// </summary>
        /// <param name="pos"></param>
        public static void ReCalculateCommission(Pos pos)
        {
            decimal comission = 0;
            foreach (var tradePair in pos.OpenTrades.Trades)
            {
                var trade = tradePair.Value;
                comission = comission + GetPositionCommission(trade);   
            }
            foreach (var tradePair in pos.CloseTrades.Trades)
            {
                var trade = tradePair.Value;
                comission = comission + GetPositionCommission(trade);   
            }
            pos.Commission = comission;
        }

        private static decimal GetPositionCommission(MyTrade trade)
        {
            decimal comission = 0;
            comission = comission + trade.FeeBoard;     // комиссия биржи

            var commisBrok = CommissionManager.GetCommissionBroker(trade.Board);
            if (commisBrok.ValueType == ValueTypeEnum.Percent)
            {
                comission = comission + trade.Volume * commisBrok.Value / 100;       // комиссия брокера
            }
            else if (commisBrok.ValueType == ValueTypeEnum.Absolut)
            {
                comission = comission + commisBrok.Value;       // комиссия брокера
            }

            var commisProp = CommissionManager.GetCommissionProp(trade.Board);
            if (commisProp.ValueType == ValueTypeEnum.Percent)
            {
                comission = comission + trade.Volume * commisProp.Value / 100;       // комиссия пропа
            }
            else if (commisProp.ValueType == ValueTypeEnum.Absolut)
            {
                comission = comission + commisProp.Value;       // комиссия пропа
            }

            trade.Commission = comission;

            return comission;
        }

    }
}
