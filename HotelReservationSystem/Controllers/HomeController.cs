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
        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                return View("AdminIndex");
            }

            var availableRooms = await _context.Rooms
                .Where(r => r.IsAvailable && !r.UnderMaintenance)
                .OrderBy(r => r.RoomType)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();

            ViewData["AvailableRooms"] = availableRooms;
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


        [Authorize]
        [HttpGet]
        public async Task<JsonResult> CheckActiveReservations()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { hasActiveReservation = false });
                }

                // Check for active reservations (Confirmed, Checked-In, or Pending)
                var activeReservations = await _context.Reservations
                    .Where(r => r.UserId == user.Id &&
                               (r.Status == "Confirmed" || r.Status == "Checked-In" || r.Status == "Pending"))
                    .OrderByDescending(r => r.CreatedDate)
                    .FirstOrDefaultAsync();

                if (activeReservations != null)
                {
                    return Json(new
                    {
                        hasActiveReservation = true,
                        status = activeReservations.Status,
                        reservationNo = activeReservations.ReservationNo,
                        roomNumber = activeReservations.RoomNumber,
                        checkInDate = activeReservations.CheckInDate?.ToString("MMM dd, yyyy"),
                        checkOutDate = activeReservations.CheckOutDate?.ToString("MMM dd, yyyy")
                    });
                }

                return Json(new { hasActiveReservation = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active reservations for user {UserId}", User.Identity?.Name);
                return Json(new { hasActiveReservation = false });
            }
        }

         
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(int? roomId)
        {
            if (!roomId.HasValue)
            {
                TempData["ErrorMessage"] = "Please select a room to book.";
                return RedirectToAction("Index");
            }

            var selectedRoom = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == roomId.Value && r.IsAvailable && !r.UnderMaintenance);

            if (selectedRoom == null)
            {
                TempData["ErrorMessage"] = "Selected room is not available. Please choose another room.";
                return RedirectToAction("Index");
            }

            var lastReservation = _context.Reservations
                .OrderByDescending(r => r.ReservationId)
                .FirstOrDefault();

            int nextNumber = (lastReservation == null) ? 1 : lastReservation.ReservationId + 1;

            var user = await _userManager.GetUserAsync(User);
            var guestName = user?.FullName ?? User.Identity.Name;

            var model = new Reservation
            {
                ReservationNo = $"RSV-{nextNumber:D4}",
                GuestName = guestName,
                Status = "Confirmed",
                PaymentStatus = "Pending",
                NumberOfGuests = 1,
                CheckInDate = null,
                CheckOutDate = null,
                NumberOfNights = 0,
                RoomId = selectedRoom.RoomId,
                RoomType = selectedRoom.RoomType,
                RoomNumber = selectedRoom.RoomNumber,
                TotalAmount = selectedRoom.PricePerNight
            };

            // Pass max guests and room info to view
            ViewData["MaxGuests"] = selectedRoom.MaxGuests;
            ViewData["RoomType"] = selectedRoom.RoomType;
            ViewData["SelectedRoom"] = selectedRoom; // Pass the entire room object

            return View(model);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    var selectedRoom = await _context.Rooms.FindAsync(model.RoomId);
                    if (selectedRoom == null || !selectedRoom.IsAvailable)
                    {
                        ModelState.AddModelError("RoomId", "Selected room is not available.");
                        return View(model);
                    }

                    // Generate Reservation Number
                    var lastReservation = _context.Reservations
                        .OrderByDescending(r => r.ReservationId)
                        .FirstOrDefault();

                    int nextNumber = (lastReservation == null) ? 1 : lastReservation.ReservationId + 1;
                    model.ReservationNo = $"RSV-{nextNumber:D4}";

                    // ✅ GENERATE TRANSACTION NUMBER (LOGICAL PROOF)
                    model.TransactionNumber = $"TXN-{DateTime.Now:yyyyMMdd}-{nextNumber:D4}";

                    // ✅ PAYMENT STATUS BASED ON PAYMENT METHOD
                    if (model.PaymentMethod == "Cash")
                    {
                        model.PaymentStatus = "Pending"; // Not paid until cash received
                        model.PaymentDate = null;
                    }
                    else
                    {
                        model.PaymentStatus = "Paid"; // Auto-paid for online payments
                        model.PaymentDate = DateTime.Now;
                    }

                    model.TotalAmount = selectedRoom.PricePerNight * model.NumberOfNights;
                    model.UserId = user.Id;
                    model.CreatedDate = DateTime.Now;
                    model.Status = "Confirmed";

                    // Mark room as unavailable
                    selectedRoom.IsAvailable = false;

                    _context.Reservations.Add(model);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Receipt", new { id = model.ReservationId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating the reservation.");
                    _logger.LogError(ex, "Error creating reservation");
                }
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Receipt(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.ReservationId == id);

            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Reservation not found.";
                return RedirectToAction("UserDashboard", "Account");
            }

            var user = await _userManager.GetUserAsync(User);
            if (reservation.UserId != user.Id && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "You are not authorized to view this receipt.";
                return RedirectToAction("UserDashboard", "Account");
            }

            return View(reservation);
        }

        [Authorize]
        public async Task<IActionResult> EditUserReservation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var reservation = await _context.Reservations
                .Include(r => r.Room) // Include room to get max guests
                .FirstOrDefaultAsync(r => r.ReservationId == id && r.UserId == user.Id);

            if (reservation == null)
            {
                return NotFound();
            }

            // Pass room capacity info
            if (reservation.Room != null)
            {
                ViewData["MaxGuests"] = reservation.Room.MaxGuests;
                ViewData["RoomType"] = reservation.Room.RoomType;
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
                reservation.NumberOfGuests = model.NumberOfGuests;
                reservation.CheckInDate = model.CheckInDate;
                reservation.CheckOutDate = model.CheckOutDate;

                if (model.CheckInDate.HasValue && model.CheckOutDate.HasValue)
                {
                    reservation.NumberOfNights = (model.CheckOutDate.Value - model.CheckInDate.Value).Days;
                }

                // Get the room price from the actual room, not hardcoded values
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == reservation.RoomId);
                if (room != null)
                {
                    reservation.TotalAmount = room.PricePerNight * reservation.NumberOfNights;
                }

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

                _rateLimitService.RecordAttempt(rateLimitKey, timeWindow);

                if (!reservation.CanBeCancelled)
                {
                    TempData["ErrorMessage"] = "This reservation cannot be cancelled. Cancellation is only allowed at least 1 hour before check-in.";
                    return RedirectToAction("UserDashboard", "Account");
                }

                if (string.IsNullOrWhiteSpace(cancellationReason))
                {
                    TempData["ErrorMessage"] = "Cancellation reason is required.";
                    return RedirectToAction("UserDashboard", "Account");
                }

                reservation.Status = "Cancelled";
                reservation.CancelledDate = DateTime.Now;
                reservation.CancellationReason = cancellationReason.Trim();

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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.ReservationId == id);

            if (reservation == null)
                return NotFound();

            // Pass room capacity info
            if (reservation.Room != null)
            {
                ViewData["MaxGuests"] = reservation.Room.MaxGuests;
                ViewData["RoomType"] = reservation.Room.RoomType;
            }

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
                reservation.NumberOfGuests = model.NumberOfGuests;
                reservation.CheckInDate = model.CheckInDate;
                reservation.CheckOutDate = model.CheckOutDate;
                reservation.Status = model.Status;

                if (model.CheckInDate.HasValue && model.CheckOutDate.HasValue)
                {
                    reservation.NumberOfNights = (model.CheckOutDate.Value - model.CheckInDate.Value).Days;
                }

                // Get the room price from the actual room, not hardcoded values
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == reservation.RoomId);
                if (room != null)
                {
                    reservation.TotalAmount = room.PricePerNight * reservation.NumberOfNights;
                }

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
                var reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                if (reservation.Status != "Confirmed")
                {
                    TempData["ErrorMessage"] = $"Cannot check in reservation with status: {reservation.Status}";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                reservation.Status = "Checked-In";

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNo} checked in successfully.";
                TempData["ShowUndoCheckIn"] = reservationId.ToString();
            }
            catch (Exception ex)
            {
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
                var reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                if (reservation.Status != "Checked-In")
                {
                    TempData["ErrorMessage"] = $"Cannot complete reservation with status: {reservation.Status}";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                reservation.Status = "Completed";
                reservation.ActualCheckOut = DateTime.Now;

                if (reservation.RoomId.HasValue)
                {
                    var room = await _context.Rooms.FindAsync(reservation.RoomId.Value);
                    if (room != null)
                    {
                        room.IsAvailable = true;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNo} completed successfully. Room is now available.";
                TempData["ShowUndoComplete"] = reservationId.ToString();
            }
            catch (Exception ex)
            {
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(int reservationId, int rating, string feedback, bool wouldRecommend)
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

                if (reservation.Status != "Completed")
                {
                    TempData["ErrorMessage"] = "You can only rate completed reservations.";
                    return RedirectToAction("UserDashboard", "Account");
                }

                if (reservation.Rating.HasValue)
                {
                    TempData["ErrorMessage"] = "You have already rated this reservation.";
                    return RedirectToAction("UserDashboard", "Account");
                }

                reservation.Rating = rating;
                reservation.Feedback = feedback;
                reservation.RatingDate = DateTime.Now;

                if (reservation.RoomId.HasValue)
                {
                    var room = await _context.Rooms.FindAsync(reservation.RoomId.Value);
                    if (room != null)
                    {
                        var totalRatings = room.TotalRatings + 1;
                        var newAverage = ((room.AverageRating * room.TotalRatings) + rating) / totalRatings;

                        room.AverageRating = newAverage;
                        room.TotalRatings = totalRatings;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you for your feedback!";
                return RedirectToAction("UserDashboard", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback for reservation {ReservationId}", reservationId);
                TempData["ErrorMessage"] = "Error submitting feedback.";
                return RedirectToAction("UserDashboard", "Account");
            }
        }


        [Authorize(Roles = "Admin")]
        public IActionResult CreateRoom()
        {
            // Set default values for the form
            var model = new Room
            {
                MaxGuests = 2,
                PricePerNight = 1000,
                IsAvailable = true
            };
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoom(Room model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if room number already exists
                    var existingRoom = await _context.Rooms
                        .FirstOrDefaultAsync(r => r.RoomNumber == model.RoomNumber);

                    if (existingRoom != null)
                    {
                        ModelState.AddModelError("RoomNumber", "Room number already exists. Please use a different room number.");
                        return View(model);
                    }

                    // Handle image upload
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        var imageUrl = await SaveRoomImage(model.ImageFile, model.RoomNumber);
                        model.ImageUrl = imageUrl;
                    }
                    else
                    {
                        // Set default image based on room type
                        model.ImageUrl = $"/images/{model.RoomType.ToLower()}1.jpg";
                    }

                    // Set default values
                    model.IsAvailable = true;
                    model.UnderMaintenance = false;
                    model.AverageRating = 0;
                    model.TotalRatings = 0;

                    _context.Rooms.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Room {model.RoomNumber} ({model.RoomType}) created successfully!";
                    return RedirectToAction("RoomManagement");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating room");
                    ModelState.AddModelError("", "An error occurred while creating the room.");
                }
            }

            return View(model);
        }

        private async Task<string> SaveRoomImage(IFormFile imageFile, string roomNumber)
        {
            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "rooms");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generate unique filename
                var fileName = $"room_{roomNumber}_{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Return the URL path
                return $"/uploads/rooms/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving room image");
                return "/images/placeholder-room.jpg";
            }
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

        // Add this method to your HomeController.cs

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserReservation(int reservationId)
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

                // ✅ Soft delete - just hide from user's view
                reservation.IsDeletedByUser = true;
                reservation.DeletedByUserDate = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Reservation {ReservationNo} soft deleted by user {UserId}",
                    reservation.ReservationNo, user.Id);

                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNo} has been removed from your dashboard.";
                return RedirectToAction("UserDashboard", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting reservation {ReservationId}", reservationId);
                TempData["ErrorMessage"] = "Error removing reservation.";
                return RedirectToAction("UserDashboard", "Account");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCashPayment(int reservationId, string receiptNumber)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                if (reservation.PaymentStatus == "Paid")
                {
                    TempData["ErrorMessage"] = "Payment is already confirmed.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                if (reservation.PaymentMethod != "Cash")
                {
                    TempData["ErrorMessage"] = "Only cash payments can be confirmed with receipt numbers.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                if (string.IsNullOrEmpty(receiptNumber))
                {
                    TempData["ErrorMessage"] = "Official receipt number is required.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                // ✅ UPDATE SYSTEM ONLY WHEN REAL CASH RECEIVED WITH RECEIPT
                reservation.PaymentStatus = "Paid";
                reservation.PaymentDate = DateTime.Now;
                reservation.ReceiptNumber = receiptNumber.Trim().ToUpper(); // Store receipt number

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cash payment confirmed! Official Receipt #{receiptNumber} recorded for reservation {reservation.ReservationNo}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming cash payment for reservation {ReservationId}", reservationId);
                TempData["ErrorMessage"] = "An error occurred while confirming the payment.";
            }

            return RedirectToAction("AdminDashboard", "Account");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoom(int roomId)
        {
            try
            {
                var room = await _context.Rooms
                    .Include(r => r.Reservations)
                    .FirstOrDefaultAsync(r => r.RoomId == roomId);

                if (room == null)
                {
                    TempData["ErrorMessage"] = "Room not found.";
                    return RedirectToAction("RoomManagement");
                }

                // Check if room has active reservations
                var activeReservations = room.Reservations?.Any(r =>
                    r.Status == "Confirmed" || r.Status == "Checked-In");

                if (activeReservations == true)
                {
                    TempData["ErrorMessage"] = $"Cannot delete Room {room.RoomNumber} because it has active reservations.";
                    return RedirectToAction("RoomManagement");
                }

                // Check if room is currently occupied
                if (!room.IsAvailable && !room.UnderMaintenance)
                {
                    TempData["ErrorMessage"] = $"Cannot delete Room {room.RoomNumber} because it is currently occupied.";
                    return RedirectToAction("RoomManagement");
                }

                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Room {room.RoomNumber} has been deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room {RoomId}", roomId);
                TempData["ErrorMessage"] = "An error occurred while deleting the room.";
            }

            return RedirectToAction("RoomManagement");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int reservationId)
        {
            try
            {
                var reservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                if (reservation.PaymentStatus == "Paid")
                {
                    TempData["ErrorMessage"] = "Payment is already confirmed.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                // Don't allow manual confirmation for cash payments (use ConfirmCashPayment instead)
                if (reservation.PaymentMethod == "Cash")
                {
                    TempData["ErrorMessage"] = "Use 'Confirm Cash Payment' for cash payments to enter receipt number.";
                    return RedirectToAction("AdminDashboard", "Account");
                }

                // Update payment status for online payments
                reservation.PaymentStatus = "Paid";
                reservation.PaymentDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payment for reservation {reservation.ReservationNo} has been confirmed successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment for reservation {ReservationId}", reservationId);
                TempData["ErrorMessage"] = "An error occurred while confirming the payment.";
            }

            return RedirectToAction("AdminDashboard", "Account");
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.ReservationId == id);
        }
    }
}