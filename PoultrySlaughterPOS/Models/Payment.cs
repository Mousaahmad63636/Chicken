using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoultrySlaughterPOS.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int CustomerId { get; set; }
        public int? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        [MaxLength(200)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual Invoice? Invoice { get; set; }
    }
}