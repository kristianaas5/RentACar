namespace RentACar.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentACar.Data;
using RentACar.Models;

/// <summary>
/// Controller for managing cars.
/// Responsibilities:
/// - Show all cars
/// - Create a new car (Admin)
/// - Edit a car (Admin)
/// - Soft-delete / restore cars (Admin)
/// </summary>
[Authorize]
public class CarsController : Controller
{
    // ====================
    // DEPENDENCY INJECTION
    // ====================

    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initialize a new instance of <see cref="CarsController"/>.
    /// </summary>
    /// <param name="context">Application database context.</param>
    public CarsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===========================
    // ALL CARS (Всички автомобили)
    // ===========================

    /// <summary>
    /// GET: /Cars/All
    /// Returns a view with all non-deleted cars ordered by brand and model.
    /// </summary>
    /// <returns>A view result containing the list of cars.</returns>
    public async Task<IActionResult> All()
    {
        var cars = await _context.Cars
            .OrderBy(c => c.Brand)
            .ThenBy(c => c.Model)
            .ToListAsync();

        return View(cars);
    }

    /// <summary>
    /// Shows soft-deleted cars for administration.
    /// </summary>
    /// <param name="query">Optional query string (unused currently).</param>
    /// <returns>A view with deleted cars.</returns>
    public async Task<IActionResult> AllDelete(string query)
    {
        var authorsQuery = _context.Cars
            .IgnoreQueryFilters() // Include deleted authors
            .Where(a => a.IsDeleted) // Only deleted authors
            .OrderBy(a => a.Brand);
        var models = await authorsQuery.ToListAsync();
        return View(models);
    }

    /// <summary>
    /// GET: /Cars/Restore/{id}
    /// Returns the restore confirmation view for a soft-deleted car.
    /// </summary>
    /// <param name="id">Car identifier.</param>
    /// <returns>NotFound if id is missing or car not found; otherwise the restore view.</returns>
    [HttpGet]
    public async Task<IActionResult> Restore(string id)
    {
        var author = await _context.Cars.IgnoreQueryFilters()
            .Where(a => a.IsDeleted).FirstOrDefaultAsync(a => a.Id == id);
        if (author == null) return NotFound();
        return View(author);
    }

    /// <summary>
    /// POST: /Cars/Restore
    /// Restores a previously soft-deleted car.
    /// </summary>
    /// <param name="model">Car model containing the Id to restore.</param>
    /// <returns>Redirects to All on success or NotFound if the car does not exist.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(Car model)
    {
        var author = await _context.Cars.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == model.Id);
        if (author == null) return NotFound();

        author.IsDeleted = false;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(All), new { model.Id });
    }

    // ===========================
    // DETAILS (Детайли)
    // ===========================

    /// <summary>
    /// GET: /Cars/Details/{id}
    /// Shows detailed information about a single car.
    /// </summary>
    /// <param name="id">Car identifier.</param>
    /// <returns>NotFound if id missing or car not found; otherwise the details view.</returns>
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

    /// <summary>
    /// GET: /Cars/Create
    /// Returns the create car form. Admin only.
    /// </summary>
    /// <returns>The create view.</returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// POST: /Cars/Create
    /// Persists a new car to the database. Admin only.
    /// </summary>
    /// <param name="car">Car model posted from the form.</param>
    /// <returns>Returns to the create view when model state is invalid; redirects to All on success.</returns>
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

    /// <summary>
    /// GET: /Cars/Edit/{id}
    /// Returns the edit form for a car. Admin only.
    /// </summary>
    /// <param name="id">Car identifier.</param>
    /// <returns>NotFound if id missing or car not found; otherwise the edit view.</returns>
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

    /// <summary>
    /// POST: /Cars/Edit/{id}
    /// Applies changes to an existing car. Admin only.
    /// </summary>
    /// <param name="id">Car id from the route.</param>
    /// <param name="car">Car model with updated values.</param>
    /// <returns>NotFound when ids mismatch or on error; returns edit view when model invalid; redirects to All on success.</returns>
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

    /// <summary>
    /// GET: /Cars/Delete/{id}
    /// Shows delete confirmation for a car. Admin only.
    /// </summary>
    /// <param name="id">Car identifier.</param>
    /// <returns>NotFound if id missing or car not found; otherwise the delete view.</returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null) return NotFound();

        var car = await _context.Cars.FindAsync(id);

        if (car == null) return NotFound();

        return View(car);
    }

    /// <summary>
    /// POST: /Cars/Delete
    /// Performs a soft delete of the car by setting IsDeleted = true. Admin only.
    /// </summary>
    /// <param name="id">Car identifier to delete.</param>
    /// <returns>Redirects to the All action on success.</returns>
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
