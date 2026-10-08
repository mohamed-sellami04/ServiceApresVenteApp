using System.ComponentModel.DataAnnotations;

namespace ServiceApresVenteApp.Models
{
    public class Complaint
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The Description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "The Client is required.")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "The Article is required.")]
        public int ArticleId { get; set; }

        [Required(ErrorMessage = "The Submission Date is required.")]
        [DataType(DataType.Date)]
        public DateTime SubmissionDate { get; set; }

        public Client Client { get; set; }
        public Article Article { get; set; }
        public List<Intervention> Interventions { get; set; } = new List<Intervention>();
    }
}