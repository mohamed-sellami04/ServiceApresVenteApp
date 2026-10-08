using Microsoft.AspNetCore.Mvc;
using ServiceApresVenteApp.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ServiceApresVenteApp.Controllers
{
    // Contrôleur pour la gestion des réclamations (complaints)
    public class ComplaintsController : Controller
    {
        private readonly ApplicationDbContext _context; // Contexte de la base de données
        private readonly ILogger<ComplaintsController> _logger; // Logger pour le suivi des activités

        // Constructeur avec injection de dépendances
        public ComplaintsController(ApplicationDbContext context, ILogger<ComplaintsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Affiche le formulaire de création d'une réclamation
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Vérification de l'authentification
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Clients");
            }

            // Préparation du modèle de vue avec la liste des articles disponibles
            var model = new ComplaintCreateViewModel
            {
                AvailableArticles = await _context.Articles
                    .Select(a => new ArticleViewModel
                    {
                        Id = a.Id,
                        Name = a.Name ?? "Unknown", // Gestion des noms null
                        WarrantyMonths = a.WarrantyMonths
                    })
                    .ToListAsync()
            };

            // Journalisation du chargement des articles
            _logger.LogInformation("Complaint Create GET: {ArticleCount} articles loaded", model.AvailableArticles.Count);

            return View(model);
        }

        // POST: Traite la soumission du formulaire de réclamation
        [HttpPost]
        public async Task<IActionResult> Create(ComplaintCreateViewModel model)
        {
            // Vérification de l'authentification
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Clients");
            }

            // Récupération de l'ID du client depuis la session
            var clientId = int.Parse(HttpContext.Session.GetString("UserId"));

            // Journalisation des données reçues
            _logger.LogInformation("Complaint Create POST: Description={Description}, SelectedArticleId={SelectedArticleId}",
                model.ComplaintDescription, model.SelectedArticleId?.ToString() ?? "null");

            // Suppression de la validation pour AvailableArticles car ce n'est pas un champ de formulaire
            ModelState.Remove("AvailableArticles");

            // Validation manuelle de l'article sélectionné
            if (!model.SelectedArticleId.HasValue || model.SelectedArticleId <= 0)
            {
                ModelState.AddModelError("SelectedArticleId", "Please select a valid article.");
            }

            // Si le modèle est valide
            if (ModelState.IsValid)
            {
                try
                {
                    // Création de la nouvelle réclamation
                    var complaint = new Complaint
                    {
                        Description = model.ComplaintDescription,
                        ClientId = clientId,
                        ArticleId = model.SelectedArticleId.Value,
                        SubmissionDate = DateTime.Now // Date automatique
                    };

                    // Ajout et sauvegarde en base
                    _context.Complaints.Add(complaint);
                    await _context.SaveChangesAsync();

                    // Journalisation et message de succès
                    _logger.LogInformation("Complaint created successfully: Id={Id}", complaint.Id);
                    TempData["SuccessMessage"] = "Complaint submitted successfully!";
                    return RedirectToAction("Create");
                }
                catch (Exception ex)
                {
                    // Gestion des erreurs
                    _logger.LogError(ex, "Failed to create complaint.");
                    ModelState.AddModelError("", $"Failed to create complaint: {ex.Message}");
                }
            }

            // Rechargement des articles disponibles en cas d'erreur
            model.AvailableArticles = await _context.Articles
                .Select(a => new ArticleViewModel
                {
                    Id = a.Id,
                    Name = a.Name ?? "Unknown",
                    WarrantyMonths = a.WarrantyMonths
                })
                .ToListAsync();

            return View(model);
        }

        // GET: Affiche la liste des réclamations du client
        [HttpGet]
        public async Task<IActionResult> ClientComplaints()
        {
            // Vérification de l'authentification
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Clients");
            }

            // Récupération de l'ID du client
            var clientId = int.Parse(HttpContext.Session.GetString("UserId"));

            // Récupération des réclamations avec leurs relations
            var complaints = await _context.Complaints
                .Include(c => c.Article) // Inclusion des données de l'article
                .Include(c => c.Interventions) // Inclusion des interventions
                .Where(c => c.ClientId == clientId) // Filtrage par client
                .OrderByDescending(c => c.SubmissionDate) // Tri par date décroissante
                .ToListAsync();

            return View(complaints);
        }
    }

    // Modèle de vue pour la création de réclamation
    public class ComplaintCreateViewModel
    {
        [Required(ErrorMessage = "The Complaint Description is required.")]
        public string ComplaintDescription { get; set; } // Description obligatoire

        [Required(ErrorMessage = "Please select a valid article.")]
        public int? SelectedArticleId { get; set; } // ID d'article obligatoire

        public List<ArticleViewModel> AvailableArticles { get; set; } = new List<ArticleViewModel>(); // Liste des articles disponibles
    }

    // Modèle de vue pour afficher un article
    public class ArticleViewModel
    {
        public int Id { get; set; } // Identifiant de l'article
        public string Name { get; set; } // Nom de l'article
        public int WarrantyMonths { get; set; } // Durée de garantie en mois
    }
}