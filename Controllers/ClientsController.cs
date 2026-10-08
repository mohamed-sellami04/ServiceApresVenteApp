using Microsoft.AspNetCore.Mvc;
using ServiceApresVenteApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceApresVenteApp.Controllers
{
    // Contrôleur pour la gestion des clients (authentification et inscription)
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context; // Contexte de la base de données
        private readonly ILogger<ClientsController> _logger; // Logger pour le suivi des activités

        // Constructeur avec injection de dépendances
        public ClientsController(ApplicationDbContext context, ILogger<ClientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Affiche le formulaire d'inscription
        [HttpGet]
        public IActionResult Register()
        {
            return View(); // Retourne la vue d'inscription
        }

        // POST: Traite la soumission du formulaire d'inscription
        [HttpPost]
        public async Task<IActionResult> Register(Client client)
        {
            // Journalisation des données reçues
            _logger.LogInformation("Register POST received: Name={Name}, Email={Email}, Password={Password}, Role={Role}, ComplaintsCount={ComplaintsCount}",
                client.Name, client.Email, client.Password, client.Role, client.Complaints?.Count ?? 0);

            // Défaut du rôle à "Client" si non spécifié
            client.Role = client.Role ?? "Client";

            // Validation du modèle
            if (ModelState.IsValid)
            {
                try
                {
                    // Vérification de l'unicité de l'email
                    if (await _context.Clients.AnyAsync(c => c.Email == client.Email))
                    {
                        ModelState.AddModelError("Email", "This email is already registered.");
                        _logger.LogWarning("Registration failed: Email {Email} already exists.", client.Email);
                        return View(client);
                    }

                    // Ajout du nouveau client
                    _context.Clients.Add(client);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Registration successful for {Email}.", client.Email);
                    TempData["SuccessMessage"] = "Registration successful! Please log in.";
                    return RedirectToAction("Login"); // Redirection vers la page de connexion
                }
                catch (Exception ex)
                {
                    // Gestion des erreurs
                    ModelState.AddModelError("", $"Registration failed: {ex.Message}");
                    _logger.LogError(ex, "Registration failed for {Email}.", client.Email);
                }
            }
            else
            {
                // Journalisation des erreurs de validation
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation error: {ErrorMessage}", error.ErrorMessage);
                }
            }
            return View(client); // Retourne la vue avec les erreurs
        }

        // GET: Affiche le formulaire de connexion
        [HttpGet]
        public IActionResult Login()
        {
            // Passage des messages de succès (ex: après inscription)
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View();
        }

        // POST: Traite la soumission du formulaire de connexion
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Validation des champs requis
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            // Recherche du client avec vérification d'email (case insensitive) et mot de passe
            var client = await _context.Clients.FirstOrDefaultAsync(
                c => c.Email.Trim().ToLower() == email.Trim().ToLower()
                && c.Password == password);

            if (client != null)
            {
                // Création de la session
                HttpContext.Session.SetString("UserId", client.Id.ToString());
                HttpContext.Session.SetString("UserRole", client.Role);

                // Redirection selon le rôle
                if (client.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin"); // Espace admin
                }
                return RedirectToAction("Create", "Complaints"); // Espace client
            }

            // Erreur si identifiants invalides
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        // GET: Déconnexion de l'utilisateur
        [HttpGet]
        public IActionResult Logout()
        {
            // Nettoyage de la session
            HttpContext.Session.Clear();
            return RedirectToAction("Login"); // Redirection vers la page de connexion
        }
    }
}