using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Controls;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Resolve ambiguous references by using aliases for the Controls namespace classes
using TransactionDisplayRecord = PoultrySlaughterPOS.Controls.TransactionDisplayRecord;
using TransactionSummary = PoultrySlaughterPOS.Controls.TransactionSummary;
using TransactionActionEventArgs = PoultrySlaughterPOS.Controls.TransactionActionEventArgs;
using TransactionExportEventArgs = PoultrySlaughterPOS.Controls.TransactionExportEventArgs;
using TransactionRefreshEventArgs = PoultrySlaughterPOS.Controls.TransactionRefreshEventArgs;

namespace PoultrySlaughterPOS.Views
{
    public partial class CustomerAccountsView : UserControl
    {
        #region Private Fields

        private readonly ILogger<CustomerAccountsView> _logger;
        private CustomerAccountsViewModel? _viewModel;
        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public CustomerAccountsView()
        {
            try
            {
                InitializeComponent();

                _logger = App.Services?.GetService<ILogger<CustomerAccountsView>>()
                          ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomerAccountsView>.Instance;

                ConfigureEventHandlers();
                ConfigureKeyboardShortcuts();

                _logger.LogDebug("CustomerAccountsView initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing CustomerAccountsView: {ex.Message}");
                throw;
            }
        }

        public CustomerAccountsView(CustomerAccountsViewModel viewModel) : this()
        {
            SetViewModel(viewModel);
        }

        #endregion

        #region Public Methods

        public void SetViewModel(CustomerAccountsViewModel viewModel)
        {
            try
            {
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
                DataContext = _viewModel;

                // Subscribe to ViewModel property changes
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;

                _logger.LogDebug("ViewModel set successfully for CustomerAccountsView");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting ViewModel");
                throw;
            }
        }

        #endregion

        #region Event Handlers

        private async void CustomerAccountsView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isInitialized) return;

                _logger.LogDebug("CustomerAccountsView loading started");

                // Initialize the view model if it's available through DI
                if (_viewModel == null)
                {
                    _viewModel = App.Services?.GetService<CustomerAccountsViewModel>();
                    if (_viewModel != null)
                    {
                        SetViewModel(_viewModel);
                    }
                }

                // Initialize the view model data (call on ViewModel, not View)
                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                }

                _isInitialized = true;
                _logger.LogInformation("CustomerAccountsView loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CustomerAccountsView loading");
                MessageBox.Show(
                    "حدث خطأ أثناء تحميل شاشة حسابات العملاء. " +
                    "يرجى التحقق من الاتصال بقاعدة البيانات والمحاولة مرة أخرى.",
                    "خطأ في التحميل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CustomerAccountsView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Call cleanup on ViewModel, not View
                _viewModel?.Cleanup();
                _logger.LogDebug("CustomerAccountsView unloaded");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during CustomerAccountsView unloading");
            }
        }

        private void CustomerAccountsView_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Handle keyboard shortcuts with correct command names
                if (e.Key == Key.F1 && _viewModel?.AddNewCustomerCommand.CanExecute(null) == true)
                {
                    _viewModel.AddNewCustomerCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.F5 && _viewModel?.RefreshDataCommand.CanExecute(null) == true)
                {
                    _viewModel.RefreshDataCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    // Clear selection or close dialogs
                    if (_viewModel != null)
                    {
                        _viewModel.SelectedCustomer = null;
                    }
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling keyboard shortcut: {Key}", e.Key);
            }
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(CustomerAccountsViewModel.SelectedCustomer) &&
                    _viewModel?.IsTransactionHistoryVisible == true)
                {
                    await RefreshTransactionHistoryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling ViewModel property change: {PropertyName}", e.PropertyName);
            }
        }

        #endregion

        #region Transaction History Event Handlers

        private async void TransactionHistoryControl_TransactionRefreshRequested(object sender, TransactionRefreshEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;

                _logger.LogDebug("Transaction refresh requested for customer: {CustomerName}", e.Customer.CustomerName);

                await _viewModel.LoadCustomerTransactionsAsync();

                var transactions = await ConvertToTransactionDisplayRecords(_viewModel.CustomerInvoices);
                var summary = CalculateTransactionSummary(transactions);

                // FIXED: Access the instance properly through the XAML control name with 'this.'
                this.TransactionHistoryControl.UpdateTransactionHistory(transactions, summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling transaction refresh request");
            }
        }

        private void TransactionHistoryControl_ViewInvoiceRequested(object sender, TransactionActionEventArgs e)
        {
            try
            {
                if (_viewModel?.ViewInvoiceDetailsCommand.CanExecute(e.Transaction.InvoiceId) == true)
                {
                    _viewModel.ViewInvoiceDetailsCommand.Execute(e.Transaction.InvoiceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling view invoice request");
            }
        }

        private void TransactionHistoryControl_PrintInvoiceRequested(object sender, TransactionActionEventArgs e)
        {
            try
            {
                if (_viewModel?.PrintInvoiceCommand.CanExecute(e.Transaction.InvoiceId) == true)
                {
                    _viewModel.PrintInvoiceCommand.Execute(e.Transaction.InvoiceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling print invoice request");
            }
        }

        private void TransactionHistoryControl_ExportTransactionsRequested(object sender, TransactionExportEventArgs e)
        {
            try
            {
                if (_viewModel?.ExportTransactionsCommand.CanExecute(e.ExportFormat) == true)
                {
                    _viewModel.ExportTransactionsCommand.Execute(new
                    {
                        Format = e.ExportFormat,
                        Customer = e.Customer,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling export transactions request");
            }
        }

        #endregion

        #region Private Methods

        private void ConfigureEventHandlers()
        {
            try
            {
                Loaded += CustomerAccountsView_Loaded;
                Unloaded += CustomerAccountsView_Unloaded;
                KeyDown += CustomerAccountsView_KeyDown;

                _logger.LogDebug("Event handlers configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring event handlers");
                throw;
            }
        }

        private void ConfigureKeyboardShortcuts()
        {
            try
            {
                // Enable keyboard navigation
                Focusable = true;

                _logger.LogDebug("Keyboard shortcuts configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring keyboard shortcuts");
            }
        }

        private async Task RefreshTransactionHistoryAsync()
        {
            try
            {
                if (_viewModel?.SelectedCustomer == null) return;

                // FIXED: Access the instance properly through the XAML control name with 'this.'
                await this.TransactionHistoryControl.LoadTransactionHistoryAsync(
                    _viewModel.SelectedCustomer,
                    _viewModel.StartDate,
                    _viewModel.EndDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing transaction history");
            }
        }

        private async Task<IEnumerable<TransactionDisplayRecord>> ConvertToTransactionDisplayRecords(IEnumerable<Invoice> invoices)
        {
            return await Task.Run(() =>
            {
                return invoices.Select(invoice => new TransactionDisplayRecord
                {
                    InvoiceId = invoice.InvoiceId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    TruckNumber = invoice.Truck?.TruckNumber ?? "غير محدد",
                    GrossWeight = invoice.GrossWeight,
                    NetWeight = invoice.NetWeight,
                    FinalAmount = invoice.FinalAmount,
                    PaidAmount = invoice.Payments?.Sum(p => p.Amount) ?? 0
                }).ToList();
            });
        }

        private TransactionSummary CalculateTransactionSummary(IEnumerable<TransactionDisplayRecord> transactions)
        {
            var transactionList = transactions.ToList();

            return new TransactionSummary
            {
                TotalInvoices = transactionList.Count,
                TotalAmount = transactionList.Sum(t => t.FinalAmount),
                PaidAmount = transactionList.Sum(t => t.PaidAmount),
                OutstandingAmount = transactionList.Sum(t => t.OutstandingAmount)
            };
        }

        #endregion

        #region Helper Classes

        private class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool>? _canExecute;

            public RelayCommand(Action execute, Func<bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

            public void Execute(object? parameter) => _execute();

            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}