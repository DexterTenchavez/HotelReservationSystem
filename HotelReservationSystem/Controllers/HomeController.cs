using HotelReservationSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HotelReservationSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ✅ Login Page
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        // ✅ Login (POST)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Dashboard");
                else
                    return RedirectToAction("Create");
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        // ✅ Register Page
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // ✅ Register (POST)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // ✅ Ensure roles exist
                    if (!await _roleManager.RoleExistsAsync("Admin"))
                        await _roleManager.CreateAsync(new IdentityRole("Admin"));

                    if (!await _roleManager.RoleExistsAsync("Customer"))
                        await _roleManager.CreateAsync(new IdentityRole("Customer"));

                    // ✅ First registered user becomes Admin automatically
                    if (_userManager.Users.Count() == 1)
                        await _userManager.AddToRoleAsync(user, "Admin");
                    else
                        await _userManager.AddToRoleAsync(user, "Customer");

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // ✅ Dashboard (Admin only)
        [Authorize(Roles = "Admin")]
        public IActionResult Dashboard()
        {
            var reservations = _context.Reservations
                           .OrderByDescending(r => r.ReservationId)
                           .ToList();

            return View(reservations);
        }

        // ✅ Logout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index");
        }

        // ✅ Create Reservation (GET)
        [Authorize]
        public IActionResult Create()
        {
            var lastReservation = _context.Reservations
                .OrderByDescending(r => r.ReservationId)
                .FirstOrDefault();

            int nextNumber = (lastReservation == null) ? 1001 : lastReservation.ReservationId + 1;

            var model = new Reservation
            {
                ReservationNo = $"RSV-{nextNumber:D4}"
            };

            return View(model);
        }

        // ✅ Create Reservation (POST)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation model)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(model.ReservationNo))
                {
                    int nextId = _context.Reservations.Count() + 1;
                    model.ReservationNo = $"RSV-{1000 + nextId}";
                }

                // ✅ Auto-price based on RoomType
                if (model.RoomType == "Single") model.TotalAmount = 1000;
                else if (model.RoomType == "Double") model.TotalAmount = 1800;
                else if (model.RoomType == "Suite") model.TotalAmount = 2500;

                _context.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction("Receipt", new { id = model.ReservationId });
            }

            return View(model);
        }

       
        [Authorize]
        public IActionResult Receipt(int id)
        {
             var reservation = _context.Reservations.FirstOrDefault(r => r.ReservationId == id);
             if (reservation == null)
                 return NotFound();

             return View(reservation);

          
        }

        // ✅ Edit Reservation (Admin only)
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var reservation = _context.Reservations.FirstOrDefault(r => r.ReservationId == id);
            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Reservation model)
        {
            if (ModelState.IsValid)
            {
                // Fetch the existing record
                var reservation = await _context.Reservations.FindAsync(model.ReservationId);
                if (reservation == null)
                    return NotFound();

                // Update fields
                reservation.GuestName = model.GuestName;
                reservation.RoomType = model.RoomType;
                reservation.NumberOfGuests = model.NumberOfGuests;
                reservation.TotalAmount = model.TotalAmount;

                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }




        // ✅ Delete Reservation (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return NotFound();

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return RedirectToAction("Dashboard");
        }

        
        


        // ✅ Privacy Page
        public IActionResult Privacy()
        {
            return View();
        }

        // ✅ Error Page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
