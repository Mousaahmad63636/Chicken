using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PoultrySlaughterPOS.Controls
{
    public partial class TransactionHistoryControl : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties

        public static readonly DependencyProperty CustomerProperty =
            DependencyProperty.Register(
                nameof(Customer),
                typeof(Customer),
                typeof(TransactionHistoryControl),
                new PropertyMetadata(null, OnCustomerChanged));

        public static readonly DependencyProperty CustomerNameProperty =
            DependencyProperty.Register(
                nameof(CustomerName),
                typeof(string),
                typeof(TransactionHistoryControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty StartDateProperty =
            DependencyProperty.Register(
                nameof(StartDate),
                typeof(DateTime),
                typeof(TransactionHistoryControl),
                new PropertyMetadata(DateTime.Today.AddMonths(-1)));

        public static readonly DependencyProperty EndDateProperty =
            DependencyProperty.Register(
                nameof(EndDate),
                typeof(DateTime),
                typeof(TransactionHistoryControl),
                new PropertyMetadata(DateTime.Today));

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(TransactionHistoryControl),
                new PropertyMetadata(false));

        #endregion

        #region Public Properties

        public Customer? Customer
        {
            get => (Customer?)GetValue(CustomerProperty);
            set => SetValue(CustomerProperty, value);
        }

        public string CustomerName
        {
            get => (string)GetValue(CustomerNameProperty);
            set => SetValue(CustomerNameProperty, value);
        }

        public DateTime StartDate
        {
            get => (DateTime)GetValue(StartDateProperty);
            set => SetValue(StartDateProperty, value);
        }

        public DateTime EndDate
        {
            get => (DateTime)GetValue(EndDateProperty);
            set => SetValue(EndDateProperty, value);
        }

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public ObservableCollection<TransactionDisplayRecord> Transactions { get; private set; }

        public TransactionSummary? Summary { get; private set; }

        #endregion

        #region Events

        public event EventHandler<TransactionActionEventArgs>? ViewInvoiceRequested;
        public event EventHandler<TransactionActionEventArgs>? PrintInvoiceRequested;
        public event EventHandler<TransactionExportEventArgs>? ExportTransactionsRequested;
        public event EventHandler<TransactionRefreshEventArgs>? TransactionRefreshRequested;
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Private Fields

        private readonly ILogger<TransactionHistoryControl> _logger;

        #endregion

        #region Constructor

        public TransactionHistoryControl()
        {
            try
            {
                InitializeComponent();
                InitializeCollections();
                InitializeEventHandlers();
                InitializeDatePickers();

                _logger = App.Services?.GetService<ILogger<TransactionHistoryControl>>()
                          ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TransactionHistoryControl>.Instance;

                _logger.LogDebug("TransactionHistoryControl initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing TransactionHistoryControl: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Public Methods

        public async Task LoadTransactionHistoryAsync(Customer customer, DateTime startDate, DateTime endDate)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer), "Customer cannot be null for transaction history loading");

            try
            {
                IsLoading = true;
                Customer = customer;
                CustomerName = customer.CustomerName;
                StartDate = startDate;
                EndDate = endDate;

                var refreshArgs = new TransactionRefreshEventArgs(customer, startDate, endDate);
                TransactionRefreshRequested?.Invoke(this, refreshArgs);

                await Task.Delay(50, CancellationToken.None);

                _logger.LogInformation("Transaction history loading initiated for customer: {CustomerName}, Period: {StartDate} to {EndDate}",
                    customer.CustomerName, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading transaction history for customer: {CustomerName}", customer?.CustomerName);
                throw;
            }
        }

        public void UpdateTransactionHistory(IEnumerable<TransactionDisplayRecord> transactions, TransactionSummary summary)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    Transactions.Clear();

                    foreach (var transaction in transactions.OrderByDescending(t => t.InvoiceDate))
                    {
                        Transactions.Add(transaction);
                    }

                    Summary = summary;
                    UpdateSummaryDisplay();
                    UpdateUIState();

                    NoDataState.Visibility = Transactions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    IsLoading = false;

                    _logger.LogDebug("Updated transaction history with {Count} transactions", Transactions.Count);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction history display");
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private void InitializeCollections()
        {
            Transactions = new ObservableCollection<TransactionDisplayRecord>();
            TransactionsDataGrid.ItemsSource = Transactions;
        }

        private void InitializeEventHandlers()
        {
            RefreshButton.Click += RefreshButton_Click;
            ExportButton.Click += ExportButton_Click;
            ApplyFilterButton.Click += ApplyFilterButton_Click;
            ExportPdfButton.Click += ExportPdfButton_Click;
            ExportExcelButton.Click += ExportExcelButton_Click;
        }

        private void InitializeDatePickers()
        {
            StartDatePicker.SelectedDate = StartDate;
            EndDatePicker.SelectedDate = EndDate;

            StartDatePicker.SelectedDateChanged += StartDatePicker_SelectedDateChanged;
            EndDatePicker.SelectedDateChanged += EndDatePicker_SelectedDateChanged;
        }

        private void StartDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (StartDatePicker?.SelectedDate.HasValue == true)
                StartDate = StartDatePicker.SelectedDate.Value;
        }

        private void EndDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (EndDatePicker?.SelectedDate.HasValue == true)
                EndDate = EndDatePicker.SelectedDate.Value;
        }

        private static void OnCustomerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TransactionHistoryControl control && e.NewValue is Customer customer)
            {
                control.CustomerNameText.Text = $"تاريخ معاملات العميل: {customer.CustomerName}";
            }
        }

        private void UpdateSummaryDisplay()
        {
            if (Summary == null) return;

            TotalInvoicesText.Text = Summary.TotalInvoices.ToString();
            TotalAmountText.Text = $"{Summary.TotalAmount:F2} ج.م";
            PaidAmountText.Text = $"{Summary.PaidAmount:F2} ج.م";
            OutstandingAmountText.Text = $"{Summary.OutstandingAmount:F2} ج.م";
        }

        private void UpdateUIState()
        {
            RecordCountText.Text = $"{Transactions.Count} معاملة";

            bool hasData = Transactions.Count > 0;
            ExportButton.IsEnabled = hasData;
            ExportPdfButton.IsEnabled = hasData;
            ExportExcelButton.IsEnabled = hasData;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Event Handlers

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    await LoadTransactionHistoryAsync(Customer, StartDate, EndDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing transaction history");
            }
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StartDatePicker.SelectedDate.HasValue)
                    StartDate = StartDatePicker.SelectedDate.Value;

                if (EndDatePicker.SelectedDate.HasValue)
                    EndDate = EndDatePicker.SelectedDate.Value;

                if (Customer != null)
                {
                    Task.Run(async () => await LoadTransactionHistoryAsync(Customer, StartDate, EndDate));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying date filter");
            }
        }

        private void ViewInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is TransactionDisplayRecord transaction)
                {
                    var args = new TransactionActionEventArgs(transaction, Customer);
                    ViewInvoiceRequested?.Invoke(this, args);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling view invoice request");
            }
        }

        private void PrintInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is TransactionDisplayRecord transaction)
                {
                    var args = new TransactionActionEventArgs(transaction, Customer);
                    PrintInvoiceRequested?.Invoke(this, args);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling print invoice request");
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var args = new TransactionExportEventArgs(Customer, StartDate, EndDate, "PDF");
                ExportTransactionsRequested?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling export request");
            }
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var args = new TransactionExportEventArgs(Customer, StartDate, EndDate, "PDF");
                ExportTransactionsRequested?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PDF export request");
            }
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var args = new TransactionExportEventArgs(Customer, StartDate, EndDate, "Excel");
                ExportTransactionsRequested?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Excel export request");
            }
        }

        #endregion
    }

    #region Supporting Classes

    public class TransactionDisplayRecord
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string TruckNumber { get; set; } = string.Empty;
        public decimal GrossWeight { get; set; }
        public decimal NetWeight { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount => FinalAmount - PaidAmount;
        public bool IsOverdue => OutstandingAmount > 0 && InvoiceDate < DateTime.Today.AddDays(-30);
    }

    public class TransactionSummary
    {
        public int TotalInvoices { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public class TransactionActionEventArgs : EventArgs
    {
        public TransactionDisplayRecord Transaction { get; }
        public Customer? Customer { get; }

        public TransactionActionEventArgs(TransactionDisplayRecord transaction, Customer? customer)
        {
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            Customer = customer;
        }
    }

    public class TransactionExportEventArgs : EventArgs
    {
        public Customer? Customer { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public string ExportFormat { get; }

        public TransactionExportEventArgs(Customer? customer, DateTime startDate, DateTime endDate, string exportFormat)
        {
            Customer = customer;
            StartDate = startDate;
            EndDate = endDate;
            ExportFormat = exportFormat ?? "PDF";
        }
    }

    public class TransactionRefreshEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public TransactionRefreshEventArgs(Customer customer, DateTime startDate, DateTime endDate)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            StartDate = startDate;
            EndDate = endDate;
        }
    }

    #endregion
}