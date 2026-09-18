using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using TradeBlotter.Sut.Models;

namespace TradeBlotter.Sut
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<OrderModel> Orders { get; } = new();
        private readonly DispatcherTimer _tickerTimer = new();
        private int _orderSequence = 904;
        private bool _tickerFlip = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            GridBlotterOrders.ItemsSource = Orders;

            SeedDeterministicData();
            InitializePriceTicker();
        }

        private void SeedDeterministicData()
        {
            Orders.Add(new OrderModel
            {
                OrderId = "ORD-2026-0901",
                Symbol = "EUR/USD",
                Side = "BUY",
                Quantity = 250000,
                LimitPrice = 1.0845m,
                Status = "FILLED",
                FilledQuantity = 250000,
                Timestamp = DateTime.Now.AddMinutes(-20)
            });

            Orders.Add(new OrderModel
            {
                OrderId = "ORD-2026-0902",
                Symbol = "GBP/USD",
                Side = "SELL",
                Quantity = 100000,
                LimitPrice = 1.2710m,
                Status = "PENDING",
                FilledQuantity = 0,
                Timestamp = DateTime.Now.AddMinutes(-15)
            });

            Orders.Add(new OrderModel
            {
                OrderId = "ORD-2026-0903",
                Symbol = "USD/JPY",
                Side = "BUY",
                Quantity = 500000,
                LimitPrice = 152.30m,
                Status = "PARTIAL",
                FilledQuantity = 200000,
                Timestamp = DateTime.Now.AddMinutes(-10)
            });

            Orders.Add(new OrderModel
            {
                OrderId = "ORD-2026-0904",
                Symbol = "USD/CHF",
                Side = "BUY",
                Quantity = 150000,
                LimitPrice = 0.8920m,
                Status = "CANCELLED",
                FilledQuantity = 0,
                Timestamp = DateTime.Now.AddMinutes(-5)
            });

            UpdateStatusCount();
        }

        private void InitializePriceTicker()
        {
            _tickerTimer.Interval = TimeSpan.FromSeconds(1);
            _tickerTimer.Tick += (s, e) =>
            {
                _tickerFlip = !_tickerFlip;
                TxtPriceTicker.Text = _tickerFlip
                    ? "EUR/USD 1.0844 ▲ | GBP/USD 1.2712 ▼"
                    : "EUR/USD 1.0840 ▼ | GBP/USD 1.2718 ▲";
            };
            _tickerTimer.Start();
        }

        private void BtnNewOrder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OrderEntryDialog { Owner = this };
            if (dialog.ShowDialog() == true && dialog.CreatedOrder != null)
            {
                _orderSequence++;
                dialog.CreatedOrder.OrderId = $"ORD-2026-{_orderSequence:D4}";
                Orders.Insert(0, dialog.CreatedOrder);
                UpdateStatusCount();
            }
        }

        private void BtnCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (GridBlotterOrders.SelectedItem is OrderModel selected)
            {
                if (selected.Status == "PENDING" || selected.Status == "PARTIAL")
                {
                    selected.Status = "CANCELLED";
                }
            }
        }

        private void UpdateStatusCount()
        {
            TxtTotalOrders.Text = $"Total Orders: {Orders.Count}";
        }

        protected override void OnClosed(EventArgs e)
        {
            _tickerTimer.Stop();
            base.OnClosed(e);
        }
    }
}
