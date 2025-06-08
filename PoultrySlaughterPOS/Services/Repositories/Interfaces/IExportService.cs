using PoultrySlaughterPOS.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoultrySlaughterPOS.Services
{
    /// <summary>
    /// Interface for export services providing document generation capabilities
    /// for transaction reports, invoices, and customer data in multiple formats
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Exports transaction data to PDF format
        /// </summary>
        /// <param name="transactions">Transaction data to export</param>
        /// <param name="customer">Customer information</param>
        /// <param name="startDate">Report start date</param>
        /// <param name="endDate">Report end date</param>
        /// <param name="fileName">Output file name without extension</param>
        /// <returns>Task representing the export operation</returns>
        Task ExportTransactionsToPdfAsync(IEnumerable<Invoice> transactions, Customer customer, DateTime startDate, DateTime endDate, string fileName);

        /// <summary>
        /// Exports transaction data to Excel format
        /// </summary>
        /// <param name="transactions">Transaction data to export</param>
        /// <param name="customer">Customer information</param>
        /// <param name="startDate">Report start date</param>
        /// <param name="endDate">Report end date</param>
        /// <param name="fileName">Output file name without extension</param>
        /// <returns>Task representing the export operation</returns>
        Task ExportTransactionsToExcelAsync(IEnumerable<Invoice> transactions, Customer customer, DateTime startDate, DateTime endDate, string fileName);

        /// <summary>
        /// Exports customer account statement to PDF
        /// </summary>
        /// <param name="customer">Customer information</param>
        /// <param name="invoices">Customer invoices</param>
        /// <param name="payments">Customer payments</param>
        /// <param name="startDate">Statement start date</param>
        /// <param name="endDate">Statement end date</param>
        /// <param name="fileName">Output file name without extension</param>
        /// <returns>Task representing the export operation</returns>
        Task ExportAccountStatementToPdfAsync(Customer customer, IEnumerable<Invoice> invoices, IEnumerable<Payment> payments, DateTime startDate, DateTime endDate, string fileName);

        /// <summary>
        /// Exports invoice to PDF format
        /// </summary>
        /// <param name="invoice">Invoice to export</param>
        /// <param name="fileName">Output file name without extension</param>
        /// <returns>Task representing the export operation</returns>
        Task ExportInvoiceToPdfAsync(Invoice invoice, string fileName);
    }
}