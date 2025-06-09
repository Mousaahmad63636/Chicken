// PoultrySlaughterPOS/Views/TransactionHistoryView.xaml.cs
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PoultrySlaughterPOS.Views
{
    /// <summary>
    /// Interaction logic for TransactionHistoryView.xaml
    /// Provides comprehensive transaction history display and management interface
    /// </summary>
    public partial class TransactionHistoryView : UserControl
    {
        #region Private Fields

        private readonly ILogger<TransactionHistoryView> _logger;
        private TransactionHistoryViewModel? _viewModel;
        private bool _isInitialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the TransactionHistoryView with logging support
        /// </summary>
        public TransactionHistoryView(ILogger<TransactionHistoryView> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            _logger.LogDebug("Transaction history view initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the view model for data binding and initializes the view
        /// </summary>
        /// <param name="viewModel">The transaction history view model</param>
        public void SetViewModel(TransactionHistoryViewModel viewModel)
        {
            try
            {
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
                DataContext = _viewModel;

                _logger.LogDebug("Transaction history view model set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting transaction history view model");
                throw;
            }
        }

        /// <summary>
        /// Initializes the view asynchronously with data loading
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                if (_viewModel == null)
                {
                    _logger.LogWarning("Cannot initialize view - view model is null");
                    return;
                }

                if (_isInitialized)
                {
                    _logger.LogDebug("View already initialized, skipping");
                    return;
                }

                _logger.LogInformation("Initializing transaction history view");

                await _viewModel.InitializeAsync();
                _isInitialized = true;

                _logger.LogInformation("Transaction history view initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing transaction history view");
                throw;
            }
        }

        /// <summary>
        /// Refreshes the view data
        /// </summary>
        public async Task RefreshAsync()
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.RefreshCommand.ExecuteAsync(null);
                    _logger.LogDebug("Transaction history view refreshed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing transaction history view");
                throw;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the view loaded event
        /// </summary>
        private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
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
                _logger.LogError(ex, "Error handling view loaded event");
            }
        }

        /// <summary>
        /// Handles the view unloaded event for cleanup
        /// </summary>
        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // Cleanup if needed
                _logger.LogDebug("Transaction history view unloaded");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling view unloaded event");
            }
        }

        #endregion
    }
}