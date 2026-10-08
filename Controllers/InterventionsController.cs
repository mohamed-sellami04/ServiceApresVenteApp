using Microsoft.AspNetCore.Mvc;
using ServiceApresVenteApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceApresVenteApp.Controllers
{
    // Contrôleur pour la gestion des interventions techniques
    public class InterventionsController : Controller
    {
        private readonly ApplicationDbContext _context; // Contexte de la base de données

        // Constructeur avec injection de dépendance
        public InterventionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Liste toutes les interventions (vue admin)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Vérification des droits admin
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Récupération des interventions avec leurs relations
            var interventions = await _context.Interventions
                .Include(i => i.Complaint) // Inclusion de la réclamation
                .ThenInclude(c => c.Client) // Inclusion du client
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Article) // Inclusion de l'article
                .ToListAsync();

            return View(interventions);
        }

        // GET: Affiche le formulaire de création d'intervention
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Vérification des droits admin
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Préparation de la liste des réclamations pour le dropdown
            var complaints = await _context.Complaints
                .Include(c => c.Client)
                .Include(c => c.Article)
                .Select(c => new
                {
                    c.Id,
                    Display = $"Complaint #{c.Id} - {c.Client.Name} - {c.Article.Name}",
                    IsUnderWarranty = c.Article.IsUnderWarranty() // Vérification de la garantie
                })
                .ToListAsync();

            ViewBag.Complaints = complaints; // Passage à la vue
            return View();
        }

        // POST: Traite la création d'une intervention
        [HttpPost]
        public async Task<IActionResult> Create(Intervention intervention)
        {
            // Vérification des droits admin
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Validation de la réclamation sélectionnée
            if (intervention.ComplaintId <= 0)
            {
                ModelState.AddModelError("ComplaintId", "Please select a valid complaint.");
            }
            else
            {
                // Nettoyage des erreurs de validation si valide
                ModelState.Remove("ComplaintId");
                ModelState.Remove("Complaint");

                // Vérification du statut de garantie
                var complaint = await _context.Complaints
                    .Include(c => c.Article)
                    .FirstOrDefaultAsync(c => c.Id == intervention.ComplaintId);

                if (complaint != null)
                {
                    bool isUnderWarranty = complaint.Article.IsUnderWarranty();
                    intervention.IsFree = isUnderWarranty;

                    // Si sous garantie, coût main d'œuvre à 0
                    if (isUnderWarranty)
                    {
                        intervention.LaborCost = 0;
                    }
                }
            }

            // Si le modèle est valide
            if (ModelState.IsValid)
            {
                try
                {
                    // Création de l'intervention
                    _context.Interventions.Add(intervention);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Intervention created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to create intervention: {ex.Message}");
                }
            }

            // Rechargement des réclamations en cas d'erreur
            var complaints = await _context.Complaints
                .Include(c => c.Client)
                .Include(c => c.Article)
                .Select(c => new
                {
                    c.Id,
                    Display = $"Complaint #{c.Id} - {c.Client.Name} - {c.Article.Name}",
                    IsUnderWarranty = c.Article.IsUnderWarranty()
                })
                .ToListAsync();

            ViewBag.Complaints = complaints;
            return View(intervention);
        }

        // GET: Liste des interventions pour un client
        [HttpGet]
        public async Task<IActionResult> ClientInterventions()
        {
            // Vérification de l'authentification
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Clients");
            }

            // Récupération de l'ID client
            var clientId = int.Parse(HttpContext.Session.GetString("UserId"));

            // Récupération des interventions du client
            var interventions = await _context.Interventions
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Client)
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Article)
                .Include(i => i.SpareParts) // Inclusion des pièces détachées
                .Where(i => i.Complaint.ClientId == clientId) // Filtrage par client
                .ToListAsync();

            return View(interventions);
        }

        // GET: Affiche la confirmation de suppression
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            // Vérification des droits admin
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Recherche de l'intervention à supprimer
            var intervention = await _context.Interventions
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Client)
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Article)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (intervention == null)
            {
                return NotFound();
            }

            return View(intervention);
        }

        // POST: Confirme la suppression
        [HttpPost, ActionName("Delete")]
   
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Vérification des droits admin
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Recherche de l'intervention avec ses pièces détachées
            var intervention = await _context.Interventions
                .Include(i => i.SpareParts)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (intervention == null)
            {
                return NotFound();
            }

            try
            {
                // Suppression en cascade des pièces détachées
                _context.SpareParts.RemoveRange(intervention.SpareParts);

                // Suppression de l'intervention
                _context.Interventions.Remove(intervention);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Intervention and related spare parts deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to delete intervention: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Affiche le formulaire d'édition
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Vérification des droits admin
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Recherche de l'intervention à éditer
            var intervention = await _context.Interventions
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Client)
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Article)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (intervention == null)
            {
                return NotFound();
            }

            return View(intervention);
        }

        // POST: Traite la modification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Intervention intervention)
        {
            // Vérification des droits admin
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Clients");
            }

            // Vérification de la cohérence des IDs
            if (id != intervention.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Récupération de l'intervention existante
                    var existingIntervention = await _context.Interventions
                        .Include(i => i.Complaint)
                        .FirstOrDefaultAsync(i => i.Id == id);

                    if (existingIntervention == null)
                    {
                        return NotFound();
                    }

                    // Mise à jour des champs modifiables
                    existingIntervention.Description = intervention.Description;
                    existingIntervention.InterventionDate = intervention.InterventionDate;
                    existingIntervention.LaborCost = intervention.LaborCost;
                    existingIntervention.IsFree = intervention.IsFree;

                    _context.Update(existingIntervention);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Intervention updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InterventionExists(intervention.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Rechargement en cas d'erreur
            var updatedIntervention = await _context.Interventions
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Client)
                .Include(i => i.Complaint)
                .ThenInclude(c => c.Article)
                .FirstOrDefaultAsync(i => i.Id == id);

            return View(updatedIntervention);
        }

        // Vérifie l'existence d'une intervention
        private bool InterventionExists(int id)
        {
            return _context.Interventions.Any(e => e.Id == id);
        }
    }
}