using Microsoft.AspNetCore.Mvc;
using ServiceApresVenteApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceApresVenteApp.Controllers
{
    // Contrôleur pour la gestion des pièces détachées (SpareParts)
    public class SparePartsController : Controller
    {
        private readonly ApplicationDbContext _context; // Contexte de la base de données

        // Constructeur avec injection de dépendance
        public SparePartsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Liste toutes les pièces détachées (vue admin)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Vérification des droits admin
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Récupération des pièces détachées avec leurs relations
            var spareParts = await _context.SpareParts
                .Include(s => s.Intervention) // Inclusion de l'intervention
                .ThenInclude(i => i.Complaint) // Inclusion de la réclamation
                .ThenInclude(c => c.Client) // Inclusion du client
                .Include(s => s.Intervention)
                .ThenInclude(i => i.Complaint)
                .ThenInclude(c => c.Article) // Inclusion de l'article
                .ToListAsync();

            return View(spareParts);
        }

        // GET: Affiche le formulaire de création d'une pièce détachée
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Vérification des droits admin
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Préparation de la liste des interventions pour le dropdown
            ViewBag.Interventions = await _context.Interventions
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Client)
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Article)
                .Select(i => new {
                    i.Id,
                    Display = $"Intervention #{i.Id} - Complaint #{i.ComplaintId} - {i.Complaint.Client.Name}"
                })
                .ToListAsync();

            return View();
        }

        // POST: Traite la création d'une pièce détachée
        [HttpPost]
        public async Task<IActionResult> Create(SparePart sparePart)
        {
            // Vérification des droits admin
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Validation de l'intervention sélectionnée
            if (sparePart.InterventionId <= 0)
            {
                ModelState.AddModelError("InterventionId", "Please select a valid intervention.");
            }
            else
            {
                // Nettoyage des erreurs de validation si valide
                ModelState.Remove("InterventionId");
                ModelState.Remove("Intervention");

                // Vérification si l'intervention est gratuite (sous garantie)
                var intervention = await _context.Interventions
                    .Include(i => i.Complaint)
                    .ThenInclude(c => c.Article)
                    .FirstOrDefaultAsync(i => i.Id == sparePart.InterventionId);

                if (intervention != null && intervention.IsFree)
                {
                    // Si sous garantie, prix à 0
                    sparePart.Price = 0;
                }
            }

            // Si le modèle est valide
            if (ModelState.IsValid)
            {
                try
                {
                    // Création de la pièce détachée
                    _context.SpareParts.Add(sparePart);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Spare part created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to create spare part: {ex.Message}");
                }
            }

            // Rechargement des interventions en cas d'erreur
            var interventions = await _context.Interventions
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Client)
                .Select(i => new
                {
                    i.Id,
                    Display = $"Intervention #{i.Id} - {i.Description} - {i.Complaint.Client.Name}",
                    i.IsFree
                })
                .ToListAsync();

            ViewBag.Interventions = interventions;
            return View(sparePart);
        }

        // GET: Liste des pièces détachées pour un client
        [HttpGet]
        public async Task<IActionResult> ClientSpareParts()
        {
            // Vérification de l'authentification
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Clients");
            }

            // Récupération de l'ID client
            var clientId = int.Parse(HttpContext.Session.GetString("UserId"));

            // Récupération des pièces détachées du client
            var spareParts = await _context.SpareParts
                .Include(sp => sp.Intervention)
                .ThenInclude(i => i.Complaint)
                .ThenInclude(c => c.Client)
                .Include(sp => sp.Intervention)
                .ThenInclude(i => i.Complaint)
                .ThenInclude(c => c.Article)
                .Where(sp => sp.Intervention.Complaint.ClientId == clientId) // Filtrage par client
                .ToListAsync();

            return View(spareParts);
        }
    }
}