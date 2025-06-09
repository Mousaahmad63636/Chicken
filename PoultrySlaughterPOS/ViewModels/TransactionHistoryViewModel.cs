// PoultrySlaughterPOS/ViewModels/TransactionHistoryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services;
using PoultrySlaughterPOS.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using PoultrySlaughterPOS.Services.Repositories.Interfaces;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Represents a transaction record for display in the transaction history
    /// </summary>
    public class TransactionRecord
    {
        public int Id { get; set; }
        public string TransactionType { get; set; } = string.Empty; // "فاتورة" or "دفعة"
        public string TransactionNumber { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string StatusIcon { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;

        // Additional fields for invoices
        public string? TruckNumber { get; set; }
        public decimal? NetWeight { get; set; }
        public decimal? UnitPrice { get; set; }

        // Additional fields for payments
        public string? RelatedInvoiceNumber { get; set; }

        // Display properties
        public string AmountDisplay => Amount >= 0 ? $"+{Amount:N2}" : $"{Amount:N2}";
        public string TransactionDateDisplay => TransactionDate.ToString("yyyy/MM/dd HH:mm");
        public string WeightDisplay => NetWeight.HasValue ? $"{NetWeight:N2} كغ" : "-";
        public string UnitPriceDisplay => UnitPrice.HasValue ? $"{UnitPrice:N2}" : "-";
    }

    /// <summary>
    /// Summary statistics for transaction history
    /// </summary>
    public class TransactionSummary
    {
        public int TotalTransactions { get; set; }
        public int InvoicesCount { get; set; }
        public int PaymentsCount { get; set; }
        public decimal TotalInvoicesAmount { get; set; }
        public decimal TotalPaymentsAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal AverageInvoiceAmount { get; set; }
        public decimal AveragePaymentAmount { get; set; }
    }

    /// <summary>
    /// ViewModel for transaction history management with comprehensive filtering and display capabilities
    /// </summary>
    public partial class TransactionHistoryViewModel : ObservableObject
    {
        #region Private Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionHistoryViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;

        private ObservableCollection<TransactionRecord> _transactions = new();
        private ICollectionView? _transactionsView;
        private ObservableCollection<Customer> _customers = new();

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today;

        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private Customer? _selectedCustomer;

        [ObservableProperty]
        private string _selectedTransactionType = "الكل";

        [ObservableProperty]
        private string _selectedPaymentMethod = "الكل";

        [ObservableProperty]
        private TransactionRecord? _selectedTransaction;

        [ObservableProperty]
        private TransactionSummary? _summary;

        [ObservableProperty]
        private string _statusMessage = "جاهز لعرض تاريخ المعاملات";

        [ObservableProperty]
        private string _statusIcon = "History";

        [ObservableProperty]
        private string _statusColor = "#007BFF";

        #endregion

        #region Public Properties

        /// <summary>
        /// Collection of transaction records for display
        /// </summary>
        public ObservableCollection<TransactionRecord> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        /// <summary>
        /// Collection view for filtering and sorting transactions
        /// </summary>
        public ICollectionView? TransactionsView
        {
            get => _transactionsView;
            private set => SetProperty(ref _transactionsView, value);
        }

        /// <summary>
        /// Collection of customers for filtering
        /// </summary>
        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        /// <summary>
        /// Available transaction types for filtering
        /// </summary>
        public List<string> TransactionTypes { get; } = new()
        {
            "الكل", "فاتورة", "دفعة"
        };

        /// <summary>
        /// Available payment methods for filtering
        /// </summary>
        public List<string> PaymentMethods { get; } = new()
        {
            "الكل", "CASH", "CHECK", "BANK_TRANSFER", "CREDIT_CARD", "DEBIT_CARD"
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the TransactionHistoryViewModel with required dependencies
        /// </summary>
        public TransactionHistoryViewModel(
            IUnitOfWork unitOfWork,
            ILogger<TransactionHistoryViewModel> logger,
            IErrorHandlingService errorHandlingService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));

            InitializeCollectionView();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the view model with data loading
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                await LoadCustomersAsync();
                await LoadTransactionsAsync();

                _logger.LogInformation("Transaction history view model initialized successfully");
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, "خطأ في تهيئة تاريخ المعاملات");
                UpdateStatus("فشل في تحميل البيانات", "ExclamationTriangle", "#DC3545");
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Loads transactions based on current filters
        /// </summary>
        [RelayCommand]
        private async Task LoadTransactionsAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    var transactions = new List<TransactionRecord>();

                    // Load invoices with date filter
                    var invoices = await _unitOfWork.Invoices.FindAsync(
                        i => i.InvoiceDate >= StartDate && i.InvoiceDate <= EndDate.AddDays(1));

                    // Load customers and trucks for the invoices
                    var customerIds = invoices.Select(i => i.CustomerId).Distinct().ToList();
                    var truckIds = invoices.Select(i => i.TruckId).Distinct().ToList();

                    var customers = await _unitOfWork.Customers.FindAsync(c => customerIds.Contains(c.CustomerId));
                    var trucks = await _unitOfWork.Trucks.FindAsync(t => truckIds.Contains(t.TruckId));

                    var customerDict = customers.ToDictionary(c => c.CustomerId);
                    var truckDict = trucks.ToDictionary(t => t.TruckId);

                    foreach (var invoice in invoices)
                    {
                        var customer = customerDict.GetValueOrDefault(invoice.CustomerId);
                        var truck = truckDict.GetValueOrDefault(invoice.TruckId);

                        transactions.Add(new TransactionRecord
                        {
                            Id = invoice.InvoiceId,
                            TransactionType = "فاتورة",
                            TransactionNumber = invoice.InvoiceNumber,
                            TransactionDate = invoice.InvoiceDate,
                            CustomerName = customer?.CustomerName ?? "غير محدد",
                            Amount = invoice.FinalAmount,
                            PaymentMethod = "فاتورة",
                            Notes = $"الوزن الصافي: {invoice.NetWeight:N2} كغ",
                            StatusIcon = "FileText",
                            StatusColor = "#28A745",
                            TruckNumber = truck?.TruckNumber,
                            NetWeight = invoice.NetWeight,
                            UnitPrice = invoice.UnitPrice
                        });
                    }

                    // Load payments with date filter
                    var payments = await _unitOfWork.Payments.FindAsync(
                        p => p.PaymentDate >= StartDate && p.PaymentDate <= EndDate.AddDays(1));

                    // Load related customers and invoices for payments
                    var paymentCustomerIds = payments.Select(p => p.CustomerId).Distinct().ToList();
                    var paymentInvoiceIds = payments.Where(p => p.InvoiceId.HasValue).Select(p => p.InvoiceId!.Value).Distinct().ToList();

                    var paymentCustomers = await _unitOfWork.Customers.FindAsync(c => paymentCustomerIds.Contains(c.CustomerId));
                    var paymentInvoices = paymentInvoiceIds.Any() ? await _unitOfWork.Invoices.FindAsync(i => paymentInvoiceIds.Contains(i.InvoiceId)) : new List<Invoice>();

                    var paymentCustomerDict = paymentCustomers.ToDictionary(c => c.CustomerId);
                    var paymentInvoiceDict = paymentInvoices.ToDictionary(i => i.InvoiceId);

                    foreach (var payment in payments)
                    {
                        var customer = paymentCustomerDict.GetValueOrDefault(payment.CustomerId);
                        var relatedInvoice = payment.InvoiceId.HasValue ? paymentInvoiceDict.GetValueOrDefault(payment.InvoiceId.Value) : null;

                        transactions.Add(new TransactionRecord
                        {
                            Id = payment.PaymentId,
                            TransactionType = "دفعة",
                            TransactionNumber = $"PAY-{payment.PaymentId}",
                            TransactionDate = payment.PaymentDate,
                            CustomerName = customer?.CustomerName ?? "غير محدد",
                            Amount = -payment.Amount, // Negative for payments
                            PaymentMethod = payment.PaymentMethod,
                            Notes = payment.Notes ?? string.Empty,
                            StatusIcon = "Money",
                            StatusColor = "#007BFF",
                            RelatedInvoiceNumber = relatedInvoice?.InvoiceNumber
                        });
                    }

                    // Sort by date descending
                    transactions = transactions.OrderByDescending(t => t.TransactionDate).ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Transactions.Clear();
                        foreach (var transaction in transactions)
                        {
                            Transactions.Add(transaction);
                        }
                    });

                    CalculateSummary();
                    ApplyFilters();

                    UpdateStatus($"تم تحميل {transactions.Count} معاملة", "CheckCircle", "#28A745");
                    _logger.LogInformation("Loaded {Count} transactions for period {StartDate} to {EndDate}",
                        transactions.Count, StartDate, EndDate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading transactions");
                    throw;
                }
            });
        }

        /// <summary>
        /// Refreshes transaction data
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadTransactionsAsync();
        }

        /// <summary>
        /// Clears all filters
        /// </summary>
        [RelayCommand]
        private void ClearFilters()
        {
            SearchTerm = string.Empty;
            SelectedCustomer = null;
            SelectedTransactionType = "الكل";
            SelectedPaymentMethod = "الكل";
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;

            ApplyFilters();
            UpdateStatus("تم مسح المرشحات", "Times", "#007BFF");
        }

        /// <summary>
        /// Exports transaction history to CSV
        /// </summary>
        [RelayCommand]
        private async Task ExportToCsvAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    var saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Title = "حفظ تاريخ المعاملات",
                        Filter = "CSV Files (*.csv)|*.csv",
                        DefaultExt = "csv",
                        FileName = $"TransactionHistory_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        var csv = GenerateCsvContent();
                        await System.IO.File.WriteAllTextAsync(saveDialog.FileName, csv, System.Text.Encoding.UTF8);

                        UpdateStatus($"تم تصدير البيانات إلى: {System.IO.Path.GetFileName(saveDialog.FileName)}", "Save", "#28A745");
                        _logger.LogInformation("Transaction history exported to {FilePath}", saveDialog.FileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error exporting transaction history");
                    throw;
                }
            });
        }

        /// <summary>
        /// Sets filter for today's transactions
        /// </summary>
        [RelayCommand]
        private async Task FilterTodayAsync()
        {
            StartDate = DateTime.Today;
            EndDate = DateTime.Today;
            await LoadTransactionsAsync();
        }

        /// <summary>
        /// Sets filter for this week's transactions
        /// </summary>
        [RelayCommand]
        private async Task FilterThisWeekAsync()
        {
            StartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            EndDate = DateTime.Today;
            await LoadTransactionsAsync();
        }

        /// <summary>
        /// Sets filter for this month's transactions
        /// </summary>
        [RelayCommand]
        private async Task FilterThisMonthAsync()
        {
            StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            EndDate = DateTime.Today;
            await LoadTransactionsAsync();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the collection view for filtering and sorting
        /// </summary>
        private void InitializeCollectionView()
        {
            TransactionsView = CollectionViewSource.GetDefaultView(Transactions);
            TransactionsView.Filter = FilterTransactions;

            // Set up property change notifications for automatic filtering
            PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// Handles property changes for automatic filtering
        /// </summary>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchTerm) ||
                e.PropertyName == nameof(SelectedCustomer) ||
                e.PropertyName == nameof(SelectedTransactionType) ||
                e.PropertyName == nameof(SelectedPaymentMethod))
            {
                ApplyFilters();
            }
        }

        /// <summary>
        /// Applies current filters to the transactions view
        /// </summary>
        private void ApplyFilters()
        {
            TransactionsView?.Refresh();
        }

        /// <summary>
        /// Filters transactions based on current criteria
        /// </summary>
        private bool FilterTransactions(object item)
        {
            if (item is not TransactionRecord transaction)
                return false;

            // Search term filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                if (!transaction.CustomerName.ToLower().Contains(searchLower) &&
                    !transaction.TransactionNumber.ToLower().Contains(searchLower) &&
                    !transaction.Notes.ToLower().Contains(searchLower))
                {
                    return false;
                }
            }

            // Customer filter
            if (SelectedCustomer != null && transaction.CustomerName != SelectedCustomer.CustomerName)
            {
                return false;
            }

            // Transaction type filter
            if (SelectedTransactionType != "الكل" && transaction.TransactionType != SelectedTransactionType)
            {
                return false;
            }

            // Payment method filter
            if (SelectedPaymentMethod != "الكل" && transaction.PaymentMethod != SelectedPaymentMethod)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Loads customers for filtering
        /// </summary>
        private async Task LoadCustomersAsync()
        {
            try
            {
                var customers = await _unitOfWork.Customers.GetAllAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Customers.Clear();
                    foreach (var customer in customers.Where(c => c.IsActive))
                    {
                        Customers.Add(customer);
                    }
                });

                _logger.LogDebug("Loaded {Count} customers for filtering", customers.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
                throw;
            }
        }

        /// <summary>
        /// Calculates summary statistics for current transactions
        /// </summary>
        private void CalculateSummary()
        {
            try
            {
                var invoices = Transactions.Where(t => t.TransactionType == "فاتورة").ToList();
                var payments = Transactions.Where(t => t.TransactionType == "دفعة").ToList();

                Summary = new TransactionSummary
                {
                    TotalTransactions = Transactions.Count,
                    InvoicesCount = invoices.Count,
                    PaymentsCount = payments.Count,
                    TotalInvoicesAmount = invoices.Sum(i => i.Amount),
                    TotalPaymentsAmount = payments.Sum(p => Math.Abs(p.Amount)),
                    NetAmount = invoices.Sum(i => i.Amount) - payments.Sum(p => Math.Abs(p.Amount)),
                    AverageInvoiceAmount = invoices.Count > 0 ? invoices.Average(i => i.Amount) : 0,
                    AveragePaymentAmount = payments.Count > 0 ? payments.Average(p => Math.Abs(p.Amount)) : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating transaction summary");
            }
        }

        /// <summary>
        /// Generates CSV content for export
        /// </summary>
        private string GenerateCsvContent()
        {
            var lines = new List<string>
            {
                "نوع المعاملة,رقم المعاملة,التاريخ,اسم الزبون,المبلغ,طريقة الدفع,ملاحظات"
            };

            foreach (var transaction in Transactions)
            {
                var line = $"\"{transaction.TransactionType}\",\"{transaction.TransactionNumber}\"," +
                          $"\"{transaction.TransactionDateDisplay}\",\"{transaction.CustomerName}\"," +
                          $"\"{transaction.Amount:F2}\",\"{transaction.PaymentMethod}\",\"{transaction.Notes}\"";
                lines.Add(line);
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Executes an operation with loading state management
        /// </summary>
        private async Task ExecuteWithLoadingAsync(Func<Task> operation)
        {
            try
            {
                IsLoading = true;
                await operation();
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, "حدث خطأ أثناء العملية");
                UpdateStatus("حدث خطأ أثناء العملية", "ExclamationTriangle", "#DC3545");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates the status message display
        /// </summary>
        private void UpdateStatus(string message, string icon, string color)
        {
            StatusMessage = message;
            StatusIcon = icon;
            StatusColor = color;
        }

        #endregion
    }
}