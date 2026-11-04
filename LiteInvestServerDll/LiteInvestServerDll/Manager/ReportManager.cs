using LiteInvest.Entity.Helpers;
using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ReportEntity;
using LiteInvest.Entity.ServerEntity;

using ClosedXML.Report;
using ClosedXML.Excel;
using System.Linq.Dynamic.Core;

namespace LiteInvestServerDll
{
    public class ReportManager
    {
        private ServerService serverService { get; }

        public ReportManager(ServerService serverService)
        {
            this.serverService = serverService;

        }

        /// <summary>
        /// Запуск ClearingClosePosition  обработка сделок закрытых позиций
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="userLogin"></param>
        /// <param name="accCapex"></param>
        /// <param name="accTrade"></param>
        public void CreateReportTradeDay(Pos pos, string userLogin, AccountCapex accCapex, AccountTrade accTrade)
        {

            foreach (var tradesPair in pos.OpenTrades.Trades)
            {
                var trade = tradesPair.Value;
                TransferOneTradeToReport(pos, accTrade, trade);
            }
            foreach (var tradesPair in pos.CloseTrades.Trades)
            {
                var trade = tradesPair.Value;
                TransferOneTradeToReport(pos, accTrade, trade);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="accTrade"></param>
        /// <param name="trade"></param>
        private void TransferOneTradeToReport(Pos pos, AccountTrade accTrade, MyTrade trade)
        {
            if (trade.Time.Date == Helper.DateTimeMSK.Date)
            {
                ReportTradeDay reportTradeDay = new ReportTradeDay();

                reportTradeDay.AccountTrade = accTrade.Guid;
                reportTradeDay.Board = (BoardEnum)accTrade.Board;
                reportTradeDay.MargaPoint = trade.MarginPoint;
                reportTradeDay.MargaMoney = trade.MarginPoint * trade.Volume * pos.PriceStepCost;
                reportTradeDay.DateTrade = trade.Time;
                reportTradeDay.Volume = trade.Volume;
                reportTradeDay.CommissionSumma = trade.Commission;
                reportTradeDay.Direction = trade.Side;
                reportTradeDay.MyTrade = trade.guid;
                reportTradeDay.Price = trade.Price;
                reportTradeDay.SecIsin = pos.SecName;
                reportTradeDay.Turnover = trade.Price * trade.Volume / pos.PriceStep * pos.PriceStepCost;
                reportTradeDay.DateReport = Helper.DateTimeMSK;

                DBManager.TryAddObjectToDB(reportTradeDay);
            }

            

        }

        public void TransferTradeToReportFinRezInstrument(Pos pos, AccountTrade accTrade)
        {
            foreach (var tradesPair in pos.OpenTrades.Trades)
            {
                var trade = tradesPair.Value;
                TransferOneTradeToFinResInstrument(pos, accTrade, trade);
            }
            foreach (var tradesPair in pos.CloseTrades.Trades)
            {
                var trade = tradesPair.Value;
                TransferOneTradeToFinResInstrument(pos, accTrade, trade);
            }

            
        }

        private  void TransferOneTradeToFinResInstrument(Pos pos, AccountTrade accTrade, MyTrade trade)
        {
            ReportFinRezInstrument reportFinRezInstrument = new ReportFinRezInstrument();
            reportFinRezInstrument.AccountTrade = accTrade.Guid;
            reportFinRezInstrument.Board = (BoardEnum)accTrade.Board;
            reportFinRezInstrument.CommissionSumma = trade.Commission;
            reportFinRezInstrument.DateReport = Helper.DateTimeMSK;
            reportFinRezInstrument.FinResSumma = 0;
            reportFinRezInstrument.Marga = trade.MarginPoint;
            reportFinRezInstrument.SecId = pos.SecName;

            DBManager.TryAddObjectToDB(reportFinRezInstrument);
        }

        public void CreateReportTransferClearingPosition(Pos pos, string userLogin, AccountCapex accCapex, AccountTrade accTrade)
        {
            ReportPositionTransfer reportPositionTransfer = new ReportPositionTransfer();
            reportPositionTransfer.Board = (BoardEnum)accTrade.Board;
            reportPositionTransfer.AccountTradeGuid = accTrade.Guid;
            reportPositionTransfer.Volume = pos.CurrentPos;
            reportPositionTransfer.Direction = pos.Side;
            reportPositionTransfer.SecIsin = pos.SecName;
            reportPositionTransfer.DateReport = Helper.DateTimeMSK;

            DBManager.TryAddObjectToDB(reportPositionTransfer);
        }

        internal void CreateReportBoard(BoardEnum board)
        {
            CreateReportFinRezBoard(board);
            Helper.CreateTimerAndStart(CreateExcelReport, 1000, false);
        }

        private void CreateReportFinRezBoard(BoardEnum board)
        {
            AccountManager accountManager = serverService.accountManager;
            List<AccountTrade>? accountTrade = accountManager.GetAllAccountsTrade();

            for (int i = 0; i < (accountTrade?.Count ?? 0); i++)
            {
                AccountTrade? accTrade = accountTrade![i];
                AccountCapex? accCapex = accountManager.GetAllAccountsCapex()?.FirstOrDefault(a => a.AccountNumber == accTrade.AccountCapex);
                if (accTrade == null || accCapex == null)
                {
                    continue;
                }
                if (accTrade.Board != board)
                {
                    continue;
                }

                DBManager.GetDBContext(db =>
                    db.ReportFinRezBoard.RemoveRange(db.ReportFinRezBoard.Where(r =>
                                                        r.AccountTrade == accTrade.Guid
                                                        && r.Board == board
                                                        && r.DateReport.Date == Helper.DateTimeMSK.Date)));

                ReportFinRezBoard? reportFinRezBoard = new ReportFinRezBoard();
                reportFinRezBoard.AccountTrade = accTrade.Guid;
                reportFinRezBoard.Board = (BoardEnum)accTrade.Board;
                reportFinRezBoard.CommissionSumma = 0;
                reportFinRezBoard.DateReport = Helper.DateTimeMSK;
                reportFinRezBoard.LimitNextSession = (accCapex.DepositCompany + accCapex.DepositTrader) * accTrade.Leverage;
                reportFinRezBoard.DepositSumma = (accCapex.DepositCompany + accCapex.DepositTrader);
                reportFinRezBoard.FinResSumma = 0;
                reportFinRezBoard.Marga = 0;
                reportFinRezBoard.MaxDayLoss = Math.Min(accTrade.MaxDayLossAbsolut, accTrade.MaxDayLossPercent * reportFinRezBoard.LimitNextSession / 100);
                reportFinRezBoard.TransactionCount = 0;

                List<ReportFinRezInstrument>? reportFinRezInstrumentList = new();

                DBManager.GetDBContext(db =>
                {
                    reportFinRezInstrumentList = db.ReportFinRezInstrument.
                                                 Where(r =>
                                                       r.AccountTrade == accTrade.Guid
                                                    && r.Board == board
                                                    && r.DateReport.Date == reportFinRezBoard.DateReport.Date).ToList();
                });

                decimal commissionSumma = reportFinRezInstrumentList.Sum(r => r.CommissionSumma);
                reportFinRezBoard.CommissionSumma = commissionSumma;

                decimal finResSumma = reportFinRezInstrumentList.Sum(r => r.FinResSumma);
                reportFinRezBoard.FinResSumma = finResSumma;

                //decimal marga = reportFinRezInstrumentList.Sum(r => r.Marga);
                reportFinRezBoard.Marga = reportFinRezBoard.CommissionSumma + reportFinRezBoard.FinResSumma;

                List<ReportTradeDay>? reportOrderDay = new();
                DBManager.GetDBContext(db =>
                {
                    reportOrderDay = db.ReportOrderDay.
                                                  Where(r =>
                                                        r.AccountTrade == reportFinRezBoard.AccountTrade
                                                     && r.Board == reportFinRezBoard.Board
                                                     && r.DateTrade.Date == reportFinRezBoard.DateReport.Date).ToList();
                });

                int transactionCount = reportOrderDay.Count;
                reportFinRezBoard.TransactionCount = transactionCount;
                DBManager.TryAddObjectToDB(reportFinRezBoard);

               

                

            }
        }


        private void CreateExcelReport()   
        {
            AccountManager accountManager = serverService.accountManager;
            List<AccountTrade>? accountTrade = accountManager.GetAllAccountsTrade();
            for (int i = 0; i < (accountTrade?.Count ?? 0); i++)
            {
                AccountTrade? accTrade = accountTrade![i];
                AccountCapex? accCapex = accountManager.GetAllAccountsCapex()?.FirstOrDefault(a => a.AccountNumber == accTrade.AccountCapex);
                if (accTrade == null || accCapex == null)
                {
                    continue;
                }
                ExcelReport(accTrade, accCapex);
                
            }
        }

        public void ExcelReport(AccountTrade accTrade, AccountCapex accCapex)
        {
            DateTime dateReport = Helper.DateTimeMSK;
            var pathBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            XLTemplate template = new XLTemplate(Path.Combine(pathBaseDirectory, @"ReportTemplate\TradeReportTemplateEveryDay.xlsx"));

            var reportFinRezBoardList = DBManager.GetObjectFromDB<ReportFinRezBoard>(query =>
                        query.Where(r => r.AccountTrade == accTrade.Guid && r.DateReport.Date == dateReport.Date));

            if ((reportFinRezBoardList?.Count ?? 0) > 0)
            {
                template.AddVariable("AccountNumber", accTrade.AccountNumber);
                template.AddVariable("DateReportString", dateReport.ToShortDateString());
                template.AddVariable("ReportFinRezBoard", reportFinRezBoardList);
                template.AddVariable("RezSumMarga", reportFinRezBoardList[0].Marga);
                template.AddVariable("RezSumCommission", reportFinRezBoardList[0].CommissionSumma);
                template.AddVariable("RezSumFinres", reportFinRezBoardList[0].FinResSumma);
                template.AddVariable("TransactionSummaEndReport", reportFinRezBoardList[0].DepositSumma);
            }

            var reportFinRezBoardListFirstDayMount = DBManager.GetObjectFromDB<ReportFinRezBoard>(query =>
                        query.Where(r => r.AccountTrade == accTrade.Guid && r.DateReport.Date <= dateReport.Date.AddDays(-dateReport.Day+1))
                             .OrderBy(rr=> rr.DateReport.Date))
                             .FirstOrDefault();

            if (reportFinRezBoardListFirstDayMount == null)
            {
                template.AddVariable("DateStartReport", dateReport.Date.AddDays(-dateReport.Day+1).ToShortDateString());
                template.AddVariable("TransactionSummaStartReport", 0);
            }
            else
            {
                template.AddVariable("DateStartReport", reportFinRezBoardListFirstDayMount.DateReport.ToShortDateString());
                template.AddVariable("TransactionSummaStartReport", reportFinRezBoardListFirstDayMount.DepositSumma);
            }



                List<ReportFinRezInstrument>? reportFinRezInstrument = DBManager.GetObjectFromDB<ReportFinRezInstrument>(query =>
                            query.Where(r => r.DateReport.Date == dateReport.Date && r.AccountTrade == accTrade.Guid)
                                 .GroupBy(x => new 
                                 {
                                     x.Board,
                                     x.AccountTrade,
                                     x.SecId,
                                     x.DateReport.Date
                                 })
                                 .Select(g => new ReportFinRezInstrument
                                 {
                                     Board = g.Key.Board,
                                     AccountTrade = g.Key.AccountTrade,
                                     SecId = g.Key.SecId,
                                     DateReport = g.Key.Date,
                                     Marga = g.Sum(x => x.Marga),
                                     CommissionSumma = g.Sum(x => x.CommissionSumma),
                                     FinResSumma = g.Sum(x => x.FinResSumma)
                                 })
                                 );

            //if ((reportFinRezInstrument?.Count??0)>0)
            {
                template.AddVariable("ReportFinRezInstrument", reportFinRezInstrument);
            }


            List<ReportCapexHistoryTransaction>? reportCapexHistoryTransactions = new();
            DBManager.GetDBContext(db =>
            {
                reportCapexHistoryTransactions = db.ReportCapexHistoryTransaction.Where(r =>
                                                    r.AccountCapex == accCapex.Guid
                                                 && r.TransactionDate.Date > dateReport.Date.AddDays(-dateReport.Day)) // в начало месяца
                                                .GroupBy(x => new
                                                {
                                                    x.TransactionDate.Date,
                                                    x.TransactionDescription,
                                                    x.AccountCapex,
                                                    x.ExchangeRate,
                                                    x.AccountTrade,
                                                    x.Board
                                                })
                                                .Select(g => new ReportCapexHistoryTransaction
                                                {
                                                    AccountCapex = g.Key.AccountCapex,
                                                    Board = g.Key.Board,
                                                    AccountTrade = g.Key.AccountTrade,
                                                    ExchangeRate = g.Key.ExchangeRate,
                                                    TransactionDate = g.Key.Date,
                                                    TransactionDescription = g.Key.TransactionDescription,
                                                    TransactionSumma = g.Sum(x => x.TransactionSumma)
                                                })
                                                .OrderBy(r => r.TransactionDate)
                                                .ToList();
            });
            


            //if ((reportCapexHistoryTransactions?.Count ?? 0) > 0)
            {
                template.AddVariable("ReportCapexHistoryTransaction", reportCapexHistoryTransactions);
            }

            List<ReportTradeDay>? reportTradeDay = new();
            DBManager.GetDBContext(db =>
            {
                //ToDo  reportTradeDay из базы вытянуть и в отчет положить

                reportTradeDay = db.ReportOrderDay.Where( r =>
                                    r.AccountTrade == accTrade.Guid
                                    && r.DateTrade.Date == dateReport.Date)
                .OrderBy(r => r.DateTrade)                    
                .ToList();

            });
            template.AddVariable("ReportOrderDay", reportTradeDay);

            List<ReportPositionTransfer>? reportPositionTransfer = new();
            DBManager.GetDBContext(db =>
            {
                reportPositionTransfer = db.ReportPositionTransfer.Where(r =>
                                                r.AccountTradeGuid == accTrade.Guid
                                                && r.DateReport.Date == dateReport.Date)
                .OrderBy(r => r.SecIsin)
                .ToList();
            });

            template.AddVariable("ReportPositionTransfer", reportPositionTransfer);
            template.AddVariable("InstrumentTransferCount", reportPositionTransfer.Count);

            template.Generate();

            string filename = Path.Combine(@"C:\ServerData", "Reports", accTrade.AccountNumber, $"Отчет {accCapex.UserName} счет {accTrade.AccountNumber} за {dateReport.ToShortDateString()}.xlsx");
            template.SaveAs(filename);
            template.Dispose();

            ReportSender.SendFileToTelegramAsync(filename);


        }
    }
}
