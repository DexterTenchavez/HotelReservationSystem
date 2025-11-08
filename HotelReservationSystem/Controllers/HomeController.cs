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

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create()
        {
            var lastReservation = _context.Reservations
                .OrderByDescending(r => r.ReservationId)
                .FirstOrDefault();

            int nextNumber = (lastReservation == null) ? 1 : lastReservation.ReservationId + 1;

            var user = await _userManager.GetUserAsync(User);
            var guestName = user?.FullName ?? User.Identity.Name;

            var availableRooms = await _context.Rooms
                .Where(r => r.IsAvailable)
                .ToListAsync();

            var model = new Reservation
            {
                ReservationNo = $"RSV-{nextNumber:D4}",
                GuestName = guestName,
                Status = "Confirmed"
            };

            ViewData["AvailableRooms"] = availableRooms;
            return View(model);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var lastReservation = _context.Reservations
                    .OrderByDescending(r => r.ReservationId)
                    .FirstOrDefault();

                int nextNumber = (lastReservation == null) ? 1 : lastReservation.ReservationId + 1;
                model.ReservationNo = $"RSV-{nextNumber:D4}";

                var selectedRoom = await _context.Rooms.FindAsync(model.RoomId);
                if (selectedRoom == null || !selectedRoom.IsAvailable)
                {
                    ModelState.AddModelError("RoomId", "Selected room is not available.");

                    var availableRoomsList = await _context.Rooms
                        .Where(r => r.IsAvailable)
                        .ToListAsync();
                    ViewData["AvailableRooms"] = availableRoomsList;
                    return View(model);
                }

                model.RoomNumber = selectedRoom.RoomNumber;
                model.TotalAmount = selectedRoom.PricePerNight * model.NumberOfNights;

                model.UserId = user.Id;
                model.CreatedDate = DateTime.Now;
                model.Status = "Confirmed";

                selectedRoom.IsAvailable = false;

                _context.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction("Receipt", new { id = model.ReservationId });
            }

            var availableRoomsReload = await _context.Rooms
                .Where(r => r.IsAvailable)
                .ToListAsync();
            ViewData["AvailableRooms"] = availableRoomsReload;

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
                reservation.CheckInDate = model.CheckInDate;
                reservation.CheckOutDate = model.CheckOutDate;
                reservation.Status = model.Status;

                decimal basePrice = 0;
                if (model.RoomType == "Single") basePrice = 1000;
                else if (model.RoomType == "Double") basePrice = 2000;
                else if (model.RoomType == "Suite") basePrice = 3500;

                reservation.TotalAmount = basePrice * model.NumberOfNights;

                await _context.SaveChangesAsync();
                return RedirectToAction("AdminDashboard", "Account");
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
            return RedirectToAction("AdminDashboard", "Account");
        }

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

            var availableRooms = await _context.Rooms
                .Where(r => r.IsAvailable || r.RoomId == reservation.RoomId)
                .ToListAsync();

            ViewData["AvailableRooms"] = availableRooms;
            return View(reservation);
        }

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

                reservation.GuestName = model.GuestName;
                reservation.RoomType = model.RoomType;
                reservation.NumberOfGuests = model.NumberOfGuests;
                reservation.CheckInDate = model.CheckInDate;
                reservation.CheckOutDate = model.CheckOutDate;

                decimal basePrice = 0;
                if (model.RoomType == "Single") basePrice = 1000;
                else if (model.RoomType == "Double") basePrice = 2000;
                else if (model.RoomType == "Suite") basePrice = 3500;

                reservation.TotalAmount = basePrice * model.NumberOfNights;

                await _context.SaveChangesAsync();
                return RedirectToAction("UserDashboard", "Account");
            }

            return View(model);
        }

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