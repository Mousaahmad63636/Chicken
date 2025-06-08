using PoultrySlaughterPOS.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoultrySlaughterPOS.Services
{
    /// <summary>
    /// Interface for printing services providing document printing capabilities
    /// for invoices, receipts, and reports in the POS system
    /// </summary>
    public interface IPrintingService
    {
        /// <summary>
        /// Prints an invoice document
        /// </summary>
        /// <param name="invoice">Invoice to print</param>
        /// <returns>Task representing the print operation</returns>
        Task PrintInvoiceAsync(Invoice invoice);

        /// <summary>
        /// Prints a payment receipt
        /// </summary>
        /// <param name="payment">Payment to print receipt for</param>
        /// <returns>Task representing the print operation</returns>
        Task PrintReceiptAsync(Payment payment);

        /// <summary>
        /// Prints a customer account statement
        /// </summary>
        /// <param name="customer">Customer for the statement</param>
        /// <param name="invoices">Customer invoices</param>
        /// <param name="payments">Customer payments</param>
        /// <param name="startDate">Statement start date</param>
        /// <param name="endDate">Statement end date</param>
        /// <returns>Task representing the print operation</returns>
        Task PrintAccountStatementAsync(Customer customer, IEnumerable<Invoice> invoices, IEnumerable<Payment> payments, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Prints a transaction history report
        /// </summary>
        /// <param name="customer">Customer for the report</param>
        /// <param name="transactions">Transaction data</param>
        /// <param name="startDate">Report start date</param>
        /// <param name="endDate">Report end date</param>
        /// <returns>Task representing the print operation</returns>
        Task PrintTransactionHistoryAsync(Customer customer, IEnumerable<Invoice> transactions, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets the list of available printers
        /// </summary>
        /// <returns>List of available printer names</returns>
        IEnumerable<string> GetAvailablePrinters();

        /// <summary>
        /// Sets the default printer for the application
        /// </summary>
        /// <param name="printerName">Name of the printer to set as default</param>
        /// <returns>True if successfully set, false otherwise</returns>
        bool SetDefaultPrinter(string printerName);
    }
}