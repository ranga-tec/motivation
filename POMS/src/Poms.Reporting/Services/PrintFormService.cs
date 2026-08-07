using Poms.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Poms.Reporting.Services;

public class PrintFormService : IPrintFormService
{
    public byte[] GenerateRegistrationForm(PatientPrintModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    // Header band: logo | title | form no/revision
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(1).Column(c =>
                        {
                            c.Item().Text("EXCEED").Bold().FontSize(16);
                            c.Item().Text("prosthetics & orthotics").FontSize(8);
                        });
                        row.RelativeItem(3).AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("Exceed Lanka Pvt Ltd").Bold().FontSize(14);
                            c.Item().AlignCenter().Text("Patient Registration form").Bold().FontSize(12);
                        });
                        row.RelativeItem(1).Border(1).Padding(4).Column(c =>
                        {
                            c.Item().Text("Form No: 101").FontSize(9);
                            c.Item().Text("Revision: 15/01").FontSize(9);
                        });
                    });

                    // Centre / Patient File No
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => Box(c, "Centre:", model.CentreName));
                        row.RelativeItem().Border(1).Padding(6).Column(c =>
                        {
                            c.Item().Text("Patient File No:").FontSize(9).SemiBold();
                            c.Item().Text(model.PatientNumber).Bold().FontSize(14);
                        });
                    });

                    col.Item().Text("Patient Details").Bold().FontSize(12).Underline();

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Element(c => Box(c, "Full Name:", model.FullName));
                            left.Item().Element(c => Box(c, "Address:", model.Address));
                            left.Item().Element(c => TelephoneTable(c, model.Contacts));
                        });
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Element(c => Box(c, "Date of Birth (DD/MM/YY):", model.Dob.ToString("dd/MM/yy")));
                            right.Item().Element(c => Box(c, "NIC# / PP / DL:", $"{model.IdentificationType}: {model.IdentificationNumber}"));
                            right.Item().Element(c => Box(c, "Gender:", model.Gender));
                            right.Item().Element(c => Box(c, "Employment:", model.Employment));
                            right.Item().Element(c => Box(c, "Province / District / City:", $"{model.Province} / {model.District} / {model.City}"));
                            right.Item().Element(c => Box(c, "Email:", model.Email));
                            right.Item().Element(c => Box(c, "Referral Source:", model.ReferralSource));
                            right.Item().Element(c => Box(c, "Referral Person:", model.ReferralPersonName));
                            right.Item().Element(c => Box(c, "Referral Contact:", model.ReferralPersonContactNumber));
                        });
                    });

                    col.Item().Text("Guardian/Carer Details").Bold().FontSize(12).Underline();

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Element(c => Box(c, "Name / Relationship:", $"{model.GuardianName} / {model.GuardianRelationship}"));
                            left.Item().Element(c => Box(c, "Address:", model.GuardianAddress));
                        });
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Element(c => Box(c, "Phone:", model.GuardianPhone));
                            right.Item().Element(c => Box(c, "Mobile:", model.GuardianMobile));
                        });
                    });

                    col.Item().Element(c => Box(c, "Time/Distance to travel to center:", model.TravelTimeDistance));
                    col.Item().Element(c => Box(c, "Handled by (Prosthetist / Orthotist):", model.AssignedClinicianName));

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).Padding(6).Text($"Date of registration: {model.RegistrationDate:dd/MM/yyyy}");
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Registration processed by:").FontSize(9);
                            c.Item().BorderBottom(1).Padding(4).Text(model.RegistrationProcessedBy);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated ").FontSize(8);
                    x.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenerateAssessmentForm(AssessmentPrintModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Assessment Form", model.PatientName, model.PatientNumber));

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Element(c => Box(c, "Date of Assessment:", model.AssessedOn.ToString("dd-MMM-yyyy")));
                    col.Item().Element(c => Box(c, "Assessment Time:",
                        model.StartTime.HasValue && model.EndTime.HasValue
                            ? $"{model.StartTime.Value:HH:mm} - {model.EndTime.Value:HH:mm}"
                            : "Not recorded"));
                    col.Item().Element(c => Box(c, "Assessment Type:", model.AssessmentType));
                    col.Item().Element(c => Box(c, "Limb Category:", model.LimbCategory));
                    col.Item().Element(c => Box(c, "Main Problem Type:", model.MainProblemType));
                    col.Item().Element(c => Box(c, "Side:", model.Side));
                    col.Item().Element(c => Box(c, "Cause / Reason / Pathology Type:",
                        model.CauseReasonOther != null ? $"{model.CauseReasonType} - {model.CauseReasonOther}" : model.CauseReasonType));
                    col.Item().Element(c => Box(c, "Additional Assessment Information:", model.AdditionalInformation));
                });

                page.Footer().AlignCenter().Text(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).FontSize(8);
            });
        }).GeneratePdf();
    }

    public byte[] GeneratePrescriptionForm(AssessmentPrintModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Prescription Form", model.PatientName, model.PatientNumber));

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Border(1).Padding(4).Text("Side").Bold();
                            header.Cell().Border(1).Padding(4).Text("Prescription").Bold();
                            header.Cell().Border(1).Padding(4).Text("Sub-Type").Bold();
                            header.Cell().Border(1).Padding(4).Text("Other").Bold();
                        });

                        foreach (var p in model.Prescriptions)
                        {
                            table.Cell().Border(1).Padding(4).Text(p.Side);
                            table.Cell().Border(1).Padding(4).Text(p.Label);
                            table.Cell().Border(1).Padding(4).Text(p.SubType ?? "");
                            table.Cell().Border(1).Padding(4).Text(p.OtherText ?? "");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).FontSize(8);
            });
        }).GeneratePdf();
    }

    public byte[] GenerateDeliveryNote(DeliveryPrintModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Delivery Note", model.PatientName, model.PatientNumber));

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Element(c => Box(c, "Date of Delivery:", model.DeliveryDate.ToString("dd-MMM-yyyy")));
                    col.Item().Element(c => Box(c, "Delivery Time:",
                        model.DeliveryTime?.ToString("HH:mm") ?? "Not recorded"));
                    col.Item().Element(c => Box(c, "Device:", model.DeviceName));
                    col.Item().Element(c => Box(c, "Notes / Remarks:", model.Notes));
                    col.Item().Element(c => Box(c, "Created By:", $"{model.CreatedBy} on {model.CreatedAt:dd-MMM-yyyy}"));
                });

                page.Footer().AlignCenter().Text(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).FontSize(8);
            });
        }).GeneratePdf();
    }

    public byte[] GenerateFollowUpNote(FollowUpPrintModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Follow-up Note", model.PatientName, model.PatientNumber));

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Element(c => Box(c, "Follow-up Date:", model.FollowUpDate.ToString("dd-MMM-yyyy")));
                    col.Item().Element(c => Box(c, "Follow-up Time:",
                        model.StartTime.HasValue && model.EndTime.HasValue
                            ? $"{model.StartTime.Value:HH:mm} - {model.EndTime.Value:HH:mm}"
                            : "Not recorded"));
                    col.Item().Element(c => Box(c, "Remarks / Notes:", model.Notes));
                    col.Item().Element(c => Box(c, "Created By:", $"{model.CreatedBy} on {model.CreatedAt:dd-MMM-yyyy}"));
                });

                page.Footer().AlignCenter().Text(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).FontSize(8);
            });
        }).GeneratePdf();
    }

    public byte[] GenerateReportPdf(string title, string[] columnHeaders, IEnumerable<string[]> rows)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(c =>
                {
                    c.Item().Text("Exceed Lanka Prosthetic & Orthotic Data Management System").Bold().FontSize(12);
                    c.Item().Text(title).FontSize(11);
                    c.Item().Text($"Generated {DateTime.Now:dd-MMM-yyyy HH:mm}").FontSize(8);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in columnHeaders)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in columnHeaders)
                            header.Cell().Border(1).Padding(3).Text(h).Bold();
                    });

                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                            table.Cell().Border(1).Padding(3).Text(cell ?? "");
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void Box(QuestPDF.Infrastructure.IContainer container, string label, string? value)
    {
        container.Border(1).Padding(5).Column(c =>
        {
            c.Item().Text(label).FontSize(9).SemiBold();
            c.Item().Text(value ?? "").FontSize(10);
        });
    }

    private static void TelephoneTable(QuestPDF.Infrastructure.IContainer container, List<PatientContactPrintRow> contacts)
    {
        container.Border(1).Padding(4).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Border(1).Padding(2).Text("Telephone No.").FontSize(8).SemiBold();
                header.Cell().Border(1).Padding(2).Text("Date Confirmed").FontSize(8).SemiBold();
                header.Cell().Border(1).Padding(2).Text("Person Checked").FontSize(8).SemiBold();
            });

            var rows = contacts.Count > 0 ? contacts : new List<PatientContactPrintRow> { new("", null, null) };
            foreach (var c in rows)
            {
                table.Cell().Border(1).Padding(2).Text(c.TelephoneNo).FontSize(9);
                table.Cell().Border(1).Padding(2).Text(c.DateConfirmed?.ToString("dd/MM/yy") ?? "").FontSize(9);
                table.Cell().Border(1).Padding(2).Text(c.PersonChecked ?? "").FontSize(9);
            }
        });
    }

    private static void ComposeHeader(QuestPDF.Infrastructure.IContainer container, string title, string patientName, string patientNumber)
    {
        container.Column(c =>
        {
            c.Item().Text("Exceed Lanka Pvt Ltd").Bold().FontSize(13);
            c.Item().Text(title).Bold().FontSize(12);
            c.Item().Text($"Patient: {patientName} ({patientNumber})").FontSize(10);
            c.Item().Text($"Generated {DateTime.Now:dd-MMM-yyyy HH:mm}").FontSize(8);
            c.Item().PaddingBottom(8).LineHorizontal(1);
        });
    }
}
