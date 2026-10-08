using System.ComponentModel.DataAnnotations;

namespace ServiceApresVenteApp.Models
{
    public class Article
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "The Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "The Warranty Period is required.")]
        [Range(0, 120, ErrorMessage = "Warranty period must be between 0 and 120 months.")]
        public int WarrantyMonths { get; set; }

        [Required(ErrorMessage = "The Price is required.")]
        [Range(0, 100000, ErrorMessage = "Price must be between 0 and 100,000.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "The Purchase Date is required.")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        // Helper method to check if article is under warranty
        public bool IsUnderWarranty()
        {
            var warrantyEndDate = PurchaseDate.AddMonths(WarrantyMonths);
            return DateTime.Now <= warrantyEndDate;
        }
    }
}
