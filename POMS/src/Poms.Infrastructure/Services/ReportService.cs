// ============================================================================
// Poms.Infrastructure/Services/ReportService.cs
// ============================================================================
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Poms.Infrastructure.Services;

public interface IReportService
{
    Task<byte[]> GeneratePatientRegistrationReportPdfAsync(DateOnly startDate, DateOnly endDate, int? centerId = null);
    Task<byte[]> GeneratePatientRegistrationReportExcelAsync(DateOnly startDate, DateOnly endDate, int? centerId = null);
    Task<byte[]> GenerateEpisodeSummaryReportPdfAsync(DateOnly startDate, DateOnly endDate, EpisodeType? type = null);
    Task<byte[]> GenerateEpisodeSummaryReportExcelAsync(DateOnly startDate, DateOnly endDate, EpisodeType? type = null);
    Task<byte[]> GenerateDeliveryReportPdfAsync(DateOnly startDate, DateOnly endDate, int? centerId = null);
    Task<byte[]> GenerateDeliveryReportExcelAsync(DateOnly startDate, DateOnly endDate, int? centerId = null);
    Task<byte[]> GenerateFollowUpReportPdfAsync(DateOnly startDate, DateOnly endDate);
    Task<byte[]> GenerateFollowUpReportExcelAsync(DateOnly startDate, DateOnly endDate);
    Task<byte[]> GeneratePatientListPdfAsync(int? centerId = null, bool? isActive = null);
    Task<byte[]> GeneratePatientListExcelAsync(int? centerId = null, bool? isActive = null);
}

public class ReportService : IReportService
{
    private readonly PomsDbContext _context;

    public ReportService(PomsDbContext context)
    {
        _context = context;
        // Configure QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    #region Patient Registration Report

    public async Task<byte[]> GeneratePatientRegistrationReportPdfAsync(DateOnly startDate, DateOnly endDate, int? centerId = null)
    {
        var patients = await GetPatientRegistrations(startDate, endDate, centerId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Patient Registration Report", startDate, endDate));
                page.Content().Element(c => ComposePatientRegistrationContent(c, patients));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GeneratePatientRegistrationReportExcelAsync(DateOnly startDate, DateOnly endDate, int? centerId = null)
    {
        var patients = await GetPatientRegistrations(startDate, endDate, centerId);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Patient Registrations");

        // Header
        var headers = new[] { "Patient #", "Name", "DOB", "Sex", "NIC", "Phone", "Center", "Registration Date" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Data
        int row = 2;
        foreach (var p in patients)
        {
            worksheet.Cell(row, 1).Value = p.PatientNumber;
            worksheet.Cell(row, 2).Value = $"{p.FirstName} {p.LastName}";
            worksheet.Cell(row, 3).Value = p.Dob?.ToString("yyyy-MM-dd") ?? "";
            worksheet.Cell(row, 4).Value = p.Sex.ToString();
            worksheet.Cell(row, 5).Value = p.NationalId ?? "";
            worksheet.Cell(row, 6).Value = p.Phone1 ?? "";
            worksheet.Cell(row, 7).Value = p.Center.Name;
            worksheet.Cell(row, 8).Value = p.RegistrationDate.ToString("yyyy-MM-dd");
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<List<Patient>> GetPatientRegistrations(DateOnly startDate, DateOnly endDate, int? centerId)
    {
        var query = _context.Patients
            .Include(p => p.Center)
            .Where(p => p.RegistrationDate >= startDate && p.RegistrationDate <= endDate);

        if (centerId.HasValue)
            query = query.Where(p => p.CenterId == centerId.Value);

        return await query.OrderBy(p => p.RegistrationDate).ToListAsync();
    }

    #endregion

    #region Episode Summary Report

    public async Task<byte[]> GenerateEpisodeSummaryReportPdfAsync(DateOnly startDate, DateOnly endDate, EpisodeType? type = null)
    {
        var episodes = await GetEpisodes(startDate, endDate, type);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Episode Summary Report", startDate, endDate));
                page.Content().Element(c => ComposeEpisodeContent(c, episodes));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateEpisodeSummaryReportExcelAsync(DateOnly startDate, DateOnly endDate, EpisodeType? type = null)
    {
        var episodes = await GetEpisodes(startDate, endDate, type);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Episodes");

        var headers = new[] { "Patient #", "Patient Name", "Type", "Opened", "Closed", "Status", "Remarks" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
        }

        int row = 2;
        foreach (var e in episodes)
        {
            worksheet.Cell(row, 1).Value = e.Patient.PatientNumber;
            worksheet.Cell(row, 2).Value = $"{e.Patient.FirstName} {e.Patient.LastName}";
            worksheet.Cell(row, 3).Value = e.Type.ToString();
            worksheet.Cell(row, 4).Value = e.OpenedOn.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 5).Value = e.ClosedOn?.ToString("yyyy-MM-dd") ?? "";
            worksheet.Cell(row, 6).Value = e.ClosedOn.HasValue ? "Closed" : "Open";
            worksheet.Cell(row, 7).Value = e.Remarks ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<List<Episode>> GetEpisodes(DateOnly startDate, DateOnly endDate, EpisodeType? type)
    {
        var query = _context.Episodes
            .Include(e => e.Patient)
            .Where(e => e.OpenedOn >= startDate && e.OpenedOn <= endDate);

        if (type.HasValue)
            query = query.Where(e => e.Type == type.Value);

        return await query.OrderBy(e => e.OpenedOn).ToListAsync();
    }

    #endregion

    #region Delivery Report

    public async Task<byte[]> GenerateDeliveryReportPdfAsync(DateOnly startDate, DateOnly endDate, int? centerId = null)
    {
        var deliveries = await GetDeliveries(startDate, endDate, centerId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Delivery Report", startDate, endDate));
                page.Content().Element(c => ComposeDeliveryContent(c, deliveries));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateDeliveryReportExcelAsync(DateOnly startDate, DateOnly endDate, int? centerId = null)
    {
        var deliveries = await GetDeliveries(startDate, endDate, centerId);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Deliveries");

        var headers = new[] { "Patient #", "Patient Name", "Episode Type", "Delivery Date", "Serial Number", "Delivered By", "Remarks" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightCoral;
        }

        int row = 2;
        foreach (var d in deliveries)
        {
            worksheet.Cell(row, 1).Value = d.Episode.Patient.PatientNumber;
            worksheet.Cell(row, 2).Value = $"{d.Episode.Patient.FirstName} {d.Episode.Patient.LastName}";
            worksheet.Cell(row, 3).Value = d.Episode.Type.ToString();
            worksheet.Cell(row, 4).Value = d.DeliveryDate?.ToString("yyyy-MM-dd") ?? "";
            worksheet.Cell(row, 5).Value = d.SerialNumber ?? "";
            worksheet.Cell(row, 6).Value = d.DeliveredBy ?? "";
            worksheet.Cell(row, 7).Value = d.Remarks ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<List<Delivery>> GetDeliveries(DateOnly startDate, DateOnly endDate, int? centerId)
    {
        var query = _context.Deliveries
            .Include(d => d.Episode)
            .ThenInclude(e => e.Patient)
            .Where(d => d.DeliveryDate >= startDate && d.DeliveryDate <= endDate);

        if (centerId.HasValue)
            query = query.Where(d => d.Episode.Patient.CenterId == centerId.Value);

        return await query.OrderBy(d => d.DeliveryDate).ToListAsync();
    }

    #endregion

    #region Follow-up Report

    public async Task<byte[]> GenerateFollowUpReportPdfAsync(DateOnly startDate, DateOnly endDate)
    {
        var followUps = await GetFollowUps(startDate, endDate);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Follow-up Report", startDate, endDate));
                page.Content().Element(c => ComposeFollowUpContent(c, followUps));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateFollowUpReportExcelAsync(DateOnly startDate, DateOnly endDate)
    {
        var followUps = await GetFollowUps(startDate, endDate);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Follow-ups");

        var headers = new[] { "Patient #", "Patient Name", "Follow-up Date", "Action Taken", "Next Appointment", "Next Plan" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
        }

        int row = 2;
        foreach (var f in followUps)
        {
            worksheet.Cell(row, 1).Value = f.Episode.Patient.PatientNumber;
            worksheet.Cell(row, 2).Value = $"{f.Episode.Patient.FirstName} {f.Episode.Patient.LastName}";
            worksheet.Cell(row, 3).Value = f.FollowUpDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 4).Value = f.ActionTaken ?? "";
            worksheet.Cell(row, 5).Value = f.NextAppointmentDate?.ToString("yyyy-MM-dd") ?? "";
            worksheet.Cell(row, 6).Value = f.NextPlan ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<List<FollowUp>> GetFollowUps(DateOnly startDate, DateOnly endDate)
    {
        return await _context.FollowUps
            .Include(f => f.Episode)
            .ThenInclude(e => e.Patient)
            .Where(f => f.FollowUpDate >= startDate && f.FollowUpDate <= endDate)
            .OrderBy(f => f.FollowUpDate)
            .ToListAsync();
    }

    #endregion

    #region Patient List Report

    public async Task<byte[]> GeneratePatientListPdfAsync(int? centerId = null, bool? isActive = null)
    {
        var patients = await GetPatientList(centerId, isActive);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeSimpleHeader(c, "Patient List"));
                page.Content().Element(c => ComposePatientListContent(c, patients));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GeneratePatientListExcelAsync(int? centerId = null, bool? isActive = null)
    {
        var patients = await GetPatientList(centerId, isActive);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Patients");

        var headers = new[] { "Patient #", "Name", "DOB", "Sex", "NIC", "Phone", "Center", "Status" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        int row = 2;
        foreach (var p in patients)
        {
            worksheet.Cell(row, 1).Value = p.PatientNumber;
            worksheet.Cell(row, 2).Value = $"{p.FirstName} {p.LastName}";
            worksheet.Cell(row, 3).Value = p.Dob?.ToString("yyyy-MM-dd") ?? "";
            worksheet.Cell(row, 4).Value = p.Sex.ToString();
            worksheet.Cell(row, 5).Value = p.NationalId ?? "";
            worksheet.Cell(row, 6).Value = p.Phone1 ?? "";
            worksheet.Cell(row, 7).Value = p.Center.Name;
            worksheet.Cell(row, 8).Value = p.IsActive ? "Active" : "Inactive";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<List<Patient>> GetPatientList(int? centerId, bool? isActive)
    {
        var query = _context.Patients.Include(p => p.Center).AsQueryable();

        if (centerId.HasValue)
            query = query.Where(p => p.CenterId == centerId.Value);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        return await query.OrderBy(p => p.PatientNumber).ToListAsync();
    }

    #endregion

    #region PDF Composition Helpers

    private void ComposeHeader(IContainer container, string title, DateOnly startDate, DateOnly endDate)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("POMS - Prosthetic & Orthotic Management System")
                        .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text(title).FontSize(18).Bold();
                    col.Item().Text($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}").FontSize(10);
                });
                row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
            });
            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    private void ComposeSimpleHeader(IContainer container, string title)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("POMS - Prosthetic & Orthotic Management System")
                        .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text(title).FontSize(18).Bold();
                });
                row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
            });
            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }

    private void ComposePatientRegistrationContent(IContainer container, List<Patient> patients)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2); // Patient #
                columns.RelativeColumn(3); // Name
                columns.RelativeColumn(2); // DOB
                columns.RelativeColumn(1); // Sex
                columns.RelativeColumn(2); // NIC
                columns.RelativeColumn(2); // Phone
                columns.RelativeColumn(3); // Center
                columns.RelativeColumn(2); // Reg Date
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Patient #").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Name").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("DOB").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Sex").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("NIC").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Phone").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Center").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Reg Date").Bold();
            });

            foreach (var p in patients)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.PatientNumber);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{p.FirstName} {p.LastName}");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.Dob?.ToString("yyyy-MM-dd") ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.Sex.ToString());
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.NationalId ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.Phone1 ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.Center.Name);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.RegistrationDate.ToString("yyyy-MM-dd"));
            }
        });
    }

    private void ComposeEpisodeContent(IContainer container, List<Episode> episodes)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
                columns.RelativeColumn(3);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Patient #").Bold();
                header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Patient Name").Bold();
                header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Type").Bold();
                header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Opened").Bold();
                header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Closed").Bold();
                header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Status").Bold();
                header.Cell().Background(Colors.Green.Lighten3).Padding(5).Text("Remarks").Bold();
            });

            foreach (var e in episodes)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.Patient.PatientNumber);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{e.Patient.FirstName} {e.Patient.LastName}");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.Type.ToString());
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.OpenedOn.ToString("yyyy-MM-dd"));
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.ClosedOn?.ToString("yyyy-MM-dd") ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.ClosedOn.HasValue ? "Closed" : "Open");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.Remarks ?? "-");
            }
        });
    }

    private void ComposeDeliveryContent(IContainer container, List<Delivery> deliveries)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Patient #").Bold();
                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Patient Name").Bold();
                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Episode Type").Bold();
                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Delivery Date").Bold();
                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Serial Number").Bold();
                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Delivered By").Bold();
                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Remarks").Bold();
            });

            foreach (var d in deliveries)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(d.Episode.Patient.PatientNumber);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{d.Episode.Patient.FirstName} {d.Episode.Patient.LastName}");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(d.Episode.Type.ToString());
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(d.DeliveryDate?.ToString("yyyy-MM-dd") ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(d.SerialNumber ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(d.DeliveredBy ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(d.Remarks ?? "-");
            }
        });
    }

    private void ComposeFollowUpContent(IContainer container, List<FollowUp> followUps)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Yellow.Lighten3).Padding(5).Text("Patient #").Bold();
                header.Cell().Background(Colors.Yellow.Lighten3).Padding(5).Text("Patient Name").Bold();
                header.Cell().Background(Colors.Yellow.Lighten3).Padding(5).Text("Follow-up Date").Bold();
                header.Cell().Background(Colors.Yellow.Lighten3).Padding(5).Text("Action Taken").Bold();
                header.Cell().Background(Colors.Yellow.Lighten3).Padding(5).Text("Next Appointment").Bold();
                header.Cell().Background(Colors.Yellow.Lighten3).Padding(5).Text("Next Plan").Bold();
            });

            foreach (var f in followUps)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(f.Episode.Patient.PatientNumber);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{f.Episode.Patient.FirstName} {f.Episode.Patient.LastName}");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(f.FollowUpDate.ToString("yyyy-MM-dd"));
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(f.ActionTaken ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(f.NextAppointmentDate?.ToString("yyyy-MM-dd") ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(f.NextPlan ?? "-");
            }
        });
    }

    private void ComposePatientListContent(IContainer container, List<Patient> patients)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Patient #").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Name").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("DOB").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Sex").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("NIC").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Phone").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Center").Bold();
                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Status").Bold();
            });

            foreach (var p in patients)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.PatientNumber);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{p.FirstName} {p.LastName}");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.Dob?.ToString("yyyy-MM-dd") ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.Sex.ToString());
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.NationalId ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.Phone1 ?? "-");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.Center.Name);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                    .Text(p.IsActive ? "Active" : "Inactive")
                    .FontColor(p.IsActive ? Colors.Green.Darken2 : Colors.Red.Darken2);
            }
        });
    }

    #endregion
}
