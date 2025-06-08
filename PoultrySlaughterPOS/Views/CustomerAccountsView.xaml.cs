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

                ConfigureViewModelEventHandlers();

                _logger.LogInformation("ViewModel set successfully for CustomerAccountsView");
                _ = InitializeViewModelAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting ViewModel for CustomerAccountsView");
                throw;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    _logger.LogDebug("CustomerAccountsView already initialized, skipping");
                    return;
                }

                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                    _isInitialized = true;
                    _logger.LogInformation("CustomerAccountsView initialization completed successfully");
                }
                else
                {
                    _logger.LogWarning("Cannot initialize CustomerAccountsView: ViewModel is null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CustomerAccountsView initialization");
                throw;
            }
        }

        public void Cleanup()
        {
            try
            {
                _viewModel?.Cleanup();
                _isInitialized = false;
                _logger.LogDebug("CustomerAccountsView cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during CustomerAccountsView cleanup");
            }
        }

        #endregion

        #region Event Handlers

        private async void CustomerAccountsView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized && _viewModel != null)
                {
                    await InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CustomerAccountsView Loaded event");
                MessageBox.Show(
                    "حدث خطأ أثناء تحميل صفحة الزبائن. يرجى المحاولة مرة أخرى.",
                    "خطأ في التحميل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void CustomerAccountsView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during CustomerAccountsView Unloaded event");
            }
        }

        private void CustomerAccountsView_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;

                switch (e.Key)
                {
                    case Key.F1:
                        if (_viewModel.AddNewCustomerCommand.CanExecute(null))
                        {
                            _viewModel.AddNewCustomerCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    case Key.F2:
                        if (_viewModel.EditCustomerCommand.CanExecute(null))
                        {
                            _viewModel.EditCustomerCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    case Key.F5:
                        if (_viewModel.RefreshDataCommand.CanExecute(null))
                        {
                            _viewModel.RefreshDataCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    case Key.Delete:
                        if (_viewModel.DeleteCustomerCommand.CanExecute(null))
                        {
                            _viewModel.DeleteCustomerCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    case Key.Escape:
                        if (_viewModel.ClearFiltersCommand.CanExecute(null))
                        {
                            _viewModel.ClearFiltersCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    default:
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            switch (e.Key)
                            {
                                case Key.F:
                                    SearchTextBox?.Focus();
                                    SearchTextBox?.SelectAll();
                                    e.Handled = true;
                                    break;

                                case Key.R:
                                    if (_viewModel.RefreshDataCommand.CanExecute(null))
                                    {
                                        _viewModel.RefreshDataCommand.Execute(null);
                                        e.Handled = true;
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling keyboard shortcut: {Key}", e.Key);
            }
        }

        private void CustomersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_viewModel?.SelectedCustomer != null &&
                    _viewModel.EditCustomerCommand.CanExecute(null))
                {
                    _viewModel.EditCustomerCommand.Execute(null);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling DataGrid double-click");
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter && CustomersDataGrid != null)
                {
                    CustomersDataGrid.Focus();

                    if (CustomersDataGrid.Items.Count > 0)
                    {
                        CustomersDataGrid.SelectedIndex = 0;
                        CustomersDataGrid.ScrollIntoView(CustomersDataGrid.SelectedItem);
                    }

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling search text box KeyDown event");
            }
        }

        private void CustomersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_viewModel?.SelectedCustomer != null &&
                    !_viewModel.IsCustomerDetailsVisible)
                {
                    _viewModel.ShowCustomerDetailsCommand?.Execute(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling DataGrid selection change");
            }
        }

        private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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

                TransactionHistoryControl.UpdateTransactionHistory(transactions, summary);
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
                Focusable = true;
                InputBindings.Clear();

                var newCustomerBinding = new KeyBinding(
                    new RelayCommand(() => _viewModel?.AddNewCustomerCommand?.Execute(null)),
                    Key.N,
                    ModifierKeys.Control);

                InputBindings.Add(newCustomerBinding);

                _logger.LogDebug("Keyboard shortcuts configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring keyboard shortcuts");
            }
        }

        private void ConfigureViewModelEventHandlers()
        {
            if (_viewModel == null) return;

            try
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring ViewModel event handlers");
            }
        }

        private async Task InitializeViewModelAsync()
        {
            try
            {
                if (_viewModel != null && !_isInitialized)
                {
                    await _viewModel.InitializeAsync();
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ViewModel initialization");

                MessageBox.Show(
                    "حدث خطأ أثناء تحميل بيانات الزبائن. يرجى التحقق من الاتصال بقاعدة البيانات والمحاولة مرة أخرى.",
                    "خطأ في التحميل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task RefreshTransactionHistoryAsync()
        {
            try
            {
                if (_viewModel?.SelectedCustomer == null) return;

                await TransactionHistoryControl.LoadTransactionHistoryAsync(
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