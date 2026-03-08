using Microsoft.EntityFrameworkCore;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Models.Academic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PWCEPortal.Services;

public class TranscriptService : ITranscriptService
{
    private readonly PortalDbContext _context;
    private readonly IGPAService _gpaService;

    public TranscriptService(PortalDbContext context, IGPAService gpaService)
    {
        _context = context;
        _gpaService = gpaService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateTranscriptPdfAsync(
        Guid studentId, TranscriptType type, string requestedByUserId, string? purpose = null)
    {
        var student = await _context.Students
            .Include(s => s.CollegeProgram)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        var semesterResults = await _context.SemesterResults
            .Include(r => r.AcademicSemester).ThenInclude(s => s!.AcademicYear)
            .Where(r => r.StudentId == studentId && r.IsPublished && r.IsDeleted != true)
            .OrderBy(r => r.AcademicSemester!.AcademicYear!.Year).ThenBy(r => r.AcademicSemester!.SemesterName)
            .ToListAsync();

        var latestCumulative = await _context.CumulativeResults
            .Include(r => r.AcademicYear)
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.ComputedAt)
            .FirstOrDefaultAsync();

        var courseResultsBySemester = await _gpaService.GetCourseResultsBySemesterAsync(studentId);

        bool isUnofficial = type == TranscriptType.Unofficial;
        string studentName = student != null ? $"{student.Surname} {student.OtherNames}".Trim() : "Unknown";

        // Compute running cumulative GPA for each semester
        decimal runningWeightedGP = 0;
        int runningCredits = 0;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9));

                // Watermark layer for unofficial transcripts
                if (isUnofficial)
                {
                    page.Background().Canvas((canvas, size) =>
                    {
                        using var paint = new SkiaSharp.SKPaint
                        {
                            Color = SkiaSharp.SKColors.LightGray.WithAlpha(80),
                            TextSize = 72,
                            IsAntialias = true,
                            FakeBoldText = true,
                            TextAlign = SkiaSharp.SKTextAlign.Center
                        };
                        canvas.Save();
                        canvas.Translate(size.Width / 2, size.Height / 2);
                        canvas.RotateDegrees(-45);
                        canvas.DrawText("UNOFFICIAL", 0, 0, paint);
                        canvas.Restore();
                    });
                }

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("PWCE SCHOOL PORTAL").Bold().FontSize(16);
                    col.Item().AlignCenter().Text("OFFICIAL ACADEMIC TRANSCRIPT").Bold().FontSize(12);
                    if (isUnofficial)
                        col.Item().AlignCenter().Text("— UNOFFICIAL COPY —").FontColor(Colors.Red.Medium).Bold().FontSize(10);
                    col.Item().LineHorizontal(1).LineColor(Colors.Black);
                    col.Item().PaddingTop(6).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        void Row(string label, string value)
                        {
                            t.Cell().Text(label).SemiBold();
                            t.Cell().Text(value);
                        }
                        Row("Student Name:", studentName);
                        Row("Student ID:", student?.StudentID ?? "-");
                        Row("Programme:", student?.CollegeProgram?.ProgramName ?? "-");
                        Row("Level of Entry:", student?.LevelOfEntry?.ToString() ?? "-");
                        Row("Enrolment Year:", student?.EnrolmentYear ?? "-");
                        Row("Generated:", DateTime.UtcNow.ToString("dd MMM yyyy HH:mm") + " UTC");
                        if (!string.IsNullOrEmpty(purpose))
                            Row("Purpose:", purpose);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Black);
                });

                page.Content().PaddingTop(8).Column(content =>
                {
                    var byYear = semesterResults
                        .GroupBy(r => r.AcademicSemester?.AcademicYear?.Year ?? "Unknown")
                        .OrderBy(g => g.Key);

                    foreach (var yearGroup in byYear)
                    {
                        content.Item().PaddingTop(6).Text($"ACADEMIC YEAR: {yearGroup.Key}").Bold().FontSize(11);
                        content.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                        foreach (var sem in yearGroup)
                        {
                            var courses = courseResultsBySemester.TryGetValue(sem.AcademicSemesterId, out var cl)
                                ? cl : new List<ViewModel.Results.CourseResultViewModel>();

                            decimal semQP = courses.Sum(c => c.GradePoint * c.CreditHours);
                            runningWeightedGP += semQP;
                            runningCredits += sem.TotalCreditHours;
                            decimal runningCGPA = runningCredits > 0
                                ? Math.Round(runningWeightedGP / runningCredits, 2) : 0;

                            content.Item().PaddingTop(4).Text($"Semester: {sem.AcademicSemester?.SemesterName}").SemiBold();

                            content.Item().PaddingTop(2).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(60);  // Code
                                    c.RelativeColumn(3);   // Course Name
                                    c.ConstantColumn(30);  // Credits
                                    c.ConstantColumn(45);  // Score%
                                    c.ConstantColumn(30);  // Grade
                                    c.ConstantColumn(35);  // GP
                                    c.ConstantColumn(40);  // QP
                                });

                                // Header
                                static IContainer HeaderCell(IContainer c) =>
                                    c.Background(Colors.Grey.Lighten3).Padding(3);

                                t.Header(h =>
                                {
                                    h.Cell().Element(HeaderCell).Text("Code").SemiBold();
                                    h.Cell().Element(HeaderCell).Text("Course Name").SemiBold();
                                    h.Cell().Element(HeaderCell).AlignCenter().Text("Cr").SemiBold();
                                    h.Cell().Element(HeaderCell).AlignCenter().Text("Score%").SemiBold();
                                    h.Cell().Element(HeaderCell).AlignCenter().Text("Grd").SemiBold();
                                    h.Cell().Element(HeaderCell).AlignCenter().Text("GP").SemiBold();
                                    h.Cell().Element(HeaderCell).AlignCenter().Text("QP").SemiBold();
                                });

                                static IContainer DataCell(IContainer c) =>
                                    c.BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3);

                                foreach (var course in courses)
                                {
                                    decimal qp = course.GradePoint * course.CreditHours;
                                    t.Cell().Element(DataCell).Text(course.CourseCode);
                                    t.Cell().Element(DataCell).Text(course.CourseName);
                                    t.Cell().Element(DataCell).AlignCenter().Text(course.CreditHours.ToString());
                                    t.Cell().Element(DataCell).AlignCenter().Text(course.TotalScore.ToString("F1"));
                                    t.Cell().Element(DataCell).AlignCenter().Text(course.GradeLetter).Bold();
                                    t.Cell().Element(DataCell).AlignCenter().Text(course.GradePoint.ToString("F2"));
                                    t.Cell().Element(DataCell).AlignCenter().Text(qp.ToString("F2"));
                                }

                                // Totals row
                                static IContainer TotalCell(IContainer c) =>
                                    c.Background(Colors.Grey.Lighten4).Padding(3);

                                t.Cell().ColumnSpan(2).Element(TotalCell).AlignRight().Text("Semester Total:").SemiBold();
                                t.Cell().Element(TotalCell).AlignCenter().Text(sem.TotalCreditHours.ToString()).SemiBold();
                                t.Cell().Element(TotalCell).Text(string.Empty);
                                t.Cell().Element(TotalCell).Text(string.Empty);
                                t.Cell().Element(TotalCell).Text(string.Empty);
                                t.Cell().Element(TotalCell).AlignCenter().Text(semQP.ToString("F2")).SemiBold();
                            });

                            content.Item().PaddingLeft(4).Row(row =>
                            {
                                row.AutoItem().Text("Semester GPA: ").SemiBold();
                                row.AutoItem().Text(sem.GPA.ToString("F2")).FontColor(Colors.Blue.Darken2).SemiBold();
                                row.ConstantItem(20);
                                row.AutoItem().Text("Cumulative GPA: ").SemiBold();
                                row.AutoItem().Text(runningCGPA.ToString("F2")).FontColor(Colors.Blue.Darken2).SemiBold();
                            });

                            content.Item().PaddingBottom(4);
                        }
                    }

                    // Summary
                    content.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Black);
                    content.Item().PaddingTop(4).Text("ACADEMIC SUMMARY").Bold().FontSize(11);
                    content.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(); });
                        void Row(string lbl, string val)
                        {
                            t.Cell().Padding(3).Text(lbl).SemiBold();
                            t.Cell().Padding(3).Text(val);
                        }
                        Row("Total Credit Hours Attempted:",
                            (latestCumulative?.TotalCreditHoursAttempted ?? runningCredits).ToString());
                        Row("Total Credit Hours Earned:",
                            (latestCumulative?.TotalCreditHoursEarned ?? runningCredits).ToString());
                        decimal finalCGPA = latestCumulative?.CGPA ??
                            (runningCredits > 0 ? Math.Round(runningWeightedGP / runningCredits, 2) : 0);
                        Row("Cumulative GPA:", finalCGPA.ToString("F2"));
                        Row("Classification:", latestCumulative?.Classification ?? "-");
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                    t.Span($"  |  {(isUnofficial ? "UNOFFICIAL" : "OFFICIAL")} TRANSCRIPT  |  PWCE School Portal");
                    t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Medium));
                });
            });
        }).GeneratePdf();

        // Log the request
        _context.TranscriptRequests.Add(new TranscriptRequest
        {
            StudentId = studentId,
            TranscriptType = type,
            RequestedById = requestedByUserId,
            RequestedAt = DateTime.UtcNow,
            Purpose = purpose
        });
        await _context.SaveChangesAsync();

        return pdfBytes;
    }

    public async Task<List<TranscriptRequest>> GetTranscriptLogsAsync(Guid? studentId = null)
    {
        var query = _context.TranscriptRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedBy)
            .Where(r => r.IsDeleted != true);

        if (studentId.HasValue)
            query = query.Where(r => r.StudentId == studentId.Value);

        return await query.OrderByDescending(r => r.RequestedAt).ToListAsync();
    }

    public async Task<TranscriptRequest?> GetRequestByIdAsync(Guid id) =>
        await _context.TranscriptRequests
            .Include(r => r.Student)
            .Include(r => r.RequestedBy)
            .FirstOrDefaultAsync(r => r.Id == id);
}
