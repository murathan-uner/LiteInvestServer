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

namespace AvpPlazaTester
{
    public class TestAggregateGlass :INotifyPropertyChanged
    {
        
        public List<int> ScaleList
        {
            get => AggregateGlass.ScaleList;
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
            
            aggregateGlass = AggregateGlass.Build(plaza);
            aggregateGlass.NewInsideQuotesEvent += AggregateGlass_NewInsideQuotesEvent;

            Plaza_SecuritiesLoadedEvent();
            plaza.SecuritiesLoadedEvent += Plaza_SecuritiesLoadedEvent;
            PropertyChanged += TestAggregateGlass_PropertyChanged;
        }

        private void TestAggregateGlass_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedScale")
            {
                aggregateGlass.NeedUpDateGlass(SelectedSecurity?.Id);
            }
        }

        private void AggregateGlass_NewInsideQuotesEvent(InsideQuotes insideQuotes)
        {
            Task.Run(() =>
            {
                try
                {
                    if (SelectedSecurity?.Id != insideQuotes.SecurityId || selectedScale == 0)
                    {
                        return; // стакан не потому инструменту который надо
                    }
                    List<MarketDepthLevel> mdLevels = insideQuotes.ScaledQuotes[selectedScale];
                    UpdateGlass((List<MarketDepthLevel>)mdLevels.Clone(), insideQuotes.CentreQuotes[selectedScale]);
                }
                catch (Exception ex)
                {
                Debug.WriteLine(ex.Message);
                }
            }
            );
        }


        DateTime listGlassScrollIntoViewTime = DateTime.Now;

        private void UpdateGlass(List<MarketDepthLevel> mdLevels, int centreQuotes)
        {
            try
            {
                Dispatcher?.Invoke(new Action(() =>
                {
                    try
                    {
                        List<MarketDepthLevel>? d = (List<MarketDepthLevel>?)listGlass.ItemsSource;
                        listGlass.ItemsSource = mdLevels;
                        d?.Clear();
                        if (listGlassScrollIntoViewTime.AddSeconds(2) < DateTime.Now && mdLevels.Count > centreQuotes)
                        {
                            listGlass.ScrollIntoView(mdLevels[centreQuotes - 2]);
                            // listGlass.ScrollIntoView(mdLevels[centreQuotes + 2]);
                            listGlassScrollIntoViewTime = DateTime.Now;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }));
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

            aggregateGlass.SubscribAllScaledGlass(selectedSecurity.Id);
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
