using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Implementation of export services providing document generation capabilities
    /// for transaction reports, invoices, and customer data in multiple formats
    /// </summary>
    public class ExportService : IExportService
    {
        #region Private Fields

        private readonly ILogger<ExportService> _logger;

        #endregion

        #region Constructor

        public ExportService(ILogger<ExportService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Exports transaction data to PDF format
        /// </summary>
        public async Task ExportTransactionsToPdfAsync(IEnumerable<Invoice> transactions, Customer customer, DateTime startDate, DateTime endDate, string fileName)
        {
            try
            {
                _logger.LogInformation("Starting PDF export for customer {CustomerId} transactions", customer.CustomerId);

                await Task.Run(() =>
                {
                    var filePath = GetExportFilePath(fileName, "pdf");

                    // TODO: Implement actual PDF generation using a library like iTextSharp or PdfSharp
                    // For now, create a placeholder file with transaction summary
                    var summary = CreateTransactionSummary(transactions, customer, startDate, endDate);
                    File.WriteAllText(filePath, summary);

                    _logger.LogInformation("PDF export completed successfully: {FilePath}", filePath);

                    // Show success message to user
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"تم تصدير التقرير بنجاح إلى:\n{filePath}", "تصدير ناجح",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting transactions to PDF");
                throw;
            }
        }

        /// <summary>
        /// Exports transaction data to Excel format
        /// </summary>
        public async Task ExportTransactionsToExcelAsync(IEnumerable<Invoice> transactions, Customer customer, DateTime startDate, DateTime endDate, string fileName)
        {
            try
            {
                _logger.LogInformation("Starting Excel export for customer {CustomerId} transactions", customer.CustomerId);

                await Task.Run(() =>
                {
                    var filePath = GetExportFilePath(fileName, "xlsx");

                    // TODO: Implement actual Excel generation using a library like EPPlus or ClosedXML
                    // For now, create a CSV file with transaction data
                    var csvData = CreateTransactionCsv(transactions, customer, startDate, endDate);
                    File.WriteAllText(filePath.Replace(".xlsx", ".csv"), csvData);

                    _logger.LogInformation("Excel export completed successfully: {FilePath}", filePath);

                    // Show success message to user
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"تم تصدير التقرير بنجاح إلى:\n{filePath.Replace(".xlsx", ".csv")}", "تصدير ناجح",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting transactions to Excel");
                throw;
            }
        }

        /// <summary>
        /// Exports customer account statement to PDF
        /// </summary>
        public async Task ExportAccountStatementToPdfAsync(Customer customer, IEnumerable<Invoice> invoices, IEnumerable<Payment> payments, DateTime startDate, DateTime endDate, string fileName)
        {
            try
            {
                _logger.LogInformation("Starting account statement PDF export for customer {CustomerId}", customer.CustomerId);

                await Task.Run(() =>
                {
                    var filePath = GetExportFilePath($"{fileName}_كشف_حساب", "pdf");

                    // TODO: Implement actual PDF generation
                    var statement = CreateAccountStatement(customer, invoices, payments, startDate, endDate);
                    File.WriteAllText(filePath, statement);

                    _logger.LogInformation("Account statement export completed successfully: {FilePath}", filePath);

                    // Show success message to user
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"تم تصدير كشف الحساب بنجاح إلى:\n{filePath}", "تصدير ناجح",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting account statement to PDF");
                throw;
            }
        }

        /// <summary>
        /// Exports invoice to PDF format
        /// </summary>
        public async Task ExportInvoiceToPdfAsync(Invoice invoice, string fileName)
        {
            try
            {
                _logger.LogInformation("Starting invoice PDF export for invoice {InvoiceId}", invoice.InvoiceId);

                await Task.Run(() =>
                {
                    var filePath = GetExportFilePath($"{fileName}_فاتورة_{invoice.InvoiceNumber}", "pdf");

                    // TODO: Implement actual PDF generation
                    var invoiceData = CreateInvoiceDocument(invoice);
                    File.WriteAllText(filePath, invoiceData);

                    _logger.LogInformation("Invoice export completed successfully: {FilePath}", filePath);

                    // Show success message to user
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"تم تصدير الفاتورة بنجاح إلى:\n{filePath}", "تصدير ناجح",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting invoice to PDF");
                throw;
            }
        }

        #endregion

        #region Private Methods

        private string GetExportFilePath(string fileName, string extension)
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var exportDir = Path.Combine(documentsPath, "PoultrySlaughterPOS", "Exports");

            if (!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);

            return Path.Combine(exportDir, $"{fileName}.{extension}");
        }

        private string CreateTransactionSummary(IEnumerable<Invoice> transactions, Customer customer, DateTime startDate, DateTime endDate)
        {
            var transactionList = transactions.ToList();
            var totalAmount = transactionList.Sum(t => t.FinalAmount);
            var totalPaid = transactionList.SelectMany(t => t.Payments ?? Enumerable.Empty<Payment>()).Sum(p => p.Amount);
            var outstanding = totalAmount - totalPaid;

            return $@"تقرير معاملات العميل
=================

اسم العميل: {customer.CustomerName}
رقم الهاتف: {customer.PhoneNumber ?? "غير محدد"}
الفترة: من {startDate:yyyy-MM-dd} إلى {endDate:yyyy-MM-dd}

ملخص المعاملات:
عدد الفواتير: {transactionList.Count}
إجمالي المبلغ: {totalAmount:F2} جنيه
المبلغ المدفوع: {totalPaid:F2} جنيه
المبلغ المستحق: {outstanding:F2} جنيه

تفاصيل الفواتير:
{string.Join("\n", transactionList.Select(t =>
    $"فاتورة رقم: {t.InvoiceNumber} - التاريخ: {t.InvoiceDate:yyyy-MM-dd} - المبلغ: {t.FinalAmount:F2} جنيه"))}

تم إنشاء التقرير في: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        private string CreateTransactionCsv(IEnumerable<Invoice> transactions, Customer customer, DateTime startDate, DateTime endDate)
        {
            var csv = "رقم الفاتورة,التاريخ,رقم الشاحنة,الوزن الإجمالي,الوزن الصافي,المبلغ النهائي,المبلغ المدفوع,المبلغ المستحق\n";

            foreach (var transaction in transactions)
            {
                var paidAmount = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                var outstanding = transaction.FinalAmount - paidAmount;

                csv += $"{transaction.InvoiceNumber},{transaction.InvoiceDate:yyyy-MM-dd},{transaction.Truck?.TruckNumber ?? "غير محدد"}," +
                       $"{transaction.GrossWeight},{transaction.NetWeight},{transaction.FinalAmount:F2}," +
                       $"{paidAmount:F2},{outstanding:F2}\n";
            }

            return csv;
        }

        private string CreateAccountStatement(Customer customer, IEnumerable<Invoice> invoices, IEnumerable<Payment> payments, DateTime startDate, DateTime endDate)
        {
            var invoiceList = invoices.ToList();
            var paymentList = payments.ToList();
            var totalInvoices = invoiceList.Sum(i => i.FinalAmount);
            var totalPayments = paymentList.Sum(p => p.Amount);
            var balance = totalInvoices - totalPayments;

            return $@"كشف حساب العميل
================

اسم العميل: {customer.CustomerName}
رقم الهاتف: {customer.PhoneNumber ?? "غير محدد"}
الفترة: من {startDate:yyyy-MM-dd} إلى {endDate:yyyy-MM-dd}

ملخص الحساب:
إجمالي الفواتير: {totalInvoices:F2} جنيه
إجمالي المدفوعات: {totalPayments:F2} جنيه
الرصيد الحالي: {balance:F2} جنيه

الفواتير:
{string.Join("\n", invoiceList.Select(i =>
    $"فاتورة رقم: {i.InvoiceNumber} - التاريخ: {i.InvoiceDate:yyyy-MM-dd} - المبلغ: {i.FinalAmount:F2} جنيه"))}

المدفوعات:
{string.Join("\n", paymentList.Select(p =>
    $"دفعة بتاريخ: {p.PaymentDate:yyyy-MM-dd} - المبلغ: {p.Amount:F2} جنيه - الطريقة: {p.PaymentMethod}"))}

تم إنشاء كشف الحساب في: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        private string CreateInvoiceDocument(Invoice invoice)
        {
            var paidAmount = invoice.Payments?.Sum(p => p.Amount) ?? 0;
            var outstanding = invoice.FinalAmount - paidAmount;

            // Calculate discount amount from percentage
            var discountAmount = invoice.TotalAmount * invoice.DiscountPercentage / 100;

            return $@"فاتورة رقم: {invoice.InvoiceNumber}
========================

تاريخ الفاتورة: {invoice.InvoiceDate:yyyy-MM-dd}
اسم العميل: {invoice.Customer?.CustomerName ?? "غير محدد"}
رقم الشاحنة: {invoice.Truck?.TruckNumber ?? "غير محدد"}

تفاصيل الوزن:
الوزن الإجمالي: {invoice.GrossWeight} كيلو
وزن الأقفاص: {invoice.CagesWeight} كيلو  
الوزن الصافي: {invoice.NetWeight} كيلو

التفاصيل المالية:
سعر الكيلو: {invoice.UnitPrice:F2} جنيه
المبلغ الأساسي: {invoice.TotalAmount:F2} جنيه
نسبة الخصم: {invoice.DiscountPercentage:F2}%
مبلغ الخصم: {discountAmount:F2} جنيه
المبلغ النهائي: {invoice.FinalAmount:F2} جنيه
المبلغ المدفوع: {paidAmount:F2} جنيه
المبلغ المستحق: {outstanding:F2} جنيه

تم إنشاء الفاتورة في: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        #endregion
    }
}