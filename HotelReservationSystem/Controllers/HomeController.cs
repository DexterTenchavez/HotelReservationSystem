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

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        // ✅ Home/Index - Main landing page (public)
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        // ✅ Create Reservation - GET
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create()
        {
            var lastReservation = _context.Reservations
                .OrderByDescending(r => r.ReservationId)
                .FirstOrDefault();

            // ✅ FIXED: Consistent reservation number generation
            int nextNumber = (lastReservation == null) ? 1 : lastReservation.ReservationId + 1;

            // ✅ AUTO-FILL GUEST NAME with logged-in user's name
            var user = await _userManager.GetUserAsync(User);
            var guestName = user?.FullName ?? User.Identity.Name;

            var model = new Reservation
            {
                ReservationNo = $"RSV-{nextNumber:D4}",
                GuestName = guestName, // Auto-fill the guest name
                Status = "Confirmed" // Default status
            };

            return View(model);
        }

        // ✅ Create Reservation - POST
        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation model)
        {
            if (ModelState.IsValid)
            {
                // Get the current logged-in user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // ✅ FIXED: Generate proper reservation number
                var lastReservation = _context.Reservations
                    .OrderByDescending(r => r.ReservationId)
                    .FirstOrDefault();

                int nextNumber = (lastReservation == null) ? 1 : lastReservation.ReservationId + 1;
                model.ReservationNo = $"RSV-{nextNumber:D4}";

                // ✅ FIXED: Correct price calculation
                decimal basePrice = 0;
                if (model.RoomType == "Single") basePrice = 1000;
                else if (model.RoomType == "Double") basePrice = 2000;
                else if (model.RoomType == "Suite") basePrice = 3500;

                model.TotalAmount = basePrice * model.NumberOfGuests;

                // Link reservation to user
                model.UserId = user.Id;
                model.CreatedDate = DateTime.Now;
                model.Status = "Confirmed"; // Set default status

                _context.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction("Receipt", new { id = model.ReservationId });
            }

            return View(model);
        }

        // ✅ Receipt
        [Authorize]
        public IActionResult Receipt(int id)
        {
            var reservation = _context.Reservations.FirstOrDefault(r => r.ReservationId == id);
            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        // ✅ Edit Reservation (Admin only) - GET
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var reservation = _context.Reservations.FirstOrDefault(r => r.ReservationId == id);
            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        // ✅ Edit Reservation (Admin only) - POST
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Reservation model)
        {
            if (ModelState.IsValid)
            {
                var reservation = await _context.Reservations.FindAsync(model.ReservationId);
                if (reservation == null)
                    return NotFound();

                reservation.GuestName = model.GuestName;
                reservation.RoomType = model.RoomType;
                reservation.NumberOfGuests = model.NumberOfGuests;
                reservation.CheckInDate = model.CheckInDate;
                reservation.CheckOutDate = model.CheckOutDate;
                reservation.Status = model.Status;

                // ✅ FIXED: Correct price calculation in edit
                decimal basePrice = 0;
                if (model.RoomType == "Single") basePrice = 1000;
                else if (model.RoomType == "Double") basePrice = 2000;
                else if (model.RoomType == "Suite") basePrice = 3500;

                reservation.TotalAmount = basePrice * model.NumberOfGuests;

                await _context.SaveChangesAsync();
                return RedirectToAction("AdminDashboard", "Account");
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
            return RedirectToAction("AdminDashboard", "Account");
        }

        // ✅ Edit User Reservation (for users to edit their own reservations) - GET
        [Authorize]
        public async Task<IActionResult> EditUserReservation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationId == id && r.UserId == user.Id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // ✅ Edit User Reservation (for users to edit their own reservations) - POST
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserReservation(Reservation model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var reservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.ReservationId == model.ReservationId && r.UserId == user.Id);

                if (reservation == null)
                {
                    return NotFound();
                }

                // Only allow editing certain fields for users
                reservation.GuestName = model.GuestName;
                reservation.RoomType = model.RoomType;
                reservation.NumberOfGuests = model.NumberOfGuests;
                reservation.CheckInDate = model.CheckInDate;
                reservation.CheckOutDate = model.CheckOutDate;

                // Recalculate total amount
                decimal basePrice = 0;
                if (model.RoomType == "Single") basePrice = 1000;
                else if (model.RoomType == "Double") basePrice = 2000;
                else if (model.RoomType == "Suite") basePrice = 3500;

                reservation.TotalAmount = basePrice * model.NumberOfGuests;

                await _context.SaveChangesAsync();
                return RedirectToAction("UserDashboard", "Account");
            }

            return View(model);
        }

        // ✅ Cancel Reservation (for users to cancel their own reservations)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationId == id && r.UserId == user.Id);

            if (reservation == null)
            {
                return NotFound();
            }

            reservation.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return RedirectToAction("UserDashboard", "Account");
        }

        // ✅ Privacy Page
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

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