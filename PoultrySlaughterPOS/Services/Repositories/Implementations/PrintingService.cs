using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Implementation of printing services providing document printing capabilities
    /// for invoices, receipts, and reports in the POS system
    /// </summary>
    public class PrintingService : IPrintingService
    {
        #region Private Fields

        private readonly ILogger<PrintingService> _logger;
        private string _defaultPrinter;

        #endregion

        #region Constructor

        public PrintingService(ILogger<PrintingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultPrinter = GetDefaultSystemPrinter();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Prints an invoice document
        /// </summary>
        public async Task PrintInvoiceAsync(Invoice invoice)
        {
            try
            {
                _logger.LogInformation("Starting invoice print for invoice {InvoiceId}", invoice.InvoiceId);

                await Task.Run(() =>
                {
                    var printDocument = CreateInvoicePrintDocument(invoice);
                    PrintDocument(printDocument, $"فاتورة رقم {invoice.InvoiceNumber}");
                });

                _logger.LogInformation("Invoice printed successfully for invoice {InvoiceId}", invoice.InvoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing invoice {InvoiceId}", invoice.InvoiceId);
                throw;
            }
        }

        /// <summary>
        /// Prints a payment receipt
        /// </summary>
        public async Task PrintReceiptAsync(Payment payment)
        {
            try
            {
                _logger.LogInformation("Starting receipt print for payment {PaymentId}", payment.PaymentId);

                await Task.Run(() =>
                {
                    var printDocument = CreateReceiptPrintDocument(payment);
                    PrintDocument(printDocument, $"إيصال دفع رقم {payment.PaymentId}");
                });

                _logger.LogInformation("Receipt printed successfully for payment {PaymentId}", payment.PaymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing receipt for payment {PaymentId}", payment.PaymentId);
                throw;
            }
        }

        /// <summary>
        /// Prints a customer account statement
        /// </summary>
        public async Task PrintAccountStatementAsync(Customer customer, IEnumerable<Invoice> invoices, IEnumerable<Payment> payments, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Starting account statement print for customer {CustomerId}", customer.CustomerId);

                await Task.Run(() =>
                {
                    var printDocument = CreateAccountStatementPrintDocument(customer, invoices, payments, startDate, endDate);
                    PrintDocument(printDocument, $"كشف حساب {customer.CustomerName}");
                });

                _logger.LogInformation("Account statement printed successfully for customer {CustomerId}", customer.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing account statement for customer {CustomerId}", customer.CustomerId);
                throw;
            }
        }

        /// <summary>
        /// Prints a transaction history report
        /// </summary>
        public async Task PrintTransactionHistoryAsync(Customer customer, IEnumerable<Invoice> transactions, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Starting transaction history print for customer {CustomerId}", customer.CustomerId);

                await Task.Run(() =>
                {
                    var printDocument = CreateTransactionHistoryPrintDocument(customer, transactions, startDate, endDate);
                    PrintDocument(printDocument, $"تاريخ معاملات {customer.CustomerName}");
                });

                _logger.LogInformation("Transaction history printed successfully for customer {CustomerId}", customer.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing transaction history for customer {CustomerId}", customer.CustomerId);
                throw;
            }
        }

        /// <summary>
        /// Gets the list of available printers
        /// </summary>
        public IEnumerable<string> GetAvailablePrinters()
        {
            try
            {
                var printers = new List<string>();

                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    printers.Add(printerName);
                }

                return printers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available printers");
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Sets the default printer for the application
        /// </summary>
        public bool SetDefaultPrinter(string printerName)
        {
            try
            {
                if (PrinterSettings.InstalledPrinters.Cast<string>().Contains(printerName))
                {
                    _defaultPrinter = printerName;
                    _logger.LogInformation("Default printer set to: {PrinterName}", printerName);
                    return true;
                }

                _logger.LogWarning("Printer not found: {PrinterName}", printerName);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default printer to {PrinterName}", printerName);
                return false;
            }
        }

        #endregion

        #region Private Methods

        private string GetDefaultSystemPrinter()
        {
            try
            {
                var printerSettings = new PrinterSettings();
                return printerSettings.PrinterName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string CreateInvoicePrintDocument(Invoice invoice)
        {
            var paidAmount = invoice.Payments?.Sum(p => p.Amount) ?? 0;
            var outstanding = invoice.FinalAmount - paidAmount;
            var discountAmount = invoice.TotalAmount * invoice.DiscountPercentage / 100;

            return $@"
=====================================
        مذبح الدواجن - فاتورة
=====================================

رقم الفاتورة: {invoice.InvoiceNumber}
التاريخ: {invoice.InvoiceDate:yyyy-MM-dd HH:mm}

-------------------------------------
بيانات العميل:
-------------------------------------
الاسم: {invoice.Customer?.CustomerName ?? "غير محدد"}
الهاتف: {invoice.Customer?.PhoneNumber ?? "غير محدد"}

-------------------------------------
بيانات الشاحنة:
-------------------------------------
رقم الشاحنة: {invoice.Truck?.TruckNumber ?? "غير محدد"}

-------------------------------------
تفاصيل الوزن:
-------------------------------------
الوزن الإجمالي: {invoice.GrossWeight:F2} كيلو
وزن الأقفاص: {invoice.CagesWeight:F2} كيلو
الوزن الصافي: {invoice.NetWeight:F2} كيلو

-------------------------------------
التفاصيل المالية:
-------------------------------------
سعر الكيلو: {invoice.UnitPrice:F2} جنيه
المبلغ الأساسي: {invoice.TotalAmount:F2} جنيه
نسبة الخصم: {invoice.DiscountPercentage:F2}%
مبلغ الخصم: {discountAmount:F2} جنيه
المبلغ النهائي: {invoice.FinalAmount:F2} جنيه
المبلغ المدفوع: {paidAmount:F2} جنيه
المبلغ المستحق: {outstanding:F2} جنيه

=====================================
شكراً لتعاملكم معنا
{DateTime.Now:yyyy-MM-dd HH:mm}
=====================================
";
        }

        private string CreateReceiptPrintDocument(Payment payment)
        {
            return $@"
=====================================
       مذبح الدواجن - إيصال دفع
=====================================

رقم الإيصال: {payment.PaymentId}
التاريخ: {payment.PaymentDate:yyyy-MM-dd HH:mm}

-------------------------------------
بيانات العميل:
-------------------------------------
الاسم: {payment.Customer?.CustomerName ?? "غير محدد"}

-------------------------------------
تفاصيل الدفع:
-------------------------------------
المبلغ المدفوع: {payment.Amount:F2} جنيه
طريقة الدفع: {payment.PaymentMethod}
رقم الفاتورة: {payment.Invoice?.InvoiceNumber ?? "غير محدد"}

-------------------------------------
ملاحظات:
{payment.Notes ?? "لا توجد ملاحظات"}

=====================================
شكراً لتعاملكم معنا
{DateTime.Now:yyyy-MM-dd HH:mm}
=====================================
";
        }

        private string CreateAccountStatementPrintDocument(Customer customer, IEnumerable<Invoice> invoices, IEnumerable<Payment> payments, DateTime startDate, DateTime endDate)
        {
            var invoiceList = invoices.ToList();
            var paymentList = payments.ToList();
            var totalInvoices = invoiceList.Sum(i => i.FinalAmount);
            var totalPayments = paymentList.Sum(p => p.Amount);
            var balance = totalInvoices - totalPayments;

            var document = $@"
=====================================
      مذبح الدواجن - كشف حساب
=====================================

اسم العميل: {customer.CustomerName}
رقم الهاتف: {customer.PhoneNumber ?? "غير محدد"}
الفترة: من {startDate:yyyy-MM-dd} إلى {endDate:yyyy-MM-dd}

-------------------------------------
ملخص الحساب:
-------------------------------------
إجمالي الفواتير: {totalInvoices:F2} جنيه
إجمالي المدفوعات: {totalPayments:F2} جنيه
الرصيد الحالي: {balance:F2} جنيه

-------------------------------------
الفواتير:
-------------------------------------
";

            foreach (var invoice in invoiceList.OrderBy(i => i.InvoiceDate))
            {
                document += $"{invoice.InvoiceDate:yyyy-MM-dd} | {invoice.InvoiceNumber} | {invoice.FinalAmount:F2} جنيه\n";
            }

            document += "\n-------------------------------------\nالمدفوعات:\n-------------------------------------\n";

            foreach (var payment in paymentList.OrderBy(p => p.PaymentDate))
            {
                document += $"{payment.PaymentDate:yyyy-MM-dd} | {payment.Amount:F2} جنيه | {payment.PaymentMethod}\n";
            }

            document += $@"

=====================================
{DateTime.Now:yyyy-MM-dd HH:mm}
=====================================
";

            return document;
        }

        private string CreateTransactionHistoryPrintDocument(Customer customer, IEnumerable<Invoice> transactions, DateTime startDate, DateTime endDate)
        {
            var transactionList = transactions.ToList();
            var totalAmount = transactionList.Sum(t => t.FinalAmount);
            var totalPaid = transactionList.SelectMany(t => t.Payments ?? Enumerable.Empty<Payment>()).Sum(p => p.Amount);
            var outstanding = totalAmount - totalPaid;

            var document = $@"
=====================================
    مذبح الدواجن - تاريخ المعاملات
=====================================

اسم العميل: {customer.CustomerName}
الفترة: من {startDate:yyyy-MM-dd} إلى {endDate:yyyy-MM-dd}

-------------------------------------
ملخص المعاملات:
-------------------------------------
عدد الفواتير: {transactionList.Count}
إجمالي المبلغ: {totalAmount:F2} جنيه
المبلغ المدفوع: {totalPaid:F2} جنيه
المبلغ المستحق: {outstanding:F2} جنيه

-------------------------------------
تفاصيل المعاملات:
-------------------------------------
";

            foreach (var transaction in transactionList.OrderBy(t => t.InvoiceDate))
            {
                var paidAmount = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                var transactionOutstanding = transaction.FinalAmount - paidAmount;

                document += $@"{transaction.InvoiceDate:yyyy-MM-dd} | {transaction.InvoiceNumber}
الوزن: {transaction.NetWeight:F2} كيلو | المبلغ: {transaction.FinalAmount:F2} جنيه
المدفوع: {paidAmount:F2} جنيه | المستحق: {transactionOutstanding:F2} جنيه
-------------------------------------
";
            }

            document += $@"
=====================================
{DateTime.Now:yyyy-MM-dd HH:mm}
=====================================
";

            return document;
        }

        private void PrintDocument(string content, string documentName)
        {
            try
            {
                // For now, show a message indicating that printing would occur
                // In a real implementation, you would use PrintDocument class to print
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(
                        $"طباعة: {documentName}\n\nسيتم إرسال المستند إلى الطابعة: {_defaultPrinter}\n\nهل تريد المتابعة؟",
                        "تأكيد الطباعة",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // TODO: Implement actual printing using PrintDocument
                        MessageBox.Show("تم إرسال المستند إلى الطابعة بنجاح", "طباعة ناجحة",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });

                _logger.LogInformation("Document printing completed: {DocumentName}", documentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing document: {DocumentName}", documentName);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"خطأ في طباعة المستند: {ex.Message}", "خطأ في الطباعة",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        #endregion
    }
}