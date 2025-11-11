using HotelReservationSystem.Models;
using HotelReservationSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        private readonly IRateLimitService _rateLimitService;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IRateLimitService rateLimitService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _rateLimitService = rateLimitService;
        }

        [AllowAnonymous]
        public IActionResult Landing()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
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

        // ============ CUSTOMER RESERVATION METHODS ============
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

        // ============ USER RESERVATION MANAGEMENT ============
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

                if (reservation.Status == "Checked-In" || reservation.Status == "Completed")
                {
                    TempData["ErrorMessage"] = "Cannot edit reservation that is already checked-in or completed.";
                    return RedirectToAction("UserDashboard", "Account");
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

                TempData["SuccessMessage"] = "Reservation updated successfully!";
                return RedirectToAction("UserDashboard", "Account");
            }

            var availableRooms = await _context.Rooms
                .Where(r => r.IsAvailable || r.RoomId == model.RoomId)
                .ToListAsync();
            ViewData["AvailableRooms"] = availableRooms;

            return View(model);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int reservationId, string cancellationReason = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var reservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.UserId == user.Id);

                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToAction("UserDashboard", "Account");
                }

                // RATE LIMITING: 3 cancellations per hour per user
                var rateLimitKey = $"cancel_reservation:{user.Id}";
                var maxAttempts = 3;
                var timeWindow = TimeSpan.FromHours(1);

                if (_rateLimitService.IsRateLimited(rateLimitKey, maxAttempts, timeWindow))
                {
                    var timeUntilReset = _rateLimitService.GetTimeUntilReset(rateLimitKey, timeWindow);
                    var minutes = timeUntilReset.HasValue ? Math.Ceiling(timeUntilReset.Value.TotalMinutes) : 60;

                    _logger.LogWarning("Rate limit exceeded for user {UserId}. Attempts: {MaxAttempts} per hour",
                        user.Id, maxAttempts);

                    TempData["ErrorMessage"] = $"You have reached the maximum cancellation limit ({maxAttempts} per hour). Please try again in {minutes} minutes.";
                    return RedirectToAction("UserDashboard", "Account");
                }

                // Record the cancellation attempt
                _rateLimitService.RecordAttempt(rateLimitKey, timeWindow);

                // Check reservation-specific cancellation rules
                if (!reservation.CanBeCancelled)
                {
                    TempData["ErrorMessage"] = "This reservation cannot be cancelled. Cancellation is only allowed at least 1 hour before check-in.";
                    return RedirectToAction("UserDashboard", "Account");
                }

                // Validate cancellation reason
                if (string.IsNullOrWhiteSpace(cancellationReason))
                {
                    TempData["ErrorMessage"] = "Cancellation reason is required.";
                    return RedirectToAction("UserDashboard", "Account");
                }

                // Perform actual cancellation
                reservation.Status = "Cancelled";
                reservation.CancelledDate = DateTime.Now;
                reservation.CancellationReason = cancellationReason.Trim();

                // Make the room available again
                if (reservation.RoomId.HasValue)
                {
                    var room = await _context.Rooms.FindAsync(reservation.RoomId.Value);
                    if (room != null)
                    {
                        room.IsAvailable = true;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Reservation {ReservationNo} cancelled by user {UserId}",
                    reservation.ReservationNo, user.Id);

                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNo} has been cancelled successfully.";
                return RedirectToAction("UserDashboard", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling reservation {ReservationId} for user", reservationId);
                TempData["ErrorMessage"] = "Error cancelling reservation.";
                return RedirectToAction("UserDashboard", "Account");
            }
        }



        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("CancellationLimit")]
        public async Task<IActionResult> UndoCancelReservation(int reservationId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var reservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.UserId == user.Id);

                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToAction("UserDashboard", "Account");
                }

                if (reservation.Status != "Cancelled")
                {
                    TempData["ErrorMessage"] = $"Cannot undo cancellation for reservation with status: {reservation.Status}";
                    return RedirectToAction("UserDashboard", "Account");
                }

                // Check if the room is still available
                if (reservation.RoomId.HasValue)
                {
                    var room = await _context.Rooms.FindAsync(reservation.RoomId.Value);
                    if (room == null || !room.IsAvailable)
                    {
                        TempData["ErrorMessage"] = "Cannot restore reservation because the room is no longer available.";
                        return RedirectToAction("UserDashboard", "Account");
                    }
                }

                reservation.Status = "Confirmed";
                reservation.CancelledDate = null;
                reservation.CancellationReason = null;

                // Mark room as occupied again
                if (reservation.RoomId.HasValue)
                {
                    var room = await _context.Rooms.FindAsync(reservation.RoomId.Value);
                    if (room != null)
                    {
                        room.IsAvailable = false;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNo} has been restored successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error undoing cancellation");
                TempData["ErrorMessage"] = "Error restoring reservation.";
            }

            return RedirectToAction("UserDashboard", "Account");
        }

        // ============ ADMIN METHODS ============
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.ReservationId == id);

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

                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNo} updated successfully.";
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

            TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNo} deleted successfully.";
            return RedirectToAction("AdminDashboard", "Account");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInReservation(int reservationId)
        {
            try
            {
                Console.WriteLine($"=== CHECK IN STARTED ===");
                Console.WriteLine($"Reservation ID: {reservationId}");

                var reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

                if (reservation == null)
                {
                    Console.WriteLine($"Reservation not found for ID: {reservationId}");
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                Console.WriteLine($"Found reservation: {reservation.ReservationNo}");
                Console.WriteLine($"Current status: {reservation.Status}");

                if (reservation.Status != "Confirmed")
                {
                    Console.WriteLine($"Cannot check in - wrong status: {reservation.Status}");
                    TempData["ErrorMessage"] = $"Cannot check in reservation with status: {reservation.Status}";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                // Update the status
                reservation.Status = "Checked-In";
                Console.WriteLine($"Updating status to: Checked-In");

                // Save changes
                var changes = await _context.SaveChangesAsync();
                Console.WriteLine($"Database changes saved: {changes} changes");

                // Set TempData for undo button - FIXED: Use proper key
                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNo} checked in successfully.";
                TempData["ShowUndoCheckIn"] = reservationId.ToString(); // FIXED: Convert to string

                Console.WriteLine($"=== CHECK IN COMPLETED ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== CHECK IN ERROR ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");

                _logger.LogError(ex, "Error checking in reservation");
                TempData["ErrorMessage"] = "Error checking in reservation.";
            }

            return RedirectToAction("AdminDashboard", "Account");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UndoCheckIn(int id)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.ReservationId == id);

                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                if (reservation.Status != "Checked-In")
                {
                    TempData["ErrorMessage"] = $"Cannot undo check-in for reservation with status: {reservation.Status}";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                reservation.Status = "Confirmed";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Check-in for reservation {reservation.ReservationNo} has been undone.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error undoing check-in");
                TempData["ErrorMessage"] = "Error undoing check-in.";
            }

            return RedirectToAction("AdminDashboard", "Account");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteReservationEarly(int reservationId)
        {
            try
            {
                Console.WriteLine($"=== COMPLETE RESERVATION STARTED ===");
                Console.WriteLine($"Reservation ID: {reservationId}");

                var reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

                if (reservation == null)
                {
                    Console.WriteLine($"Reservation not found for ID: {reservationId}");
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                Console.WriteLine($"Found reservation: {reservation.ReservationNo}");
                Console.WriteLine($"Current status: {reservation.Status}");

                if (reservation.Status != "Checked-In")
                {
                    Console.WriteLine($"Cannot complete - wrong status: {reservation.Status}");
                    TempData["ErrorMessage"] = $"Cannot complete reservation with status: {reservation.Status}";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                // Update the reservation status
                reservation.Status = "Completed";
                reservation.ActualCheckOut = DateTime.Now;
                Console.WriteLine($"Updating status to: Completed");

                // Update the room availability
                if (reservation.RoomId.HasValue)
                {
                    var room = await _context.Rooms.FindAsync(reservation.RoomId.Value);
                    if (room != null)
                    {
                        room.IsAvailable = true;
                        Console.WriteLine($"Room {room.RoomNumber} marked as available");
                    }
                }

                // Save changes to database
                var changes = await _context.SaveChangesAsync();
                Console.WriteLine($"Database changes saved: {changes} changes");

                // Set TempData for undo button - FIXED: Use proper key
                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNo} completed successfully. Room is now available.";
                TempData["ShowUndoComplete"] = reservationId.ToString(); // FIXED: Convert to string

                Console.WriteLine($"=== COMPLETE RESERVATION COMPLETED ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== COMPLETE RESERVATION ERROR ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");

                _logger.LogError(ex, "Error completing reservation");
                TempData["ErrorMessage"] = $"Error completing reservation: {ex.Message}";
            }

            return RedirectToAction("AdminDashboard", "Account");
        }



        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UndoCompleteReservation(int id)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.ReservationId == id);

                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                if (reservation.Status != "Completed")
                {
                    TempData["ErrorMessage"] = $"Cannot undo completion for reservation with status: {reservation.Status}";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                reservation.Status = "Checked-In";
                reservation.ActualCheckOut = null;

                if (reservation.Room != null)
                {
                    reservation.Room.IsAvailable = false;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Completion for reservation {reservation.ReservationNo} has been undone.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error undoing reservation completion");
                TempData["ErrorMessage"] = "Error undoing reservation completion.";
            }

            return RedirectToAction("AdminDashboard", "Account");
        }

        // ============ ROOM MANAGEMENT ============
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RoomManagement()
        {
            var rooms = await _context.Rooms
                .Include(r => r.Reservations.Where(res => res.Status != "Completed" && res.Status != "Cancelled"))
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            return View(rooms);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRoomAvailable(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
                return NotFound();

            room.IsAvailable = true;
            room.UnderMaintenance = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Room {room.RoomNumber} has been marked as available.";
            return RedirectToAction("RoomManagement");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRoomUnderMaintenance(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
                return NotFound();

            room.IsAvailable = false;
            room.UnderMaintenance = true;

            // Cancel any active reservations for this room
            var activeReservations = await _context.Reservations
                .Where(r => r.RoomId == roomId && (r.Status == "Confirmed" || r.Status == "Checked-In"))
                .ToListAsync();

            foreach (var reservation in activeReservations)
            {
                reservation.Status = "Cancelled";
                reservation.RoomId = null;
                reservation.RoomNumber = null;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Room {room.RoomNumber} has been marked under maintenance. " +
                                        $"{activeReservations.Count} active reservations cancelled.";
            return RedirectToAction("RoomManagement");
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.ReservationId == id);
        }
    }
}