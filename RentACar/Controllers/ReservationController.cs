namespace RentACar.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentACar.Data;
using RentACar.Models;

/// <summary>
/// Контролер за управление на резервации.
/// Предоставя действия за:
/// - преглед на собствени резервации (My),
/// - преглед на всички резервации за администратори (All),
/// - създаване на нова резервация (Create),
/// - изтриване на резервация (Delete).
/// Използва `ApplicationDbContext` за достъп до данни и прилага авторизация.
/// </summary>
[Authorize]
public class ReservationController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReservationController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===========================
    // MY RESERVATIONS
    // ===========================

    public async Task<IActionResult> My()
    {
        // Взимаме Username от login
        var username = User.Identity?.Name;

        if (username == null)
        {
            return Challenge();
        }

        // Намираме нашия User от таблицата
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null)
        {
            return NotFound();
        }

        var reservations = await _context.Reservations
            .Where(r => r.UserId == user.Id)
            .Include(r => r.Car)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync();

        return View(reservations);
    }

    // ===========================
    // ALL (Admin)
    // ===========================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> All()
    {
        var reservations = await _context.Reservations
            .Include(r => r.Car)
            .Include(r => r.User)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync();

        return View(reservations);
    }

    // ===========================
    // CREATE
    // ===========================

    [HttpGet]
    public IActionResult Create(string carId)
    {
        var reservation = new Reservation
        {
            CarId = carId // 🔥 ТОВА Е КЛЮЧОВО
        };

        return View(reservation);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Reservation model)
    {
        // ===========================
        // СТЪПКА 1: ВАЛИДАЦИЯ
        // ===========================

        //if (!ModelState.IsValid)
        //{
        //    return View(model);
        //}

        // ===========================
        // СТЪПКА 2: ДАТИ
        // ===========================

        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError("EndDate",
                "Крайната дата трябва да е след началната.");
            return View(model);
        }

        // ===========================
        // СТЪПКА 3: ВЗИМАМЕ USER
        // ===========================

        var username = User.Identity?.Name;

        if (username == null)
        {
            return Challenge();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null)
        {
            return NotFound();
        }

        // ===========================
        // СТЪПКА 4: ПРОВЕРКА ЗА ЗАЕТОСТ
        // ===========================

        bool isBusy = await _context.Reservations
            .AnyAsync(r =>
                r.CarId == model.CarId &&
                r.IsReserved &&
                (
                    model.StartDate < r.EndDate &&
                    model.EndDate > r.StartDate
                )
            );

        if (isBusy)
        {
            ModelState.AddModelError("",
                "Колата вече е заета за този период!");
            return View(model);
        }

        // ===========================
        // СТЪПКА 5: СЪЗДАВАНЕ
        // ===========================

        var reservation = new Reservation
        {
            CarId = model.CarId,
            UserId = user.Id,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            IsReserved = true
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(My));
    }

    // ===========================
    // DELETE (Admin)
    // ===========================

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var reservation = await _context.Reservations.FindAsync(id);

        if (reservation == null)
        {
            return NotFound();
        }
        reservation.IsDeleted = true;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(All));
    }
}
