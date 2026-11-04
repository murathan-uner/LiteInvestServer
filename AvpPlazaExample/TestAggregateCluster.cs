using Aggregate;

using ConnectorService;
using LiteInvest.Entity.PlazaEntity;

using PlazaEngine.Engine;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace AvpPlazaTester
{
    public class TestAggregateCluster :INotifyPropertyChanged
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
            get => PlazaOptions.ScaleList;
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

        
        AggregateClusterWindow aggregateClusterWindow;

        AggregateCluster aggregateCluster;

        Action<string> logMessage;

        public TestAggregateCluster(PlazaConnector connector, Action<string> logMessage)
        {
            this.logMessage = logMessage;
            Securities = new BindingList<Security>();

            plaza = connector;
            plaza.Emulation = true;
           
            plaza.SecuritiesLoadedEvent += Plaza_SecuritiesLoadedEvent;

            aggregateClusterWindow  = new AggregateClusterWindow(this);
            aggregateClusterWindow.DataContext = this;
            aggregateClusterWindow.Show();
            
            Plaza_SecuritiesLoadedEvent();

            aggregateCluster = AggregateCluster.Build(plaza, ScaleList);
            aggregateCluster.NewInsideClusterEvent += AggregateCluster_NewInsideClusterEvent;

            Thread threadUpdateGlass = new Thread(ThreadUpdateCluster);
            threadUpdateGlass.IsBackground = true;
            threadUpdateGlass.Name = "ThreadUpdateGlass";
            threadUpdateGlass.Start();
        }

        private void ThreadUpdateCluster()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(200);
                    if (queuePriceLevels.IsEmpty || !queuePriceLevels.TryDequeue(out List<MarketDepthLevel> md))
                    {
                        continue;
                    }
                    while (!queuePriceLevels.IsEmpty)    // если  валятся быстрее, чем мы их можем нарисовать, то берем последний
                    {
                        DateTime timeTryDequeue = DateTime.Now;
                        queuePriceLevels.TryDequeue(out md);
                        if (timeTryDequeue.AddMilliseconds(300) < DateTime.Now)
                        {
                            Debug.WriteLine($"Тики по коду инструмента {SelectedSecurity?.Id} валятся быстрее, чем мы их можем прочитать.");
                        }
                    }
                    UpdateCluster(md);
                    DateTime update = DateTime.Now;
                    
                }
                catch (Exception ex)
                {
                    throw;
                }

            }
        }

        private void UpdateCluster(List<MarketDepthLevel>? md)
        {
            try
            {
                aggregateClusterWindow.Dispatcher?.Invoke(new Action(() =>
                {
                    try
                    {
                        aggregateClusterWindow.GlassView.ItemsSource = md;
                        ICollectionView view = CollectionViewSource.GetDefaultView(aggregateClusterWindow.GlassView.ItemsSource);
                        view.Refresh();
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

        ConcurrentQueue<List<MarketDepthLevel>> queuePriceLevels = new ConcurrentQueue<List<MarketDepthLevel>>();

        private void AggregateCluster_NewInsideClusterEvent(InsideClusters insideClusters)
        {
            try
            {
                if (SelectedSecurity?.Id != insideClusters.SecurityId || selectedScale == 0)
                {
                    return; // стакан не потому инструменту который надо
                }

                var last = insideClusters.ScaledCluster[selectedScale].Keys.Max();

                List<MarketDepthLevel> mdLevels = insideClusters.ScaledCluster[selectedScale][last];
                queuePriceLevels.Enqueue(mdLevels);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChange([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void Plaza_SecuritiesLoadedEvent()
        {

            var securitis = plaza.Securities.Values.ToList();
            securitis.Sort((x, y) => x.Name.CompareTo(y.Name));
            aggregateClusterWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                Securities.Clear();
                foreach (var sec in securitis)
                {
                    Securities.Add(sec);
                }
            }));
        }

        internal void CommandSubscripAggregateCluster()
        {
            if (SelectedSecurity == null)
            {
                return;
            }
            aggregateCluster.SubscribAllScaledCluster(selectedSecurity.Id, plaza.Emulation);
        }
    }
}
