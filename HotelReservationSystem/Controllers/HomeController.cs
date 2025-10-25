using HotelReservationSystem.Models;
using Microsoft.AspNetCore.Authorization;
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

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // ✅ Home/Index - Main landing page (public)
        [AllowAnonymous]
        public IActionResult Index()
        {
            // Return empty view for landing page - no model needed
            return View();
        }

        // ✅ Dashboard (Admin only) - shows reservations
        [Authorize(Roles = "Admin")]
        public IActionResult Dashboard()
        {
            var reservations = _context.Reservations
                           .OrderByDescending(r => r.ReservationId)
                           .ToList();

            return View(reservations);
        }

        // ... rest of your HomeController methods remain the same
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
                var reservation = await _context.Reservations.FindAsync(model.ReservationId);
                if (reservation == null)
                    return NotFound();

                reservation.GuestName = model.GuestName;
                reservation.RoomType = model.RoomType;
                reservation.NumberOfGuests = model.NumberOfGuests;
                reservation.TotalAmount = model.TotalAmount;

                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }

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