using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;

namespace Poms.Web.Controllers;

[Authorize(Policy = "ClinicianOrAdmin")]
public class ConditionsController : Controller
{
    private readonly PomsDbContext _context;
    private readonly ILogger<ConditionsController> _logger;

    public ConditionsController(PomsDbContext context, ILogger<ConditionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Conditions
    public async Task<IActionResult> Index(string searchString)
    {
        var query = _context.Conditions.AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(c =>
                c.Code.Contains(searchString) ||
                c.Name.Contains(searchString) ||
                (c.Description != null && c.Description.Contains(searchString)));
        }

        var conditions = await query
            .OrderBy(c => c.Code)
            .ToListAsync();

        ViewBag.SearchString = searchString;
        return View(conditions);
    }

    // GET: Conditions/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var condition = await _context.Conditions
            .FirstOrDefaultAsync(m => m.Id == id);

        if (condition == null) return NotFound();

        return View(condition);
    }

    // GET: Conditions/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Conditions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Condition condition)
    {
        if (ModelState.IsValid)
        {
            try
            {
                condition.CreatedBy = User.Identity?.Name;
                _context.Add(condition);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Condition {Code} created by {User}",
                    condition.Code, User.Identity?.Name);

                TempData["Success"] = "Condition created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating condition");
                ModelState.AddModelError("", "An error occurred while creating the condition.");
            }
        }

        return View(condition);
    }

    // GET: Conditions/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var condition = await _context.Conditions.FindAsync(id);
        if (condition == null) return NotFound();

        return View(condition);
    }

    // POST: Conditions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Condition condition)
    {
        if (id != condition.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                condition.UpdatedBy = User.Identity?.Name;
                condition.UpdatedAt = DateTime.UtcNow;
                _context.Update(condition);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Condition updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ConditionExists(condition.Id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating condition {ConditionId}", id);
                ModelState.AddModelError("", "An error occurred while updating the condition.");
            }
        }

        return View(condition);
    }

    // GET: Conditions/Delete/5
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var condition = await _context.Conditions
            .FirstOrDefaultAsync(m => m.Id == id);

        if (condition == null) return NotFound();

        return View(condition);
    }

    // POST: Conditions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var condition = await _context.Conditions.FindAsync(id);
        if (condition != null)
        {
            _context.Conditions.Remove(condition);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Condition {Code} deleted by {User}",
                condition.Code, User.Identity?.Name);
        }

        TempData["Success"] = "Condition deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    private bool ConditionExists(int id)
    {
        return _context.Conditions.Any(e => e.Id == id);
    }
}