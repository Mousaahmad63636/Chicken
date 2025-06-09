using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoultrySlaughterPOS.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }

        [Required]
        [MaxLength(20)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public int TruckId { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        public decimal GrossWeight { get; set; }
        public decimal CagesWeight { get; set; }
        public int CagesCount { get; set; }
        public decimal NetWeight { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal PreviousBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual Truck Truck { get; set; } = null!;
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}