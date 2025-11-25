using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Models;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
public class CashReceiptsController : Controller
{
    private readonly AppDbContext _context;

    public CashReceiptsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var pendingPayments = _context.Reservations
            .Where(r => r.PaymentStatus == "Pending" && r.Status != "Cancelled")
            .OrderBy(r => r.CreatedDate)
            .ToList();

        var todayReceipts = _context.Reservations
            .Where(r => r.PaymentStatus == "Paid" &&
                       r.PaymentDate.Value.Date == DateTime.Today)
            .OrderByDescending(r => r.PaymentDate)
            .ToList();

        var model = new CashReceiptsViewModel
        {
            PendingPayments = pendingPayments,
            TodayReceipts = todayReceipts,
            TotalPendingAmount = pendingPayments.Sum(r => r.TotalAmount),
            TotalTodayCash = todayReceipts.Sum(r => r.TotalAmount)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceiveCash(int reservationId, string receiptNumber)
    {
        try
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Reservation not found.";
                return RedirectToAction("Index");
            }

            if (reservation.PaymentStatus == "Paid")
            {
                TempData["ErrorMessage"] = "Payment is already confirmed.";
                return RedirectToAction("Index");
            }

            if (reservation.PaymentMethod != "Cash")
            {
                TempData["ErrorMessage"] = "Only cash payments can be processed here.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(receiptNumber))
            {
                TempData["ErrorMessage"] = "Official receipt number is required.";
                return RedirectToAction("Index");
            }

            reservation.PaymentStatus = "Paid";
            reservation.PaymentDate = DateTime.Now;
            reservation.ReceiptNumber = receiptNumber.Trim().ToUpper();
            reservation.CashierName = User.Identity.Name;
            reservation.CashReceivedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cash payment received by {User.Identity.Name}! OR #{receiptNumber} recorded for {reservation.ReservationNo}. Amount: ₱{reservation.TotalAmount:N2}";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while processing the cash payment.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPaymentWithReference(int reservationId, string referenceNumber)
    {
        try
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Reservation not found.";
                return RedirectToAction("Index");
            }

            if (reservation.PaymentStatus == "Paid")
            {
                TempData["ErrorMessage"] = "Payment is already confirmed.";
                return RedirectToAction("Index");
            }

            if (reservation.PaymentMethod == "Cash")
            {
                TempData["ErrorMessage"] = "Use 'Receive Cash' for cash payments to enter receipt number.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(referenceNumber))
            {
                TempData["ErrorMessage"] = "Reference number is required.";
                return RedirectToAction("Index");
            }

            reservation.PaymentStatus = "Paid";
            reservation.PaymentDate = DateTime.Now;
            reservation.ReferenceNumber = referenceNumber.Trim();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Online payment confirmed! Reference #{referenceNumber} recorded for reservation {reservation.ReservationNo}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while confirming the payment.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkReceiveCash(int[] reservationIds, string[] receiptNumbers)
    {
        try
        {
            if (reservationIds == null || reservationIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No reservations selected.";
                return RedirectToAction("Index");
            }

            int successCount = 0;
            for (int i = 0; i < reservationIds.Length; i++)
            {
                var reservation = await _context.Reservations.FindAsync(reservationIds[i]);
                if (reservation != null && reservation.PaymentStatus == "Pending" && reservation.PaymentMethod == "Cash")
                {
                    reservation.PaymentStatus = "Paid";
                    reservation.PaymentDate = DateTime.Now;
                    reservation.ReceiptNumber = receiptNumbers[i]?.Trim().ToUpper();
                    successCount++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully processed {successCount} cash payments.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while processing bulk payments.";
        }

        return RedirectToAction("Index");
    }
}