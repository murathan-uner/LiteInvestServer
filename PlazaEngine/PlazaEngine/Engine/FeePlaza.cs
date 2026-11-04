using LiteInvest.Entity.PlazaEntity;

using PlazaEngine.Engine;

using ru.micexrts.cgate;
using ru.micexrts.cgate.message;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PlazaEngine.Engine
{
    public class FeePlaza : IDisposable
    {

        private PlazaConnector plazaConnector;
        private Listener? listener;
        private Listener? listenerBroker;

        private Thread threadNotification;
        private CancellationTokenSource cancelTokenSource;
        private CancellationToken token;

        private bool _loadFeeSnapshot = false;


        /// <summary>
		/// thread state of tick listener
        /// состояние потока листнера тиков
        /// </summary>
        private string _listenerFeeBoardReplState;

        /// <summary>
		/// thread state of tick listener
        /// состояние потока листнера тиков
        /// </summary>
        private string _listenerFeeBrokerReplState;

        /// <summary>
        /// flag saying that the connection with the list was interrupted/флаг, говорящий о том что коннект с листнером прервался 
        /// and it requires a full reboot/и требуется его полная перезагрузка
        /// </summary>
        private bool _listenerFeeBoardNeadToReconnect;


        private bool _listenerFeeBrokerNeadToReconnect;
        /// <summary>
        /// listener
        /// листнер 
        /// </summary>
        private Listener _listenerFeeBoard;

        /// <summary>
        /// listener
        /// листнер 
        /// </summary>
        private Listener _listenerFeeBroker;

        /// <summary>
        /// Пришел новый тик - обезличенная сделка
        /// key = SecurityID, value = List Trade
        /// </summary>
        internal event Action<Dictionary<long, List<FeeBoard>>>? NewFeeBoardEvent;


        /// <summary>
        /// строка инициализации листнера отвечающего за приём fee
        /// </summary>
        private const string ListenFeeBoardString = "p2repl://FORTS_FEE_REPL;scheme=|FILE|SchemasPlaza/Schemas/fee.ini|CustReplScheme";


        /// <summary>
        /// строка инициализации листнера отвечающего за приём fee
        /// </summary>
        private const string ListenFeeBrokerString = "p2repl://FORTS_BROKER_FEE_REPL;scheme=|FILE|SchemasPlaza/Schemas/brokerfee.ini|CustReplScheme";


        /// <summary>
        /// Класс отвечающий за подписку на тики - обезличенные сделки
        /// </summary>
        /// <param name="plazaConnector"></param>

        public FeePlaza(PlazaConnector plazaConnector)
        {
            this.plazaConnector = plazaConnector;


        }

        public void Dispose()
        {
            try { listener?.Close(); } catch { }
            try { listener?.Dispose(); } catch { }
            cancelTokenSource?.Cancel();


        }

        /// <summary>
        /// Проверить и запустить прослушивание потоков
        /// </summary>
        internal void CheckListnerFee()
        {
            CheckListnerFeeBoard();
            CheckListnerFeeBroker();

        }



        internal void CheckListnerFeeBoard()
        {
            try
            {
                if (_listenerFeeBoard == null)
                {
                    _listenerFeeBoard = new Listener(plazaConnector.ConnectionObjectPlaza, ListenFeeBoardString);
                    SetListener(_listenerFeeBoard);

                }
                if (_listenerFeeBoardNeadToReconnect || _listenerFeeBoard.State == State.Error)
                {
                    _listenerFeeBoardNeadToReconnect = false;
                    try
                    {
                        _listenerFeeBoard.Close();
                        _listenerFeeBoard.Dispose();
                    }
                    catch (Exception error)
                    {
                        plazaConnector.SendLogMessage(error);
                    }

                    _listenerFeeBoard = null;
                    return;
                }
                if (_listenerFeeBoard.State == State.Closed)
                {
                    if (_listenerFeeBoardReplState == null)
                    {
                        if (_loadFeeSnapshot)
                        {
                            _listenerFeeBoard.Open("mode=snapshot+online");
                        }
                        else
                        {
                            _listenerFeeBoard.Open("mode=online");
                        }

                        RouterLoggerEngine.Log("Загрузка Fee Board пошла", "Connect");
                    }
                    else
                    {
                        if (_loadFeeSnapshot)
                        {
                            _listenerFeeBoard.Open("mode=snapshot+online;" + "replstate=" + _listenerFeeBoardReplState);
                        }
                        else
                        {
                            _listenerFeeBoard.Open("mode=online;" + "replstate=" + _listenerFeeBoardReplState);
                        }
                        
                    }
                }
            }

            catch (Exception error)
            {
                try
                {
                    _listenerFeeBoard.Dispose();
                }
                catch
                {
                    // ignore
                }

                _listenerFeeBoard = null;

                plazaConnector.SendLogMessage(error);
            }
        }


        internal void CheckListnerFeeBroker()
        {
            try
            {
                if (_listenerFeeBroker == null)
                {
                    _listenerFeeBroker = new Listener(plazaConnector.ConnectionObjectPlaza, ListenFeeBrokerString);
                    SetListenerBroker(_listenerFeeBroker);

                }
                if (_listenerFeeBrokerNeadToReconnect || _listenerFeeBroker.State == State.Error)
                {
                    _listenerFeeBrokerNeadToReconnect = false;
                    try
                    {
                        _listenerFeeBroker.Close();
                        _listenerFeeBroker.Dispose();
                    }
                    catch (Exception error)
                    {
                        plazaConnector.SendLogMessage(error);
                    }

                    _listenerFeeBroker = null;
                    return;
                }
                if (_listenerFeeBroker.State == State.Closed)
                {
                    if (_listenerFeeBoardReplState == null)
                    {
                        if (_loadFeeSnapshot)
                        {
                            _listenerFeeBroker.Open("mode=snapshot+online");
                        }
                        else
                        {
                            _listenerFeeBroker.Open("mode=online");
                        }

                        RouterLoggerEngine.Log("Загрузка Fee Board пошла", "Connect");
                    }
                    else
                    {
                        if (_loadFeeSnapshot)
                        {
                            _listenerFeeBroker.Open("mode=snapshot+online;" + "replstate=" + _listenerFeeBrokerReplState);
                        }
                        else
                        {
                            _listenerFeeBroker.Open("mode=online;" + "replstate=" + _listenerFeeBrokerReplState);
                        }

                    }
                }
            }

            catch (Exception error)
            {
                try
                {
                    _listenerFeeBroker.Dispose();
                }
                catch
                {
                    // ignore
                }

                _listenerFeeBroker = null;
                

                plazaConnector.SendLogMessage(error);
            }
        }

        public void SetListenerBroker(Listener _listener)
        {
            if (listenerBroker != null)
            {
                try
                {
                    listenerBroker.Handler = null;
                    listenerBroker.Close();
                    listenerBroker = null;
                }
                catch
                {
                    listenerBroker = null;
                }
            }

            listenerBroker = _listener;
            listenerBroker.Handler += new Listener.MessageHandler(FeeBoardMessageHandler);

        }


        public void SetListener(Listener _listener)
        {
            if (listener != null)
            {
                try
                {
                    listener.Handler = null;
                    listener.Close();
                    listener = null;
                }
                catch
                {
                    listener = null;
                }
            }

            listener = _listener;
            listener.Handler += new Listener.MessageHandler(FeeBoardMessageHandler);

            cancelTokenSource?.Cancel();
            Thread.Sleep(1000);
            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;


            threadNotification = new Thread(ThreadNotificationFeeBoard);
            threadNotification.Name = "ThreadNotificationTicks";
            threadNotification.IsBackground = true;
            threadNotification.Start();
        }

        private int FeeBoardMessageHandler(Connection conn, Listener listener, Message msg)
        {
            try
            {
                var scheme = listener.Scheme;
                string schemeFee = "feeBoard";
                if (scheme?.Messages.FirstOrDefault(m => m.Name == "broker_fee") != null )
                {
                    schemeFee = "feeBroker";
                }
                
                RouterLoggerEngine.Log($"{DateTime.Now.ToLongTimeString()} All Message {schemeFee} msg.Type={msg?.Type} msg.Size={msg?.Size}, msg.Data={msg?.Data?.Length}", "feeBoard");

                switch (msg.Type)
                {
                    case MessageType.MsgStreamData:
                        {
                            StreamDataMessage replmsg = (StreamDataMessage)msg;

                            Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} START FeeBoardMessageHandler MsgName == {schemeFee} ");
                            RouterLoggerEngine.Log($"{DateTime.Now.ToLongTimeString()} FeeBoardMessage {schemeFee}\n {replmsg}", "feeBoard");

                            break;
                        }

                    case MessageType.MsgOpen:
                        break;
                    case MessageType.MsgTime:
                        break;
                    case MessageType.MsgClose:
                        break;
                    case MessageType.MsgData:
                        {
                            DataMessage replmsg = (DataMessage)msg;

                            RouterLoggerEngine.Log($"{DateTime.Now.ToLongTimeString()} FeeBoardMessage {schemeFee}\n MsgData {replmsg}", "feeBoard");

                            break;
                        }
                    
                    case MessageType.MsgTnBegin:
                        break;
                    case MessageType.MsgTnCommit:
                        break;
                    case MessageType.MsgP2MQTimeout:
                        break;
                    case MessageType.MsgP2ReplLifeNum:
                        break;
                    case MessageType.MsgP2ReplClearDeleted:
                        break;
                    case MessageType.MsgP2ReplOnline:
                        {
                            RouterLoggerEngine.Log($"{DateTime.Now.ToLongTimeString()} FeeBoardMessage {schemeFee} OnLine", "feeBoard");
                            break;
                        }
                    case MessageType.MsgP2ReplReplState:
                        {
                            if (schemeFee == "feeBoard")
                            {
                                _listenerFeeBoardReplState = ((P2ReplStateMessage)msg).ReplState;
                                _listenerFeeBoardNeadToReconnect = true;
                            }
                            else
                            {
                                _listenerFeeBrokerReplState = ((P2ReplStateMessage)msg).ReplState;
                                _listenerFeeBrokerNeadToReconnect = true;
                            }
                                break;
                        }
                    default:
                        break;
                }

            }
            catch (CGateException e)
            {
                plazaConnector.SendLogMessage(e);
                return (int)e.ErrCode;
            }

            return 0;
        }


        private void ThreadNotificationFeeBoard()
        {
            while (!token.IsCancellationRequested)
            {
                //Thread.Sleep(periodMilliSecond);
                Thread.Sleep(1);
                try
                {

                }
                catch
                {

                }
            }

        }

    }
}