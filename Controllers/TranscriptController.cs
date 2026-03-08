using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;

namespace PWCEPortal.Controllers;

[Authorize]
public class TranscriptController : Controller
{
    private readonly ITranscriptService _transcriptService;
    private readonly PortalDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TranscriptController(ITranscriptService transcriptService, PortalDbContext context, UserManager<ApplicationUser> userManager)
    {
        _transcriptService = transcriptService;
        _context = context;
        _userManager = userManager;
    }

    // ── Student: generate own unofficial transcript ──────────────────────────

    [Authorize(Policy = Permissions.Transcripts.GenerateOwn)]
    public async Task<IActionResult> MyTranscript()
    {
        var user = await _userManager.GetUserAsync(User);
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == user!.Email);
        if (student is null)
        {
            TempData["ErrorMessage"] = "Student profile not found.";
            return RedirectToAction("Index", "StudentDashboard");
        }
        return View(student);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Transcripts.GenerateOwn)]
    public async Task<IActionResult> DownloadOwnTranscript(string purpose)
    {
        var user = await _userManager.GetUserAsync(User);
        var student = await _context.Students
            .Include(s => s.CollegeProgram)
            .FirstOrDefaultAsync(s => s.Email == user!.Email);
        if (student is null) return NotFound();

        var bytes = await _transcriptService.GenerateTranscriptPdfAsync(student.Id, TranscriptType.Unofficial, user!.Id, purpose);
        var safeName = SanitizeName($"{student.Surname}{student.OtherNames}");
        var fileName = $"Unofficial_Transcript_{safeName}_{student.StudentID}_{DateTime.UtcNow:yyyy-MM-dd}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    // ── Staff: generate official transcript ─────────────────────────────────

    [Authorize(Policy = Permissions.Transcripts.GenerateOfficial)]
    public async Task<IActionResult> Generate()
    {
        ViewBag.Students = await _context.Students
            .Where(s => s.IsDeleted != true)
            .OrderBy(s => s.Surname)
            .ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Transcripts.GenerateOfficial)]
    public async Task<IActionResult> Generate(Guid studentId, string purpose)
    {
        var user = await _userManager.GetUserAsync(User);
        var student = await _context.Students
            .Include(s => s.CollegeProgram)
            .FirstOrDefaultAsync(s => s.Id == studentId);
        if (student is null) return NotFound();

        var bytes = await _transcriptService.GenerateTranscriptPdfAsync(studentId, TranscriptType.Official, user!.Id, purpose);
        var safeName = SanitizeName($"{student.Surname}{student.OtherNames}");
        var fileName = $"Official_Transcript_{safeName}_{student.StudentID}_{DateTime.UtcNow:yyyy-MM-dd}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    // ── Transcript log ───────────────────────────────────────────────────────

    [Authorize(Policy = Permissions.Transcripts.ViewLog)]
    public async Task<IActionResult> Log()
    {
        var logs = await _transcriptService.GetTranscriptLogsAsync();
        return View(logs);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string SanitizeName(string name) =>
        Regex.Replace(name.Trim(), @"[^A-Za-z0-9]", "");
}
