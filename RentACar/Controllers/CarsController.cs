namespace RentACar.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentACar.Data;
using RentACar.Models;

/// <summary>
/// Контролер за управление на автомобили
/// Отговаря за:
/// - Показване на всички коли
/// - Създаване на нова кола (Admin)
/// - Редакция (Admin)
/// - Изтриване (Admin)
/// </summary>
[Authorize]
public class CarsController : Controller
{
    // ====================
    // DEPENDENCY INJECTION
    // ====================

    private readonly ApplicationDbContext _context;

    public CarsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===========================
    // ALL CARS (Всички автомобили)
    // ===========================

    /// <summary>
    /// GET: /Cars/All
    /// Показва всички коли
    /// </summary>
    public async Task<IActionResult> All()
    {
        var cars = await _context.Cars
            .OrderBy(c => c.Brand)
            .ThenBy(c => c.Model)
            .ToListAsync();

        return View(cars);
    }

    // ===========================
    // DETAILS (Детайли)
    // ===========================

    public async Task<IActionResult> Details(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var car = await _context.Cars.FindAsync(id);

        if (car == null)
        {
            return NotFound();
        }

        return View(car);
    }

    // ===========================
    // CREATE CAR (Admin only)
    // ===========================

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Car car)
    {
        // СТЪПКА 1: Валидация
        if (!ModelState.IsValid)
        {
            return View(car);
        }

        // СТЪПКА 2: Добавяне в базата
        _context.Cars.Add(car);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(All));
    }

    // ===========================
    // EDIT CAR (Admin only)
    // ===========================

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var car = await _context.Cars.FindAsync(id);

        if (car == null)
        {
            return NotFound();
        }

        return View(car);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(string id, Car car)
    {
        if (id != car.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(car);
        }

        try
        {
            _context.Update(car);
            await _context.SaveChangesAsync();
        }
        catch
        {
            return NotFound();
        }

        return RedirectToAction(nameof(All));
    }

    // ===========================
    // DELETE CAR (Admin only)
    // ===========================

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null) return NotFound();

        var car = await _context.Cars.FindAsync(id);

        if (car == null) return NotFound();

        return View(car);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var car = await _context.Cars.FindAsync(id);

        if (car == null) return NotFound();

        car.IsDeleted = true;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(All));
    }
}
