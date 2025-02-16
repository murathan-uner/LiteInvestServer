using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

using LiteInvest.Entity.PlazaEntity;

using PlazaEngine.Engine;
using ConnectorService;

using static AvpPlazaExample.MainWindow;

using System.Windows.Media;
using System.Collections.Concurrent;

namespace AvpPlazaTester
{
    public class TestAggregateGlass :INotifyPropertyChanged
    {

        private string labelGlassData;

        public string LabelGlassData
        {
            get { return labelGlassData; }
            set
            {
                if (labelGlassData != value)
                {
                    labelGlassData = value;
                    OnPropertyChange();
                }
            }
        }

        private string labelGlassTimeUpdate;

        public string LabelGlassTimeUpdate
        {
            get 
            {
                return labelGlassTimeUpdate; 
            } 
            set 
            {
                if (labelGlassTimeUpdate != value)
                {
                    labelGlassTimeUpdate = value;
                    OnPropertyChange();
                }
            }
        }

        public List<int> ScaleList
        {
            get => aggregateGlass.ScaleList;
            set
            {
                OnPropertyChange();
            }
        }

        private int selectedScale;

        public int SelectedScale
        {
            get
            {
                return selectedScale;
            }
            set
            {
                if (selectedScale != value)
                {
                    selectedScale = value;
                    OnPropertyChange();

                }
            }
        }

        private Security selectedSecurity;
        public Security SelectedSecurity
        { 
            get
            {
                return selectedSecurity;
            }
            
           set
            {
                if (selectedSecurity != value)
                {
                    selectedSecurity = value;
                    OnPropertyChange();
                }
            }
        
        }
        


        public BindingList<Security> Securities { get; set; }

        PlazaConnector? plaza;

        AggregateGlass aggregateGlass;

        public TestAggregateGlass(PlazaConnector? plaza)
        {
            SelectedScale = 1;
            this.plaza = plaza;
            Securities = new BindingList<Security>();
            
            aggregateGlass = AggregateGlass.Build(plaza, new List<int>() { 1, 5, 10, 20, 50, 100 });
            aggregateGlass.NewInsideQuotesEvent += AggregateGlass_NewInsideQuotesEvent;

            Plaza_SecuritiesLoadedEvent();
            plaza.SecuritiesLoadedEvent += Plaza_SecuritiesLoadedEvent;
            PropertyChanged += TestAggregateGlass_PropertyChanged;

            Thread threadUpdateGlass = new Thread(ThreadUpdateGlass);
            threadUpdateGlass.IsBackground = true;
            threadUpdateGlass.Name = "ThreadUpdateGlass";
            threadUpdateGlass.Start();
        }

        private void TestAggregateGlass_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedScale")
            {
                aggregateGlass.NeedUpDateGlass(SelectedSecurity?.Id);
            }
        }

        ConcurrentQueue<(List<MarketDepthLevel>, int)> queueMarketDepthLevels = new ConcurrentQueue<(List<MarketDepthLevel>, int)>();

        private void AggregateGlass_NewInsideQuotesEvent(InsideQuotes insideQuotes)
        {

            try
            {
                if (SelectedSecurity?.Id != insideQuotes.SecurityId || selectedScale == 0)
                {
                    return; // стакан не потому инструменту который надо
                }
                List<MarketDepthLevel> mdLevels = insideQuotes.ScaledQuotes[selectedScale];
                queueMarketDepthLevels.Enqueue((mdLevels, insideQuotes.CentreQuotes[selectedScale]));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void ThreadUpdateGlass()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(200);
                    if (queueMarketDepthLevels.IsEmpty || !queueMarketDepthLevels.TryDequeue(out (List<MarketDepthLevel>, int) md))
                    {
                        continue;
                    }
                    while (!queueMarketDepthLevels.IsEmpty)    // если стаканы валятся быстрее, чем мы их можем нарисовать, то берем последний
                    {
                        DateTime timeTryDequeue = DateTime.Now;
                        queueMarketDepthLevels.TryDequeue(out md);
                        if (timeTryDequeue.AddMilliseconds(300) < DateTime.Now)
                        {
                            Debug.WriteLine($"Cтаканы по коду инструмента {SelectedSecurity?.Id} валятся быстрее, чем мы их можем прочитать.");
                        }
                    }
                    UpdateGlass(md.Item1, md.Item2);
                    DateTime update = DateTime.Now;
                    LabelGlassTimeUpdate = $"{SelectedSecurity?.Name} :{update:HH.mm.ss:fff} + {md.Item1.Count}, {md.Item2}";
                    LabelGlassData = $"{md.Item1[md.Item2].Price}={md.Item1[md.Item2].VolumeString}; " +
                        $"{md.Item1[md.Item2-1].Price}={md.Item1[md.Item2-1].VolumeString}; " +
                        $"{md.Item1[md.Item2+1].Price}={md.Item1[md.Item2+1].VolumeString}";

                }
                catch (Exception ex)
                {
                    Debug.WriteLine (ex.Message);
                }
            }
        }


        DateTime listGlassScrollIntoViewTime = DateTime.Now;

        private void UpdateGlass(List<MarketDepthLevel> mdLevels, int centreQuotes)
        {
            try
            {
                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    try
                    {

                        listGlass.ItemsSource = mdLevels.GetRange(centreQuotes - 50, 100);
                        //if (listGlassScrollIntoViewTime.AddSeconds(2) < DateTime.Now && mdLevels.Count > centreQuotes)
                        //{
                        //    listGlass.ScrollIntoView(mdLevels[centreQuotes - 2]);
                        //    // listGlass.ScrollIntoView(mdLevels[centreQuotes + 2]);
                        //    listGlassScrollIntoViewTime = DateTime.Now;
                        //}
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
                ));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void Plaza_SecuritiesLoadedEvent()
        {

            var securitis = plaza.Securities.Values.ToList();
            securitis.Sort((x, y) => x.Name.CompareTo(y.Name));
            if (Dispatcher != null)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    SetBindingListSecuritis(securitis);
                }));
            }
            else
            {
                SetBindingListSecuritis(securitis);
            }
        }

        private void SetBindingListSecuritis(List<Security> securitis)
        {
            Securities.Clear();
            foreach (var sec in securitis)
            {
                Securities.Add(sec);
            }
        }

        public void CommandSubscribeAggregateGlass()
        {
            if (SelectedSecurity == null)
            {
                return;
            }

            aggregateGlass.SubscribAllScaledGlass(selectedSecurity.Id, true);
        }
        



        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChange([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        Dispatcher Dispatcher;
        public void SetDispatcher(Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;
        }

        System.Windows.Controls.ListView listGlass;
        internal void SetDispatcher(AggregateGlassWindow aggregateGlassWindow)
        {
            SetDispatcher(aggregateGlassWindow.Dispatcher);
            listGlass = aggregateGlassWindow.GlassView;
        }
    }
}
