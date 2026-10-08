using Microsoft.AspNetCore.Mvc;
using ServiceApresVenteApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceApresVenteApp.Controllers
{
    // Contrôleur pour gérer les actions administratives
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context; // Contexte de la base de données

        // Constructeur avec injection de dépendance pour le contexte de la base de données
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action pour afficher la liste des réclamations (accessible via GET)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Vérification si l'utilisateur est un administrateur
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients"); // Redirection si non autorisé
            }

            // Récupération des réclamations avec les données associées (Client et Article)
            var complaints = await _context.Complaints
                .Include(c => c.Client) // Inclusion des données du Client
                .Include(c => c.Article) // Inclusion des données de l'Article
                .Select(c => new ComplaintViewModel // Projection vers un modèle de vue
                {
                    Id = c.Id,
                    Description = c.Description ?? "No description", // Valeur par défaut si null
                    ClientName = c.Client != null ? c.Client.Name ?? "Unknown" : "Unknown", // Gestion des valeurs nulles
                    ArticleName = c.Article != null ? c.Article.Name ?? "Unknown" : "Unknown",
                    SubmissionDate = c.SubmissionDate,
                    PurchaseDate = c.Article != null ? c.Article.PurchaseDate : DateTime.MinValue,
                    WarrantyMonths = c.Article != null ? c.Article.WarrantyMonths : 0
                })
                .ToListAsync(); // Exécution asynchrone de la requête

            return View(complaints); // Retourne la vue avec la liste des réclamations
        }

        // Action pour afficher la page de confirmation de suppression (accessible via GET)
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            // Vérification des droits d'administration
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Recherche de la réclamation avec ses relations
            var complaint = await _context.Complaints
                .Include(c => c.Client) // Inclusion du Client
                .Include(c => c.Article) // Inclusion de l'Article
                .Include(c => c.Interventions) // Inclusion des Interventions
                .FirstOrDefaultAsync(c => c.Id == id); // Recherche par ID

            if (complaint == null)
            {
                return NotFound(); // Retourne 404 si non trouvé
            }

            return View(complaint); // Affiche la vue de confirmation
        }

        // Action pour confirmer la suppression (accessible via POST)
        [HttpPost]
        [ActionName("Delete")] // Spécifie le nom de l'action
       
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Vérification des droits d'administration
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            try
            {
                // Recherche de la réclamation avec toutes ses relations
                var complaint = await _context.Complaints
                    .Include(c => c.Interventions) // Inclusion des Interventions
                    .ThenInclude(i => i.SpareParts) // Inclusion des Pièces détachées
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (complaint == null)
                {
                    return NotFound(); // Retourne 404 si non trouvé
                }

                // Suppression en cascade des pièces détachées associées
                foreach (var intervention in complaint.Interventions)
                {
                    _context.SpareParts.RemoveRange(intervention.SpareParts);
                }

                // Suppression des interventions associées
                _context.Interventions.RemoveRange(complaint.Interventions);

                // Suppression de la réclamation
                _context.Complaints.Remove(complaint);
                await _context.SaveChangesAsync(); // Sauvegarde des modifications

                // Message de succès
                TempData["SuccessMessage"] = "Complaint and all related data deleted successfully!";
                return RedirectToAction("Index"); // Redirection vers la liste
            }
            catch (Exception ex)
            {
                // Message d'erreur en cas d'échec
                TempData["ErrorMessage"] = $"Failed to delete complaint: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }

    // Modèle de vue pour afficher les réclamations
    public class ComplaintViewModel
    {
        public int Id { get; set; } // Identifiant de la réclamation
        public string Description { get; set; } // Description du problème
        public string ClientName { get; set; } // Nom du client
        public string ArticleName { get; set; } // Nom de l'article
        public DateTime SubmissionDate { get; set; } // Date de soumission
        public DateTime PurchaseDate { get; set; } // Date d'achat
        public int WarrantyMonths { get; set; } // Durée de garantie en mois
    }
}