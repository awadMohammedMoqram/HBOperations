using ClosedXML.Excel;
using HBOperations.Application.Common.DTOs;
using HBOperations.Domain.Enums;

namespace HBOperations.Web.Services;

/// <summary>
/// مساعدات بناء أوراق Excel للتقارير المختلفة.
/// كل طريقة تضيف ورقة عمل جاهزة بالتنسيق الرسمي للبنك (RTL، ألوان، حدود).
/// </summary>
internal static class ExcelHelpers
{
    private static readonly XLColor BankPrimary = XLColor.FromArgb(0, 61, 122);
    private static readonly XLColor BankGold = XLColor.FromArgb(212, 175, 55);
    private static readonly XLColor LightBlue = XLColor.FromArgb(232, 244, 253);
    private static readonly XLColor LightGreen = XLColor.FromArgb(232, 245, 238);
    private static readonly XLColor LightRed = XLColor.FromArgb(253, 236, 234);
    private static readonly XLColor LightOrange = XLColor.FromArgb(254, 245, 231);

    public static void AddSummarySheet(IXLWorkbook workbook, string title, ReportSummary summary, string generatedBy, DateTime reportDate)
    {
        var ws = workbook.Worksheets.Add("الملخص");
        ws.RightToLeft = true;

        ws.Cell(1, 1).Value = $"{title} — بنك حضرموت";
        ws.Range(1, 1, 1, 4).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = BankPrimary;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 28;

        ws.Cell(2, 1).Value = $"تاريخ التقرير: {reportDate:yyyy/MM/dd HH:mm}";
        ws.Cell(3, 1).Value = $"أُعد بواسطة: {generatedBy}";

        WriteSummaryRow(ws, 5, "إجمالي المعاملات", summary.Total, BankPrimary);
        WriteSummaryRow(ws, 6, "مستلمة", summary.Completed, LightGreen);
        WriteSummaryRow(ws, 7, "معلّقة", summary.Pending, LightOrange);
        WriteSummaryRow(ws, 8, "مرفوضة", summary.Rejected, LightRed);
        WriteSummaryRow(ws, 9, "نسبة الإنجاز", $"{summary.CompletionRate:F1}%", LightBlue);
        if (summary.AvgProcessingHours > 0)
            WriteSummaryRow(ws, 10, "متوسط وقت المعالجة (ساعة)", $"{summary.AvgProcessingHours:F1}", LightBlue);

        ws.Columns().AdjustToContents();
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 30);
        ws.Column(2).Width = Math.Max(ws.Column(2).Width, 20);
    }

    private static void WriteSummaryRow(IXLWorksheet ws, int row, string label, object value, XLColor accent)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = accent;
        if (accent == BankPrimary) ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;

        if (value is int i) ws.Cell(row, 2).Value = i;
        else ws.Cell(row, 2).Value = value.ToString();

        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Range(row, 1, row, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    public static void AddBranchesSheet(IXLWorkbook workbook, List<BranchReportRow> rows)
    {
        var ws = workbook.Worksheets.Add("تقرير الفروع");
        ws.RightToLeft = true;

        WriteHeaders(ws, ["الفرع", "الصادرة", "الواردة", "الإجمالي", "معلّقة", "مستلمة", "مرفوضة"]);

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.BranchName;
            ws.Cell(row, 2).Value = r.Outgoing;
            ws.Cell(row, 3).Value = r.Incoming;
            ws.Cell(row, 4).Value = r.Total;
            ws.Cell(row, 5).Value = r.Pending;
            ws.Cell(row, 6).Value = r.Completed;
            ws.Cell(row, 7).Value = r.Rejected;
            row++;
        }

        // Total row
        ws.Cell(row, 1).Value = "المجموع";
        ws.Cell(row, 2).Value = rows.Sum(r => r.Outgoing);
        ws.Cell(row, 3).Value = rows.Sum(r => r.Incoming);
        ws.Cell(row, 4).Value = rows.Sum(r => r.Total);
        ws.Cell(row, 5).Value = rows.Sum(r => r.Pending);
        ws.Cell(row, 6).Value = rows.Sum(r => r.Completed);
        ws.Cell(row, 7).Value = rows.Sum(r => r.Rejected);
        ws.Row(row).Style.Font.Bold = true;
        ws.Row(row).Style.Fill.BackgroundColor = LightBlue;

        FinalizeTable(ws, row, 7);
    }

    public static void AddDepartmentsSheet(IXLWorkbook workbook, List<DepartmentReportRow> rows)
    {
        var ws = workbook.Worksheets.Add("تقرير الإدارات");
        ws.RightToLeft = true;

        WriteHeaders(ws, ["الإدارة", "الصادرة", "الواردة", "الإجمالي", "معلّقة", "مستلمة", "مرفوضة"]);

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.DepartmentName;
            ws.Cell(row, 2).Value = r.Outgoing;
            ws.Cell(row, 3).Value = r.Incoming;
            ws.Cell(row, 4).Value = r.Total;
            ws.Cell(row, 5).Value = r.Pending;
            ws.Cell(row, 6).Value = r.Completed;
            ws.Cell(row, 7).Value = r.Rejected;
            row++;
        }

        FinalizeTable(ws, row - 1, 7);
    }

    public static void AddAuditTrailSheet(IXLWorkbook workbook, List<AuditTrailReportRow> rows, string sheetName = "سجل المراجعة")
    {
        var ws = workbook.Worksheets.Add(sheetName);
        ws.RightToLeft = true;

        WriteHeaders(ws, ["المرجع", "الموضوع", "النوع", "الحالة", "المرسل", "المستقبل", "تاريخ الإنشاء", "تاريخ الاستلام", "ملاحظة الرفض", "ملاحظة الإدارة"]);

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.ReferenceNumber;
            ws.Cell(row, 2).Value = r.Subject;
            ws.Cell(row, 3).Value = GetTypeName(r.Type);
            ws.Cell(row, 4).Value = GetStatusName(r.Status);
            ws.Cell(row, 5).Value = r.SenderBranch ?? "—";
            ws.Cell(row, 6).Value = r.ReceiverBranch ?? "—";
            ws.Cell(row, 7).Value = r.CreatedAt.ToString("yyyy/MM/dd HH:mm");
            ws.Cell(row, 8).Value = r.ReceivedAt?.ToString("yyyy/MM/dd HH:mm") ?? "—";
            ws.Cell(row, 9).Value = r.RejectionNote ?? "—";
            ws.Cell(row, 10).Value = r.AdminNote ?? "—";

            // Color status cell
            ws.Cell(row, 4).Style.Fill.BackgroundColor = r.Status switch
            {
                TransactionStatus.Received or TransactionStatus.Archived => LightGreen,
                TransactionStatus.Rejected => LightRed,
                TransactionStatus.Sent or TransactionStatus.InTransit => LightOrange,
                _ => XLColor.NoColor
            };
            row++;
        }

        FinalizeTable(ws, row - 1, 10);
    }

    public static void AddPersonalSheet(IXLWorkbook workbook, List<PersonalReportRow> rows)
    {
        var ws = workbook.Worksheets.Add("معاملاتي");
        ws.RightToLeft = true;

        WriteHeaders(ws, ["المرجع", "الموضوع", "النوع", "الاتجاه", "الطرف الآخر", "الحالة", "تاريخ الإنشاء", "تاريخ الإكمال"]);

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.ReferenceNumber;
            ws.Cell(row, 2).Value = r.Subject;
            ws.Cell(row, 3).Value = GetTypeName(r.Type);
            ws.Cell(row, 4).Value = r.Direction;
            ws.Cell(row, 5).Value = r.CounterpartName ?? "—";
            ws.Cell(row, 6).Value = GetStatusName(r.Status);
            ws.Cell(row, 7).Value = r.CreatedAt.ToString("yyyy/MM/dd");
            ws.Cell(row, 8).Value = r.CompletedAt?.ToString("yyyy/MM/dd") ?? "—";

            ws.Cell(row, 4).Style.Fill.BackgroundColor = r.Direction == "صادرة" ? LightBlue : LightGreen;
            row++;
        }

        FinalizeTable(ws, row - 1, 8);
    }

    public static void AddRejectionGroupSheet(IXLWorkbook workbook, string sheetName, string nameHeader, List<RejectionGroup> groups)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        ws.RightToLeft = true;

        WriteHeaders(ws, [nameHeader, "العدد", "النسبة"]);

        int row = 2;
        foreach (var g in groups)
        {
            ws.Cell(row, 1).Value = g.Label;
            ws.Cell(row, 2).Value = g.Count;
            ws.Cell(row, 3).Value = $"{g.Percentage:F1}%";
            ws.Cell(row, 3).Style.Fill.BackgroundColor = LightRed;
            row++;
        }

        FinalizeTable(ws, row - 1, 3);
    }

    public static void AddRejectionDetailsSheet(IXLWorkbook workbook, List<RejectionAnalysisRow> rows)
    {
        var ws = workbook.Worksheets.Add("التفاصيل");
        ws.RightToLeft = true;

        WriteHeaders(ws, ["المرجع", "الموضوع", "النوع", "الفرع", "تاريخ الرفض", "سبب الرفض"]);

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.ReferenceNumber;
            ws.Cell(row, 2).Value = r.Subject;
            ws.Cell(row, 3).Value = GetTypeName(r.Type);
            ws.Cell(row, 4).Value = r.SenderBranch ?? "—";
            ws.Cell(row, 5).Value = r.RejectedAt.ToString("yyyy/MM/dd");
            ws.Cell(row, 6).Value = r.RejectionNote ?? "بدون سبب";
            ws.Cell(row, 6).Style.Fill.BackgroundColor = LightRed;
            row++;
        }

        FinalizeTable(ws, row - 1, 6);
    }

    private static void WriteHeaders(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = BankPrimary;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.Row(1).Height = 22;
    }

    private static void FinalizeTable(IXLWorksheet ws, int lastRow, int lastCol)
    {
        if (lastRow >= 1)
        {
            var range = ws.Range(1, 1, lastRow, lastCol);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
    }

    private static string GetTypeName(TransactionType t) => t switch
    {
        TransactionType.DocumentDelivery => "تسليم مستندات",
        TransactionType.CashTransfer => "تحويل نقدي",
        TransactionType.InternalDepartment => "داخلي",
        TransactionType.BranchToBranch => "بين الفروع",
        TransactionType.Other => "أخرى",
        _ => t.ToString()
    };

    private static string GetStatusName(TransactionStatus s) => s switch
    {
        TransactionStatus.Sent => "مُرسلة",
        TransactionStatus.InTransit => "قيد التوصيل",
        TransactionStatus.Received => "مستلمة",
        TransactionStatus.Rejected => "مرفوضة",
        TransactionStatus.Archived => "مؤرشفة",
        _ => s.ToString()
    };
}
