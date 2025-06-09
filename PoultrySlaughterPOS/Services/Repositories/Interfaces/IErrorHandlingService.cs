using Microsoft.Extensions.Logging;

namespace PoultrySlaughterPOS.Services.Repositories.Interfaces
{
    /// <summary>
    /// Interface for centralized error handling throughout the application
    /// </summary>
    public interface IErrorHandlingService
    {
        void LogError(Exception exception, string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogInformation(string message, params object[] args);
        string FormatUserFriendlyError(Exception exception);
        bool ShouldRetryOperation(Exception exception);
    }

    /// <summary>
    /// Simple implementation of error handling service
    /// </summary>
    public class SimpleErrorHandlingService : IErrorHandlingService
    {
        public void LogError(Exception exception, string message, params object[] args)
        {
            // Simple console logging - you can enhance this with proper logging
            Console.WriteLine($"ERROR: {string.Format(message, args)} - {exception.Message}");
        }

        public void LogWarning(string message, params object[] args)
        {
            Console.WriteLine($"WARNING: {string.Format(message, args)}");
        }

        public void LogInformation(string message, params object[] args)
        {
            Console.WriteLine($"INFO: {string.Format(message, args)}");
        }

        public string FormatUserFriendlyError(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => "خطأ في البيانات المطلوبة",
                InvalidOperationException => "عملية غير صالحة",
                UnauthorizedAccessException => "غير مصرح بالوصول",
                TimeoutException => "انتهت مهلة العملية",
                _ => "حدث خطأ غير متوقع"
            };
        }

        public bool ShouldRetryOperation(Exception exception)
        {
            return exception is TimeoutException or
                   System.Data.Common.DbException or
                   System.Net.NetworkInformation.NetworkInformationException;
        }
    }
}