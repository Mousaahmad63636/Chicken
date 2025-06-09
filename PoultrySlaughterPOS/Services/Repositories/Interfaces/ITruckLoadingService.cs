using PoultrySlaughterPOS.Models;

namespace PoultrySlaughterPOS.Services.Repositories.Interfaces
{
    /// <summary>
    /// Interface for truck loading operations
    /// </summary>
    public interface ITruckLoadingService
    {
        Task<IEnumerable<Truck>> GetAvailableTrucksAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<TruckLoad>> GetTodaysTruckLoadsAsync(CancellationToken cancellationToken = default);
        Task<TruckLoad> CreateTruckLoadAsync(TruckLoad truckLoad, CancellationToken cancellationToken = default);
        Task<TruckLoadSummary> GetTruckLoadSummaryAsync(DateTime date, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Simple implementation of truck loading service
    /// </summary>
    public class TruckLoadingService : ITruckLoadingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TruckLoadingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Truck>> GetAvailableTrucksAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Trucks.GetActiveTrucksAsync(cancellationToken);
        }

        public async Task<IEnumerable<TruckLoad>> GetTodaysTruckLoadsAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.TruckLoads.GetTruckLoadsByDateAsync(DateTime.Today, cancellationToken);
        }

        public async Task<TruckLoad> CreateTruckLoadAsync(TruckLoad truckLoad, CancellationToken cancellationToken = default)
        {
            var result = await _unitOfWork.TruckLoads.CreateAsync(truckLoad, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }

        public async Task<TruckLoadSummary> GetTruckLoadSummaryAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            // Implement truck load summary logic
            return new TruckLoadSummary
            {
                Date = date,
                TotalLoads = 0,
                TotalWeight = 0,
                AverageWeight = 0
            };
        }
    }

    /// <summary>
    /// Summary data for truck loads
    /// </summary>
    public class TruckLoadSummary
    {
        public DateTime Date { get; set; }
        public int TotalLoads { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
    }
}