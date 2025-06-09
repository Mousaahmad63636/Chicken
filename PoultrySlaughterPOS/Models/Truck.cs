using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoultrySlaughterPOS.Models
{
    public class Truck
    {
        public int TruckId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TruckNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DriverName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
