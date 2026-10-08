using System.ComponentModel.DataAnnotations;

namespace ServiceApresVenteApp.Models
{
    public class Client
    {
        internal string Username;
        internal object PasswordHash;

        public int Id { get; set; }

        [Required(ErrorMessage = "The Name field is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "The Email field is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The Password field is required.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "The Role field is required.")]
        public string Role { get; set; } // "Client" or "Admin"

        public List<Complaint> Complaints { get; set; } = new List<Complaint>();
    }
}