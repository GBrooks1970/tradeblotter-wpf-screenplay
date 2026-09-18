using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TradeBlotter.Sut.Models
{
    public class OrderModel : INotifyPropertyChanged
    {
        private string _orderId = string.Empty;
        private string _symbol = string.Empty;
        private string _side = "BUY";
        private int _quantity;
        private decimal _limitPrice;
        private string _status = "PENDING";
        private int _filledQuantity;
        private DateTime _timestamp = DateTime.Now;

        public string OrderId
        {
            get => _orderId;
            set => SetField(ref _orderId, value);
        }

        public string Symbol
        {
            get => _symbol;
            set => SetField(ref _symbol, value);
        }

        public string Side
        {
            get => _side;
            set => SetField(ref _side, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetField(ref _quantity, value);
        }

        public decimal LimitPrice
        {
            get => _limitPrice;
            set => SetField(ref _limitPrice, value);
        }

        public string Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        public int FilledQuantity
        {
            get => _filledQuantity;
            set => SetField(ref _filledQuantity, value);
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetField(ref _timestamp, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
