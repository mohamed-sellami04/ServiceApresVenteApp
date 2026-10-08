using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApresVenteApp.Models;

namespace ServiceApresVenteApp.Controllers
{
    // Contrôleur pour la gestion des articles
    public class ArticlesController : Controller
    {
        private readonly ApplicationDbContext _context; // Contexte de la base de données
        private readonly ILogger<ArticlesController> _logger; // Logger pour le suivi des activités

        // Constructeur avec injection de dépendances
        public ArticlesController(ApplicationDbContext context, ILogger<ArticlesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Affiche la liste des articles
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Vérification des droits d'administration
                if (HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    return RedirectToAction("Login", "Clients");
                }

                // Récupération de tous les articles
                var articles = await _context.Articles.ToListAsync();
                return View(articles);
            }
            catch (Exception ex)
            {
                // Journalisation des erreurs
                _logger.LogError(ex, "Error in Articles Index");
                return RedirectToAction("Error", "Home", new { message = "Failed to load articles. Please try again later." });
            }
        }

        // GET: Affiche le formulaire de création d'un article
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                // Vérification des droits d'administration
                if (HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    return RedirectToAction("Login", "Clients");
                }
                return View(new Article()); // Retourne une vue avec un nouvel article vide
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Articles Create GET");
                return RedirectToAction("Error", "Home", new { message = "Failed to load article creation page. Please try again later." });
            }
        }

        // POST: Traite la soumission du formulaire de création
        [HttpPost]
     
        public async Task<IActionResult> Create([Bind("Name,Description,WarrantyMonths,Price,PurchaseDate")] Article article)
        {
            try
            {
                // Vérification des droits d'administration
                if (HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    return RedirectToAction("Login", "Clients");
                }

                // Journalisation des données reçues
                _logger.LogInformation("Article Create POST: Name={Name}, Description={Description}, WarrantyMonths={WarrantyMonths}, Price={Price}, PurchaseDate={PurchaseDate}",
                    article.Name, article.Description, article.WarrantyMonths, article.Price, article.PurchaseDate);

                // Validation du modèle
                if (ModelState.IsValid)
                {
                    // Ajout et sauvegarde en base de données
                    _context.Articles.Add(article);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Article created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                // Journalisation des erreurs de validation
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Count > 0)
                    {
                        _logger.LogWarning("Validation error for {Key}: {Errors}",
                            key, string.Join(", ", state.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                return View(article); // Retourne la vue avec les erreurs
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Articles Create POST");
                ModelState.AddModelError("", "An error occurred while creating the article. Please try again.");
                return View(article);
            }
        }

        // GET: Affiche le formulaire d'édition d'un article
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                // Vérification des droits d'administration
                if (HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    return RedirectToAction("Login", "Clients");
                }

                // Recherche de l'article par ID
                var article = await _context.Articles.FindAsync(id);
                if (article == null)
                {
                    return NotFound();
                }

                return View(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Articles Edit GET");
                return RedirectToAction("Error", "Home", new { message = "Failed to load article edit page. Please try again later." });
            }
        }

        // POST: Traite la soumission du formulaire d'édition
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,WarrantyMonths,Price,PurchaseDate")] Article article)
        {
            try
            {
                // Vérification des droits d'administration
                if (HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    return RedirectToAction("Login", "Clients");
                }

                // Vérification de la cohérence des IDs
                if (id != article.Id)
                {
                    return NotFound();
                }

                // Journalisation des données reçues
                _logger.LogInformation("Article Edit POST: Id={Id}, Name={Name}, Description={Description}, WarrantyMonths={WarrantyMonths}, Price={Price}, PurchaseDate={PurchaseDate}",
                    article.Id, article.Name, article.Description, article.WarrantyMonths, article.Price, article.PurchaseDate);

                if (ModelState.IsValid)
                {
                    try
                    {
                        // Mise à jour et sauvegarde
                        _context.Update(article);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Article updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Gestion des conflits de concurrence
                        if (!ArticleExists(article.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                // Journalisation des erreurs de validation
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Count > 0)
                    {
                        _logger.LogWarning("Validation error for {Key}: {Errors}",
                            key, string.Join(", ", state.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                return View(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Articles Edit POST");
                ModelState.AddModelError("", "An error occurred while updating the article. Please try again.");
                return View(article);
            }
        }

        // Vérifie l'existence d'un article
        private bool ArticleExists(int id)
        {
            return _context.Articles.Any(e => e.Id == id);
        }

        // GET: Affiche la confirmation de suppression
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Vérification des droits d'administration
                if (HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    return RedirectToAction("Login", "Clients");
                }

                // Recherche de l'article
                var article = await _context.Articles.FindAsync(id);
                if (article == null)
                {
                    return NotFound();
                }

                // Vérification des réclamations associées
                var hasComplaints = await _context.Complaints.AnyAsync(c => c.ArticleId == id);
                if (hasComplaints)
                {
                    TempData["ErrorMessage"] = "Cannot delete this article because it is associated with one or more complaints.";
                    return RedirectToAction(nameof(Index));
                }

                return View(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Articles Delete GET: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Failed to load article delete page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Confirme la suppression
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                _logger.LogInformation("Delete POST method called with ID: {Id}", id);

                // Vérification des droits d'administration
                if (HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    return RedirectToAction("Login", "Clients");
                }

                // Recherche de l'article
                var article = await _context.Articles.FindAsync(id);
                if (article == null)
                {
                    _logger.LogWarning("Article with ID {Id} not found", id);
                    return RedirectToAction("Index");
                }

                // Vérification des réclamations associées
                var hasComplaints = await _context.Complaints.AnyAsync(c => c.ArticleId == id);
                if (hasComplaints)
                {
                    _logger.LogWarning("Cannot delete article {Id} because it has associated complaints", id);
                    TempData["ErrorMessage"] = "Cannot delete this article because it is associated with one or more complaints.";
                    return RedirectToAction("Index");
                }

                // Suppression et sauvegarde
                _context.Articles.Remove(article);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Article with ID {Id} deleted successfully", id);
                TempData["SuccessMessage"] = "Article deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Articles Delete POST: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"An error occurred while deleting the article: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}