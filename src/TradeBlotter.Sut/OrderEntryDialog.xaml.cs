using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using TradeBlotter.Sut.Models;

namespace TradeBlotter.Sut
{
    public partial class OrderEntryDialog : Window
    {
        public OrderModel? CreatedOrder { get; private set; }

        public OrderEntryDialog()
        {
            InitializeComponent();
        }

        private void CmbOrderType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TxtLimitPrice == null) return;
            var selected = (CmbOrderType.SelectedItem as ComboBoxItem)?.Content?.ToString();
            TxtLimitPrice.IsEnabled = selected != "MARKET";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string symbol = (CmbSymbol.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "EUR/USD";
            string orderType = (CmbOrderType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "LIMIT";

            if (!int.TryParse(TxtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Please enter a valid quantity greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal price = 0m;
            if (orderType == "LIMIT")
            {
                if (!decimal.TryParse(TxtLimitPrice.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out price) &&
                    !decimal.TryParse(TxtLimitPrice.Text, out price))
                {
                    MessageBox.Show("Please enter a valid price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                price = 1.0842m; // Mock current market price
            }

            CreatedOrder = new OrderModel
            {
                Symbol = symbol,
                Side = "BUY",
                Quantity = quantity,
                LimitPrice = price,
                Status = "PENDING",
                FilledQuantity = 0,
                Timestamp = DateTime.Now
            };

            DialogResult = true;
            Close();
        }
    }
}
