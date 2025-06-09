// PoultrySlaughterPOS/ViewModels/TruckManagementViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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
using System.Windows;
using System.Windows.Data;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Enterprise-grade Truck Management ViewModel implementing comprehensive truck and driver management,
    /// fleet tracking, performance analytics, and maintenance scheduling for poultry slaughter operations.
    /// 
    /// ARCHITECTURE: Defensive programming with comprehensive null safety, error handling,
    /// and robust state management for mission-critical fleet operations.
    /// </summary>
    public partial class TruckManagementViewModel : ObservableObject
    {
        #region Private Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TruckManagementViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IServiceProvider _serviceProvider;

        // Collections for UI binding
        private ObservableCollection<Truck> _trucks = new();
        private ObservableCollection<TruckLoad> _truckLoads = new();
        private ObservableCollection<string> _validationErrors = new();

        // Current selections and state
        private Truck? _selectedTruck;
        private TruckLoad? _selectedTruckLoad;

        // Search and filtering
        private string _searchText = string.Empty;
        private bool _showActiveOnly = true;
        private bool _showAvailableOnly = false;

        // Date range filtering
        private DateTime _startDate = DateTime.Today.AddMonths(-1);
        private DateTime _endDate = DateTime.Today;

        // UI State
        private bool _isLoading = false;
        private bool _hasValidationErrors = false;
        private string _statusMessage = "جاهز لإدارة الشاحنات والسائقين";
        private string _statusIcon = "Truck";
        private string _statusColor = "#28A745";

        // Statistics and analytics
        private int _totalTrucksCount = 0;
        private int _activeTrucksCount = 0;
        private int _availableTrucksCount = 0;
        private int _trucksInTransitCount = 0;
        private decimal _totalLoadCapacity = 0;
        private decimal _averageLoadPerTruck = 0;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;
        private int _totalRecords = 0;

        // View states
        private bool _isTruckDetailsVisible = true;
        private bool _isLoadHistoryVisible = false;
        private bool _isPerformanceAnalysisVisible = false;

        // Form fields for truck creation/editing
        private string _truckNumber = string.Empty;
        private string _driverName = string.Empty;
        private bool _isActive = true;
        private bool _isEditMode = false;

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private bool _isInitialized = false;

        public ObservableCollection<Truck> Trucks
        {
            get => _trucks;
            set => SetProperty(ref _trucks, value);
        }

        public ObservableCollection<TruckLoad> TruckLoads
        {
            get => _truckLoads;
            set => SetProperty(ref _truckLoads, value);
        }

        public ObservableCollection<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value);
        }

        public Truck? SelectedTruck
        {
            get => _selectedTruck;
            set
            {
                if (SetProperty(ref _selectedTruck, value))
                {
                    OnSelectedTruckChanged();
                }
            }
        }

        public TruckLoad? SelectedTruckLoad
        {
            get => _selectedTruckLoad;
            set => SetProperty(ref _selectedTruckLoad, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        public bool ShowActiveOnly
        {
            get => _showActiveOnly;
            set
            {
                if (SetProperty(ref _showActiveOnly, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        public bool ShowAvailableOnly
        {
            get => _showAvailableOnly;
            set
            {
                if (SetProperty(ref _showAvailableOnly, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    _ = LoadTruckLoadsAsync();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    _ = LoadTruckLoadsAsync();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set => SetProperty(ref _hasValidationErrors, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string StatusIcon
        {
            get => _statusIcon;
            set => SetProperty(ref _statusIcon, value);
        }

        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        // Statistics Properties
        public int TotalTrucksCount
        {
            get => _totalTrucksCount;
            set => SetProperty(ref _totalTrucksCount, value);
        }

        public int ActiveTrucksCount
        {
            get => _activeTrucksCount;
            set => SetProperty(ref _activeTrucksCount, value);
        }

        public int AvailableTrucksCount
        {
            get => _availableTrucksCount;
            set => SetProperty(ref _availableTrucksCount, value);
        }

        public int TrucksInTransitCount
        {
            get => _trucksInTransitCount;
            set => SetProperty(ref _trucksInTransitCount, value);
        }

        public decimal TotalLoadCapacity
        {
            get => _totalLoadCapacity;
            set => SetProperty(ref _totalLoadCapacity, value);
        }

        public decimal AverageLoadPerTruck
        {
            get => _averageLoadPerTruck;
            set => SetProperty(ref _averageLoadPerTruck, value);
        }

        // Pagination Properties
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    _ = LoadTrucksAsync();
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    _ = LoadTrucksAsync();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set => SetProperty(ref _totalRecords, value);
        }

        // View State Properties
        public bool IsTruckDetailsVisible
        {
            get => _isTruckDetailsVisible;
            set => SetProperty(ref _isTruckDetailsVisible, value);
        }

        public bool IsLoadHistoryVisible
        {
            get => _isLoadHistoryVisible;
            set => SetProperty(ref _isLoadHistoryVisible, value);
        }

        public bool IsPerformanceAnalysisVisible
        {
            get => _isPerformanceAnalysisVisible;
            set => SetProperty(ref _isPerformanceAnalysisVisible, value);
        }

        // Form Properties
        public string TruckNumber
        {
            get => _truckNumber;
            set
            {
                if (SetProperty(ref _truckNumber, value))
                {
                    ValidateForm();
                }
            }
        }

        public string DriverName
        {
            get => _driverName;
            set
            {
                if (SetProperty(ref _driverName, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        /// <summary>
        /// Indicates whether previous page navigation is available
        /// </summary>
        public bool CanGoPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Indicates whether next page navigation is available
        /// </summary>
        public bool CanGoNextPage => CurrentPage < TotalPages;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes TruckManagementViewModel with comprehensive dependency injection
        /// and enhanced null safety validation
        /// </summary>
        public TruckManagementViewModel(
            IUnitOfWork unitOfWork,
            ILogger<TruckManagementViewModel> logger,
            IErrorHandlingService errorHandlingService,
            IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            InitializeCollections();
            InitializeCommands();

            _logger.LogInformation("TruckManagementViewModel initialized with enterprise-grade architecture");
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initializes collections with UI optimization and thread safety
        /// </summary>
        private void InitializeCollections()
        {
            try
            {
                // Enable cross-thread collection access for UI updates
                BindingOperations.EnableCollectionSynchronization(Trucks, new object());
                BindingOperations.EnableCollectionSynchronization(TruckLoads, new object());
                BindingOperations.EnableCollectionSynchronization(ValidationErrors, new object());

                _logger.LogDebug("Collections initialized with thread-safe synchronization");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing collections");
                throw;
            }
        }

        /// <summary>
        /// Initializes command bindings for UI interaction
        /// </summary>
        private void InitializeCommands()
        {
            try
            {
                // Commands are auto-generated by CommunityToolkit.Mvvm
                _logger.LogDebug("Commands initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing commands");
                throw;
            }
        }

        /// <summary>
        /// Main initialization method called by the view
        /// </summary>
        public async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                _logger.LogDebug("TruckManagementViewModel already initialized");
                return;
            }

            try
            {
                IsLoading = true;
                UpdateStatus("جاري تحميل بيانات الشاحنات...", "Loading", "#FFC107");

                await LoadTrucksAsync();
                await LoadStatisticsAsync();
                await LoadTruckLoadsAsync();

                IsInitialized = true;
                UpdateStatus("جاهز لإدارة الشاحنات والسائقين", "Truck", "#28A745");

                _logger.LogInformation("TruckManagementViewModel initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TruckManagementViewModel initialization");
                UpdateStatus("خطأ في تحميل البيانات", "ExclamationTriangle", "#DC3545");
                await _errorHandlingService.HandleExceptionAsync(ex, "خطأ في تحميل بيانات الشاحنات");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Data Loading Methods

        /// <summary>
        /// Loads trucks with pagination and filtering
        /// </summary>
        private async Task LoadTrucksAsync()
        {
            try
            {
                IsLoading = true;
                var repository = _unitOfWork.Trucks;

                // Build filter predicate
                System.Linq.Expressions.Expression<Func<Truck, bool>>? filter = null;

                if (ShowActiveOnly)
                {
                    filter = t => t.IsActive;
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    if (filter == null)
                    {
                        filter = t => t.TruckNumber.ToLower().Contains(searchLower) ||
                                     t.DriverName.ToLower().Contains(searchLower);
                    }
                    else
                    {
                        var originalFilter = filter;
                        filter = t => originalFilter.Compile()(t) &&
                                     (t.TruckNumber.ToLower().Contains(searchLower) ||
                                      t.DriverName.ToLower().Contains(searchLower));
                    }
                }

                // Use the correct repository method signature with explicit type declaration
                var (trucks, totalCount) = await repository.GetPagedAsync(
                    CurrentPage,
                    PageSize,
                    filter,
                    t => t.TruckNumber,
                    true);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Trucks.Clear();
                    foreach (var truck in trucks)
                    {
                        Trucks.Add(truck);
                    }
                });

                TotalRecords = totalCount;
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

                _logger.LogDebug("Loaded {Count} trucks for page {Page}", trucks.Count(), CurrentPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading trucks");
                await _errorHandlingService.HandleErrorAsync(ex, "خطأ في تحميل بيانات الشاحنات");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Loads truck loads for selected date range
        /// </summary>
        private async Task LoadTruckLoadsAsync()
        {
            try
            {
                if (SelectedTruck == null) return;

                var repository = _unitOfWork.TruckLoads;
                var loads = await repository.FindAsync(
                    tl => tl.TruckId == SelectedTruck.TruckId &&
                          tl.LoadDate >= StartDate &&
                          tl.LoadDate <= EndDate);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TruckLoads.Clear();
                    foreach (var load in loads.OrderByDescending(l => l.LoadDate))
                    {
                        TruckLoads.Add(load);
                    }
                });

                _logger.LogDebug("Loaded {Count} truck loads for truck {TruckId}", loads.Count(), SelectedTruck.TruckId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading truck loads for truck {TruckId}", SelectedTruck?.TruckId);
                await _errorHandlingService.HandleExceptionAsync(ex, "خطأ في تحميل تاريخ التحميل");
            }
        }

        /// <summary>
        /// Loads fleet statistics and analytics
        /// </summary>
        private async Task LoadStatisticsAsync()
        {
            try
            {
                var repository = _unitOfWork.Trucks;

                TotalTrucksCount = await repository.CountAsync();
                ActiveTrucksCount = await repository.CountAsync(t => t.IsActive);
                AvailableTrucksCount = (await repository.GetTrucksForLoadingAsync()).Count();
                TrucksInTransitCount = (await repository.GetTrucksInTransitAsync()).Count();

                // Calculate load statistics
                var performanceData = await repository.GetTruckPerformanceAsync(StartDate, EndDate);
                TotalLoadCapacity = performanceData.Sum(p => p.TotalWeight);
                AverageLoadPerTruck = ActiveTrucksCount > 0 ? TotalLoadCapacity / ActiveTrucksCount : 0;

                _logger.LogDebug("Fleet statistics loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading fleet statistics");
                await _errorHandlingService.HandleErrorAsync(ex, "خطأ في تحميل إحصائيات الأسطول");
            }
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Adds a new truck to the fleet
        /// </summary>
        [RelayCommand]
        private async Task AddTruckAsync()
        {
            try
            {
                if (!ValidateForm())
                {
                    UpdateStatus("يرجى تصحيح الأخطاء في النموذج", "ExclamationTriangle", "#DC3545");
                    return;
                }

                IsLoading = true;
                UpdateStatus("جاري إضافة الشاحنة...", "Loading", "#FFC107");

                var truck = new Truck
                {
                    TruckNumber = TruckNumber.Trim(),
                    DriverName = DriverName.Trim(),
                    IsActive = IsActive,
                    CreatedDate = DateTime.Now
                };

                var repository = _unitOfWork.Trucks;

                // Check for duplicate truck number
                var existingTruck = await repository.GetTruckByNumberAsync(truck.TruckNumber);
                if (existingTruck != null)
                {
                    ValidationErrors.Add("رقم الشاحنة موجود مسبقاً");
                    HasValidationErrors = true;
                    UpdateStatus("رقم الشاحنة موجود مسبقاً", "ExclamationTriangle", "#DC3545");
                    return;
                }

                await repository.AddAsync(truck);
                await _unitOfWork.SaveChangesAsync();

                // Refresh the trucks list
                await LoadTrucksAsync();
                await LoadStatisticsAsync();

                // Clear form
                ClearForm();

                UpdateStatus($"تم إضافة الشاحنة {truck.TruckNumber} بنجاح", "CheckCircle", "#28A745");
                _logger.LogInformation("Successfully added truck {TruckNumber}", truck.TruckNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding truck");
                UpdateStatus("خطأ في إضافة الشاحنة", "ExclamationTriangle", "#DC3545");
                await _errorHandlingService.HandleErrorAsync(ex, "خطأ في إضافة الشاحنة");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates the selected truck
        /// </summary>
        [RelayCommand]
        private async Task UpdateTruckAsync()
        {
            try
            {
                if (SelectedTruck == null)
                {
                    UpdateStatus("يرجى اختيار شاحنة للتعديل", "ExclamationTriangle", "#DC3545");
                    return;
                }

                if (!ValidateForm())
                {
                    UpdateStatus("يرجى تصحيح الأخطاء في النموذج", "ExclamationTriangle", "#DC3545");
                    return;
                }

                IsLoading = true;
                UpdateStatus("جاري تحديث بيانات الشاحنة...", "Loading", "#FFC107");

                var repository = _unitOfWork.Trucks;

                // Check for duplicate truck number (excluding current truck)
                var existingTruck = await repository.GetTruckByNumberAsync(TruckNumber.Trim());
                if (existingTruck != null && existingTruck.TruckId != SelectedTruck.TruckId)
                {
                    ValidationErrors.Add("رقم الشاحنة موجود مسبقاً");
                    HasValidationErrors = true;
                    UpdateStatus("رقم الشاحنة موجود مسبقاً", "ExclamationTriangle", "#DC3545");
                    return;
                }

                SelectedTruck.TruckNumber = TruckNumber.Trim();
                SelectedTruck.DriverName = DriverName.Trim();
                SelectedTruck.IsActive = IsActive;

                await repository.UpdateAsync(SelectedTruck);
                await _unitOfWork.SaveChangesAsync();

                // Refresh the trucks list
                await LoadTrucksAsync();
                await LoadStatisticsAsync();

                // Exit edit mode and clear form
                IsEditMode = false;
                ClearForm();

                UpdateStatus($"تم تحديث الشاحنة {SelectedTruck.TruckNumber} بنجاح", "CheckCircle", "#28A745");
                _logger.LogInformation("Successfully updated truck {TruckNumber}", SelectedTruck.TruckNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating truck {TruckId}", SelectedTruck?.TruckId);
                UpdateStatus("خطأ في تحديث الشاحنة", "ExclamationTriangle", "#DC3545");
                await _errorHandlingService.HandleErrorAsync(ex, "خطأ في تحديث الشاحنة");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Deletes the selected truck (soft delete by setting IsActive = false)
        /// </summary>
        [RelayCommand]
        private async Task DeleteTruckAsync()
        {
            try
            {
                if (SelectedTruck == null)
                {
                    UpdateStatus("يرجى اختيار شاحنة للحذف", "ExclamationTriangle", "#DC3545");
                    return;
                }

                var result = MessageBox.Show(
                    $"هل أنت متأكد من حذف الشاحنة {SelectedTruck.TruckNumber}؟\n\nسيتم إلغاء تفعيل الشاحنة فقط ولن يتم حذفها نهائياً.",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result != MessageBoxResult.Yes) return;

                IsLoading = true;
                UpdateStatus("جاري حذف الشاحنة...", "Loading", "#FFC107");

                var repository = _unitOfWork.Trucks;

                // Soft delete by setting IsActive = false
                SelectedTruck.IsActive = false;
                await repository.UpdateAsync(SelectedTruck);
                await _unitOfWork.SaveChangesAsync();

                // Refresh the trucks list
                await LoadTrucksAsync();
                await LoadStatisticsAsync();

                // Clear selection and form
                SelectedTruck = null;
                ClearForm();

                UpdateStatus("تم حذف الشاحنة بنجاح", "CheckCircle", "#28A745");
                _logger.LogInformation("Successfully deleted truck {TruckNumber}", SelectedTruck?.TruckNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting truck {TruckId}", SelectedTruck?.TruckId);
                UpdateStatus("خطأ في حذف الشاحنة", "ExclamationTriangle", "#DC3545");
                await _errorHandlingService.HandleErrorAsync(ex, "خطأ في حذف الشاحنة");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Enters edit mode for the selected truck
        /// </summary>
        [RelayCommand]
        private void EditTruck()
        {
            if (SelectedTruck == null)
            {
                UpdateStatus("يرجى اختيار شاحنة للتعديل", "ExclamationTriangle", "#DC3545");
                return;
            }

            IsEditMode = true;
            TruckNumber = SelectedTruck.TruckNumber;
            DriverName = SelectedTruck.DriverName;
            IsActive = SelectedTruck.IsActive;

            UpdateStatus($"جاري تعديل الشاحنة {SelectedTruck.TruckNumber}", "Edit", "#FFC107");
        }

        /// <summary>
        /// Cancels edit mode and clears the form
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            IsEditMode = false;
            ClearForm();
            UpdateStatus("تم إلغاء التعديل", "Info", "#17A2B8");
        }

        /// <summary>
        /// Refreshes all data
        /// </summary>
        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;
                UpdateStatus("جاري تحديث البيانات...", "Refresh", "#FFC107");

                await LoadTrucksAsync();
                await LoadStatisticsAsync();
                await LoadTruckLoadsAsync();

                UpdateStatus("تم تحديث البيانات بنجاح", "CheckCircle", "#28A745");
                _logger.LogInformation("Data refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing data");
                UpdateStatus("خطأ في تحديث البيانات", "ExclamationTriangle", "#DC3545");
                await _errorHandlingService.HandleExceptionAsync(ex, "خطأ في تحديث البيانات");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Applies current filters to the trucks list
        /// </summary>
        [RelayCommand]
        private async Task ApplyFiltersAsync()
        {
            CurrentPage = 1; // Reset to first page when applying filters
            await LoadTrucksAsync();
        }

        /// <summary>
        /// Clears all filters
        /// </summary>
        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            SearchText = string.Empty;
            ShowActiveOnly = true;
            ShowAvailableOnly = false;
            CurrentPage = 1;

            await LoadTrucksAsync();
            UpdateStatus("تم مسح المرشحات", "Info", "#17A2B8");
        }

        /// <summary>
        /// Navigates to the previous page
        /// </summary>
        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CanGoPreviousPage)
            {
                CurrentPage--;
                await Task.Delay(10); // Brief delay for UI responsiveness
            }
        }

        /// <summary>
        /// Navigates to the next page
        /// </summary>
        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CanGoNextPage)
            {
                CurrentPage++;
                await Task.Delay(10); // Brief delay for UI responsiveness
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Handles selection change for truck
        /// </summary>
        private async void OnSelectedTruckChanged()
        {
            try
            {
                if (SelectedTruck != null)
                {
                    await LoadTruckLoadsAsync();
                    UpdateStatus($"تم اختيار الشاحنة {SelectedTruck.TruckNumber}", "Truck", "#17A2B8");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling truck selection change");
                await _errorHandlingService.HandleErrorAsync(ex, "خطأ في اختيار الشاحنة");
            }
        }

        /// <summary>
        /// Validates the truck form
        /// </summary>
        private bool ValidateForm()
        {
            ValidationErrors.Clear();

            if (string.IsNullOrWhiteSpace(TruckNumber))
            {
                ValidationErrors.Add("رقم الشاحنة مطلوب");
            }
            else if (TruckNumber.Trim().Length < 2)
            {
                ValidationErrors.Add("رقم الشاحنة يجب أن يكون على الأقل حرفين");
            }

            if (string.IsNullOrWhiteSpace(DriverName))
            {
                ValidationErrors.Add("اسم السائق مطلوب");
            }
            else if (DriverName.Trim().Length < 2)
            {
                ValidationErrors.Add("اسم السائق يجب أن يكون على الأقل حرفين");
            }

            HasValidationErrors = ValidationErrors.Any();
            return !HasValidationErrors;
        }

        /// <summary>
        /// Clears the truck form
        /// </summary>
        private void ClearForm()
        {
            TruckNumber = string.Empty;
            DriverName = string.Empty;
            IsActive = true;
            ValidationErrors.Clear();
            HasValidationErrors = false;
        }

        /// <summary>
        /// Updates the status message with icon and color
        /// </summary>
        private void UpdateStatus(string message, string icon, string color)
        {
            StatusMessage = message;
            StatusIcon = icon;
            StatusColor = color;
        }

        /// <summary>
        /// Cleanup method for proper resource disposal
        /// </summary>
        public void Cleanup()
        {
            try
            {
                Trucks.Clear();
                TruckLoads.Clear();
                ValidationErrors.Clear();

                _logger.LogDebug("TruckManagementViewModel cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during TruckManagementViewModel cleanup");
            }
        }

        #endregion
    }
}