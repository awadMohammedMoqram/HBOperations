using HBOperations.Application.Common.DTOs;
using HBOperations.Domain.Enums;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HBOperations.Web.Services;

/// <summary>
/// خدمة توليد تقارير PDF احترافية بهوية بنك حضرموت الرسمية
/// تستخدم نفس الألوان والخط (Tajawal) المستخدمة في الموقع
/// </summary>
public class PdfReportService
{
    private readonly IWebHostEnvironment _env;
    private static bool _fontsRegistered;
    private static readonly object _fontLock = new();

    // ═══════════ Bank Official Palette (matches website CSS) ═══════════
    // From hadhramout-bank-theme.css :root variables
    private const string Primary = "#003d7a";       // --hb-primary
    private const string PrimaryLight = "#00589e";  // --hb-primary-light
    private const string Gold = "#d4af37";          // --hb-gold
    private const string GoldDark = "#c9a332";      // --hb-gold-dark
    private const string White = "#ffffff";
    private const string LightBg = "#f8f9fa";       // --hb-light-bg
    private const string GrayLight = "#e8e8e8";     // --hb-gray-light
    private const string Gray = "#666666";          // --hb-gray
    private const string TextColor = "#333333";     // --hb-text
    private const string PageBg = "#f2f2f2";        // body bg

    // Soft accent tints for status (light, matching website cards)
    private const string SuccessBg = "#e8f5ee";
    private const string Success = "#1e7e45";
    private const string WarnBg = "#fef5e7";
    private const string Warn = "#b8730e";
    private const string DangerBg = "#fdecea";
    private const string Danger = "#a93226";
    private const string InfoBg = "#e8f1fa";
    private const string Info = "#003d7a";

    private const string FontFamily = "Tajawal";

    public PdfReportService(IWebHostEnvironment env)
    {
        _env = env;
        QuestPDF.Settings.License = LicenseType.Community;
        EnsureFontsRegistered();
    }

    private void EnsureFontsRegistered()
    {
        if (_fontsRegistered) return;
        lock (_fontLock)
        {
            if (_fontsRegistered) return;
            try
            {
                var fontsDir = Path.Combine(_env.WebRootPath, "fonts");
                foreach (var file in new[] { "Tajawal-Regular.ttf", "Tajawal-Medium.ttf", "Tajawal-Bold.ttf" })
                {
                    var path = Path.Combine(fontsDir, file);
                    if (File.Exists(path))
                    {
                        using var fs = File.OpenRead(path);
                        FontManager.RegisterFont(fs);
                    }
                }
            }
            catch { /* fall back to system font */ }
            _fontsRegistered = true;
        }
    }

    private byte[]? GetLogoBytes()
    {
        var path = Path.Combine(_env.WebRootPath, "Images", "aboutLogo-fDwVp-So.png");
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    /// <summary>
    /// Creates a tiled SVG pattern matching the website header's ::before pseudo-element
    /// (main-pattern-sAiPHf0c.svg at 10% opacity)
    /// </summary>
    private string? GetHeaderPatternSvg()
    {
        var svgPath = Path.Combine(_env.WebRootPath, "Images", "main-pattern-sAiPHf0c.svg");
        if (!File.Exists(svgPath)) return null;

        // Read original paths from SVG
        var content = File.ReadAllText(svgPath);
        // Extract path data between <svg> and </svg>
        var startIdx = content.IndexOf('>') + 1;
        var endIdx = content.LastIndexOf("</svg>");
        if (startIdx <= 0 || endIdx <= 0) return null;
        var paths = content[startIdx..endIdx].Trim();

        // Build a tiled SVG: A4 width ≈ 595px, header height ≈ 80px
        // Original tile: 108x18. Tile 6 cols × 5 rows with opacity 0.12
        const int tileW = 108, tileH = 18;
        const int cols = 6, rows = 5;
        var svgWidth = tileW * cols;  // 648
        var svgHeight = tileH * rows; // 90

        var sb = new System.Text.StringBuilder();
        sb.Append($"<svg width=\"{svgWidth}\" height=\"{svgHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");
        sb.Append($"<rect width=\"{svgWidth}\" height=\"{svgHeight}\" fill=\"none\"/>");
        sb.Append($"<g opacity=\"0.12\">");
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var tx = col * tileW;
                var ty = row * tileH;
                sb.Append($"<g transform=\"translate({tx},{ty})\">");
                sb.Append(paths);
                sb.Append("</g>");
            }
        }
        sb.Append("</g></svg>");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════════════════

    public byte[] GenerateBranchReport(List<BranchReportRow> data, ReportSummary summary, string generatedBy, DateTime reportDate)
    {
        return GenerateDocument("تقرير الفروع", summary, generatedBy, reportDate, col =>
        {
            ComposeSummaryBox(col, "يعرض هذا التقرير تحليلاً شاملاً لحركة المعاملات الواردة والصادرة عبر جميع فروع البنك، مع توضيح مؤشرات الأداء الرئيسية وحالات المعاملات.");

            ComposeSection(col, "بيانات الفروع التفصيلية", section =>
            {
                section.Item().Element(c => BuildTransactionTable(c, "الفرع", data.Select(r => (r.BranchName, r.Outgoing, r.Incoming, r.Total, r.Pending, r.Completed, r.Rejected)).ToList()));
            });

            if (data.Count > 0)
            {
                ComposeSection(col, "النتائج والملاحظات", section =>
                {
                    var topBranch = data.OrderByDescending(d => d.Total).First();
                    var completionRate = summary.Total > 0 ? (double)summary.Completed / summary.Total * 100 : 0;

                    Observation(section, "info", $"أعلى فرع نشاطاً: {topBranch.BranchName} بإجمالي {topBranch.Total} معاملة");
                    Observation(section, "success", $"نسبة الإنجاز الإجمالية: {completionRate:F1}% — {(completionRate >= 80 ? "أداء ممتاز" : "يحتاج تحسين")}");
                    if (summary.AvgProcessingHours > 0)
                        Observation(section, "warn", $"متوسط زمن المعاملة: {summary.AvgProcessingHours:F1} ساعة");
                });
            }
        });
    }

    public byte[] GenerateDepartmentReport(List<DepartmentReportRow> data, ReportSummary summary, string generatedBy, DateTime reportDate)
    {
        return GenerateDocument("تقرير الإدارات", summary, generatedBy, reportDate, col =>
        {
            ComposeSummaryBox(col, "يستعرض هذا التقرير أداء الإدارات المختلفة في معالجة المعاملات الواردة والصادرة، مع تحديد الإدارات الأكثر نشاطاً والأكثر كفاءة في الإنجاز.");

            ComposeSection(col, "بيانات الإدارات التفصيلية", section =>
            {
                section.Item().Element(c => BuildTransactionTable(c, "الإدارة", data.Select(r => (r.DepartmentName, r.Outgoing, r.Incoming, r.Total, r.Pending, r.Completed, r.Rejected)).ToList()));
            });

            if (data.Count > 0)
            {
                ComposeSection(col, "النتائج والملاحظات", section =>
                {
                    var topDept = data.OrderByDescending(d => d.Total).First();
                    var completionRate = summary.Total > 0 ? (double)summary.Completed / summary.Total * 100 : 0;

                    Observation(section, "info", $"أعلى إدارة نشاطاً: {topDept.DepartmentName} بإجمالي {topDept.Total} معاملة");
                    Observation(section, "success", $"نسبة الإنجاز الإجمالية: {completionRate:F1}%");
                });
            }
        });
    }

    public byte[] GeneratePerformanceReport(List<PerformanceReportRow> data, ReportSummary summary, string generatedBy, DateTime reportDate, string scopeLabel)
    {
        return GenerateDocument($"تقرير الأداء — {scopeLabel}", summary, generatedBy, reportDate, col =>
        {
            ComposeSummaryBox(col, "يقدم هذا التقرير تحليلاً مفصلاً لمؤشرات الأداء الرئيسية شاملاً عدد المعاملات المعالجة، نسبة الإنجاز، ومتوسط زمن المعالجة لكل جهة.");

            ComposeSection(col, "مؤشرات الأداء التفصيلية", section =>
            {
                section.Item().Element(container =>
                {
                    container.Border(1).BorderColor(GrayLight).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(40);
                            c.RelativeColumn(3);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                        });

                        TableHeader(table, ["#", "الاسم", "المعاملات", "مكتملة", "مرفوضة", "متوسط (ساعة)", "نسبة الإنجاز"]);

                        for (int i = 0; i < data.Count; i++)
                        {
                            var r = data[i];
                            var bg = i % 2 == 0 ? White : LightBg;
                            DataCell(table, (i + 1).ToString(), bg, color: Gray);
                            DataCell(table, r.Name, bg, bold: true, color: Primary, alignRight: true);
                            DataCell(table, r.TotalHandled.ToString(), bg);
                            BadgeCell(table, r.Completed, bg, Success, SuccessBg);
                            BadgeCell(table, r.Rejected, bg, Danger, DangerBg);
                            DataCell(table, r.AvgHours > 0 ? r.AvgHours.ToString("F1") : "—", bg);

                            var rateColor = r.CompletionRate >= 80 ? Success : r.CompletionRate >= 50 ? Warn : Danger;
                            DataCell(table, r.CompletionRate > 0 ? $"{r.CompletionRate:F0}%" : "—", bg, bold: true, color: rateColor);
                        }
                    });
                });
            });

            if (data.Count > 0)
            {
                ComposeSection(col, "النتائج والتوصيات", section =>
                {
                    var topPerformer = data.Where(d => d.CompletionRate > 0).OrderByDescending(d => d.CompletionRate).FirstOrDefault();
                    var fastest = data.Where(d => d.AvgHours > 0).OrderBy(d => d.AvgHours).FirstOrDefault();

                    if (topPerformer != null)
                        Observation(section, "success", $"أعلى نسبة إنجاز: {topPerformer.Name} ({topPerformer.CompletionRate:F0}%)");
                    if (fastest != null)
                        Observation(section, "info", $"أسرع معالجة: {fastest.Name} ({fastest.AvgHours:F1} ساعة)");
                });
            }
        });
    }

    public byte[] GenerateAdminAffairsReport(List<AdminAffairsReportRow> data, ReportSummary summary, string generatedBy, DateTime reportDate)
    {
        return GenerateDocument("تقرير الشؤون الإدارية", summary, generatedBy, reportDate, col =>
        {
            ComposeSummaryBox(col, "يوضح هذا التقرير أداء فريق الشؤون الإدارية في استلام وتسليم المعاملات بين الفروع والإدارات، مع تحليل زمن التوصيل والمعاملات المعلّقة.");

            ComposeSection(col, "أداء فريق الشؤون الإدارية", section =>
            {
                section.Item().Element(container =>
                {
                    container.Border(1).BorderColor(GrayLight).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(40);
                            c.RelativeColumn(3);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.5f);
                        });

                        TableHeader(table, ["#", "الموظف", "تم الاستلام", "تم التسليم", "معلّقة", "متوسط التوصيل (ساعة)"]);

                        for (int i = 0; i < data.Count; i++)
                        {
                            var r = data[i];
                            var bg = i % 2 == 0 ? White : LightBg;
                            DataCell(table, (i + 1).ToString(), bg, color: Gray);
                            DataCell(table, r.HandlerName, bg, bold: true, color: Primary, alignRight: true);
                            DataCell(table, r.PickedUp.ToString(), bg);
                            BadgeCell(table, r.Delivered, bg, Success, SuccessBg);
                            BadgeCell(table, r.Pending, bg, Warn, WarnBg);
                            DataCell(table, r.AvgDeliveryHours > 0 ? r.AvgDeliveryHours.ToString("F1") : "—", bg);
                        }
                    });
                });
            });

            if (data.Count > 0)
            {
                ComposeSection(col, "النتائج والملاحظات", section =>
                {
                    var bestDelivery = data.Where(d => d.AvgDeliveryHours > 0).OrderBy(d => d.AvgDeliveryHours).FirstOrDefault();
                    var totalPending = data.Sum(d => d.Pending);

                    if (bestDelivery != null)
                        Observation(section, "success", $"أسرع موظف في التوصيل: {bestDelivery.HandlerName} ({bestDelivery.AvgDeliveryHours:F1} ساعة)");
                    Observation(section, totalPending > 5 ? "warn" : "info", $"إجمالي المعاملات المعلّقة: {totalPending}");
                });
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //  Document Builder — matches website header / cards / tables
    // ═══════════════════════════════════════════════════════════════

    private byte[] GenerateDocument(string title, ReportSummary summary, string generatedBy, DateTime reportDate, Action<ColumnDescriptor> addContent)
    {
        var logo = GetLogoBytes();
        var patternSvg = GetHeaderPatternSvg();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.PageColor(PageBg);
                page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(10).FontColor(TextColor));
                page.ContentFromRightToLeft();

                // ─── HEADER (Primary blue gradient + identity pattern like site) ─────────
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Layers(layers =>
                    {
                        // Layer 1: Solid navy background
                        layers.Layer().Background(Primary);

                        // Layer 2: Identity pattern overlay (matching .hb-header::before)
                        if (patternSvg != null)
                        {
                            layers.Layer().AlignCenter().AlignMiddle()
                                .Svg(SvgImage.FromText(patternSvg));
                        }

                        // Layer 3: Header content
                        layers.PrimaryLayer().PaddingVertical(16).PaddingHorizontal(24).Row(row =>
                        {
                            // Right side: Logo + Bank name
                            row.RelativeItem().Row(innerRow =>
                            {
                                if (logo != null)
                                {
                                    innerRow.ConstantItem(52).AlignMiddle().Image(logo).FitWidth();
                                    innerRow.ConstantItem(14);
                                }
                                innerRow.RelativeItem().AlignMiddle().Column(c =>
                                {
                                    c.Item().Text("بنك حضرموت").FontSize(18).Bold().FontColor(White);
                                    c.Item().PaddingTop(2).Text("نظام إدارة العمليات المصرفية").FontSize(9).FontColor("#ffffffcc");
                                });
                            });

                            // Left side: Report title + meta
                            row.ConstantItem(210).AlignMiddle().Column(c =>
                            {
                                c.Item().AlignLeft().Text(title).FontSize(17).Bold().FontColor(Gold);
                                c.Item().PaddingTop(5).AlignLeft().Text(t =>
                                {
                                    t.Span("التاريخ: ").FontSize(9).FontColor("#ffffffb3");
                                    t.Span($"{reportDate:yyyy/MM/dd}").FontSize(9).Bold().FontColor(White);
                                });
                                c.Item().AlignLeft().Text(t =>
                                {
                                    t.Span("الوقت: ").FontSize(9).FontColor("#ffffffb3");
                                    t.Span($"{reportDate:HH:mm}").FontSize(9).Bold().FontColor(White);
                                });
                                c.Item().AlignLeft().Text(t =>
                                {
                                    t.Span("أعدّ بواسطة: ").FontSize(9).FontColor("#ffffffb3");
                                    t.Span(generatedBy).FontSize(9).Bold().FontColor(White);
                                });
                            });
                        });
                    });

                    // Gold stripe (like website's gold accent)
                    headerCol.Item().Height(3).Background(Gold);
                });

                // ─── CONTENT ──────────────────────────────────────────
                page.Content().PaddingHorizontal(24).PaddingTop(18).PaddingBottom(10).Column(col =>
                {
                    // Stat cards row (like dashboard .stat-card)
                    col.Item().Row(row =>
                    {
                        StatCard(row, summary.Total.ToString(), "إجمالي المعاملات", Primary);
                        row.ConstantItem(10);
                        StatCard(row, summary.Completed.ToString(), "مسلّمة", Success);
                        row.ConstantItem(10);
                        StatCard(row, summary.Pending.ToString(), "معلّقة", Warn);
                        row.ConstantItem(10);
                        StatCard(row, summary.Rejected.ToString(), "مرفوضة", Danger);
                    });

                    // Secondary stats: completion progress + avg time
                    col.Item().PaddingTop(12).Row(row =>
                    {
                        var pct = summary.Total > 0 ? (double)summary.Completed / summary.Total * 100 : 0;

                        row.RelativeItem().Background(White).Border(1).BorderColor(GrayLight)
                            .Padding(14).Column(c =>
                            {
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("نسبة الإنجاز").FontSize(10).Bold().FontColor(Gray);
                                    r.ConstantItem(60).AlignLeft().Text($"{pct:F1}%").FontSize(15).Bold().FontColor(Success);
                                });
                                c.Item().PaddingTop(8).Height(8).Background(GrayLight).Row(barRow =>
                                {
                                    if (pct > 0)
                                        barRow.RelativeItem((float)Math.Max(pct, 0.5)).Background(Success);
                                    if (pct < 100)
                                        barRow.RelativeItem((float)Math.Max(100 - pct, 0.5)).Background(GrayLight);
                                });
                            });

                        row.ConstantItem(10);

                        row.RelativeItem().Background(White).Border(1).BorderColor(GrayLight)
                            .Padding(14).Row(r =>
                            {
                                r.RelativeItem().AlignMiddle().Text("متوسط وقت المعاملة").FontSize(10).Bold().FontColor(Gray);
                                r.ConstantItem(90).AlignMiddle().AlignLeft().Text(t =>
                                {
                                    t.Span(summary.AvgProcessingHours > 0 ? summary.AvgProcessingHours.ToString("F1") : "—")
                                        .FontSize(15).Bold().FontColor(Primary);
                                    t.Span("  ساعة").FontSize(9).FontColor(Gray);
                                });
                            });
                    });

                    col.Item().PaddingTop(16);
                    addContent(col);
                });

                // ─── FOOTER ───────────────────────────────────────────
                page.Footer().BorderTop(2).BorderColor(Gold).Background(White)
                    .PaddingHorizontal(24).PaddingVertical(10).Row(row =>
                    {
                        row.RelativeItem().AlignMiddle().Text(t =>
                        {
                            t.Span("بنك ").FontSize(10).Bold().FontColor(Primary);
                            t.Span("حضرموت").FontSize(10).Bold().FontColor(Gold);
                            t.Span("  ·  نظام إدارة العمليات").FontSize(8).FontColor(Gray);
                        });

                        row.ConstantItem(170).AlignMiddle().AlignCenter()
                            .Text("سري · للاستخدام الداخلي فقط")
                            .FontSize(8).Bold().FontColor(Gray).AlignCenter();

                        row.RelativeItem().AlignMiddle().AlignLeft().Text(t =>
                        {
                            t.Span("صفحة ").FontSize(9).FontColor(Gray);
                            t.CurrentPageNumber().FontSize(9).FontColor(Primary).Bold();
                            t.Span(" من ").FontSize(9).FontColor(Gray);
                            t.TotalPages().FontSize(9).FontColor(Primary).Bold();
                        });
                    });
            });
        }).GeneratePdf();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Component Builders
    // ═══════════════════════════════════════════════════════════════

    private static void StatCard(RowDescriptor row, string value, string label, string accent)
    {
        row.RelativeItem().Background(White).Border(1).BorderColor(GrayLight)
            .Padding(0).Column(col =>
            {
                col.Item().Height(3).Background(accent);
                col.Item().PaddingVertical(14).PaddingHorizontal(10).Column(c =>
                {
                    c.Item().AlignCenter().Text(value).FontSize(24).Bold().FontColor(accent);
                    c.Item().AlignCenter().PaddingTop(4).Text(label).FontSize(9).FontColor(Gray);
                });
            });
    }

    private static void ComposeSummaryBox(ColumnDescriptor col, string text)
    {
        col.Item().PaddingBottom(14).Background(InfoBg).Border(1).BorderColor("#cfdcec")
            .BorderRight(3).BorderColor(Primary)
            .PaddingVertical(12).PaddingHorizontal(14)
            .Text(text).FontSize(9.5f).FontColor(Primary).LineHeight(1.7f);
    }

    private static void ComposeSection(ColumnDescriptor parentCol, string title, Action<ColumnDescriptor> content)
    {
        parentCol.Item().PaddingBottom(14).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(4).Background(Gold);
                row.ConstantItem(8);
                row.RelativeItem().AlignMiddle().PaddingBottom(2)
                    .Text(title).FontSize(13).Bold().FontColor(Primary);
            });
            col.Item().PaddingTop(2).BorderBottom(1).BorderColor(GrayLight);
            col.Item().PaddingTop(10).Column(content);
        });
    }

    private static void Observation(ColumnDescriptor section, string type, string text)
    {
        var (bg, color) = type switch
        {
            "success" => (SuccessBg, Success),
            "warn" => (WarnBg, Warn),
            "danger" => (DangerBg, Danger),
            _ => (InfoBg, Info)
        };

        section.Item().PaddingBottom(6).Background(bg).BorderRight(3).BorderColor(color)
            .PaddingVertical(9).PaddingHorizontal(12)
            .Text(text).FontSize(9.5f).FontColor(color).LineHeight(1.5f);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Tables (matches .hb-table in website CSS)
    // ═══════════════════════════════════════════════════════════════

    private static void BuildTransactionTable(IContainer container, string nameHeader,
        List<(string Name, int Outgoing, int Incoming, int Total, int Pending, int Completed, int Rejected)> rows)
    {
        container.Border(1).BorderColor(GrayLight).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(40);
                c.RelativeColumn(3);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(1.2f);
            });

            TableHeader(table, ["#", nameHeader, "الصادرة", "الواردة", "الإجمالي", "معلّقة", "مسلّمة", "مرفوضة"]);

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var bg = i % 2 == 0 ? White : LightBg;
                DataCell(table, (i + 1).ToString(), bg, color: Gray);
                DataCell(table, r.Name, bg, bold: true, color: Primary, alignRight: true);
                BadgeCell(table, r.Outgoing, bg, Info, InfoBg);
                BadgeCell(table, r.Incoming, bg, Info, InfoBg);
                DataCell(table, r.Total.ToString(), bg, bold: true);
                BadgeCell(table, r.Pending, bg, Warn, WarnBg);
                BadgeCell(table, r.Completed, bg, Success, SuccessBg);
                BadgeCell(table, r.Rejected, bg, Danger, DangerBg);
            }

            // Total row (gold accent like website footer)
            TotalCell(table, "");
            TotalCell(table, "المجموع");
            TotalCell(table, rows.Sum(r => r.Outgoing).ToString());
            TotalCell(table, rows.Sum(r => r.Incoming).ToString());
            TotalCell(table, rows.Sum(r => r.Total).ToString());
            TotalCell(table, rows.Sum(r => r.Pending).ToString());
            TotalCell(table, rows.Sum(r => r.Completed).ToString());
            TotalCell(table, rows.Sum(r => r.Rejected).ToString());
        });
    }

    private static void TableHeader(TableDescriptor table, string[] headers)
    {
        foreach (var h in headers)
        {
            table.Cell().Background(Primary).PaddingVertical(10).PaddingHorizontal(8)
                .Text(h).FontColor(White).FontSize(9.5f).Bold().AlignCenter();
        }
    }

    private static void DataCell(TableDescriptor table, string text, string bg,
        bool bold = false, string? color = null, bool alignRight = false)
    {
        var cell = table.Cell().Background(bg).BorderBottom(1).BorderColor(GrayLight)
            .PaddingVertical(8).PaddingHorizontal(8);
        var t = cell.Text(text).FontSize(9.5f);
        if (alignRight) t.AlignRight(); else t.AlignCenter();
        if (bold) t.Bold();
        t.FontColor(color ?? TextColor);
    }

    private static void BadgeCell(TableDescriptor table, int value, string rowBg, string color, string badgeBg)
    {
        var cell = table.Cell().Background(rowBg).BorderBottom(1).BorderColor(GrayLight)
            .PaddingVertical(6).PaddingHorizontal(6).AlignCenter().AlignMiddle();

        if (value > 0)
        {
            cell.Background(badgeBg).PaddingVertical(3).PaddingHorizontal(8)
                .Text(value.ToString()).FontSize(9).Bold().FontColor(color).AlignCenter();
        }
        else
        {
            cell.Text("0").FontSize(9).FontColor(Gray).AlignCenter();
        }
    }

    private static void TotalCell(TableDescriptor table, string text)
    {
        table.Cell().Background(PrimaryLight).PaddingVertical(9).PaddingHorizontal(8)
            .Text(text).FontSize(10).Bold().FontColor(White).AlignCenter();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Audit Trail Report (للمدققين / المراجعين فقط)
    //  يعرض الحقول الحساسة: ملاحظة الرفض، ملاحظة الإدارة
    // ═══════════════════════════════════════════════════════════════

    public byte[] GenerateAuditTrailReport(List<AuditTrailReportRow> rows, ReportSummary summary, string generatedBy, DateTime reportDate)
    {
        return GenerateDocument("تقرير المراجعة والتدقيق", summary, generatedBy, reportDate, col =>
        {
            ComposeSummaryBox(col,
                "يعرض هذا التقرير المخصّص للمدققين والمراجعين السجل الكامل للمعاملات بما فيها الحقول الحساسة (ملاحظات الرفض والإدارة) لأغراض التدقيق والمتابعة.");

            ComposeSection(col, $"سجل المعاملات ({rows.Count} معاملة)", section =>
            {
                section.Item().Element(container =>
                {
                    container.Border(1).BorderColor(GrayLight).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.4f); // المرجع
                            c.RelativeColumn(2.4f); // الموضوع
                            c.RelativeColumn(1.1f); // النوع
                            c.RelativeColumn(1.1f); // الحالة
                            c.RelativeColumn(1.6f); // المرسل
                            c.RelativeColumn(1.6f); // المستقبل
                            c.RelativeColumn(1.2f); // التاريخ
                            c.RelativeColumn(2.2f); // ملاحظات الإدارة/الرفض
                        });

                        TableHeader(table, ["المرجع", "الموضوع", "النوع", "الحالة", "من", "إلى", "تاريخ", "ملاحظات حساسة"]);

                        for (int i = 0; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var bg = i % 2 == 0 ? White : LightBg;
                            DataCell(table, r.ReferenceNumber, bg, bold: true, color: Primary);
                            DataCell(table, Truncate(r.Subject, 60), bg, alignRight: true);
                            DataCell(table, GetTypeName(r.Type), bg);
                            StatusBadgeCell(table, r.Status, bg);
                            DataCell(table, r.SenderName ?? "—", bg, alignRight: true);
                            DataCell(table, r.ReceiverName ?? "—", bg, alignRight: true);
                            DataCell(table, r.CreatedAt.ToString("yyyy/MM/dd"), bg, color: Gray);

                            // الملاحظات الحساسة
                            var notes = new List<string>();
                            if (!string.IsNullOrWhiteSpace(r.RejectionNote)) notes.Add($"رفض: {Truncate(r.RejectionNote, 80)}");
                            if (!string.IsNullOrWhiteSpace(r.AdminNote)) notes.Add($"إدارة: {Truncate(r.AdminNote, 80)}");
                            DataCell(table, notes.Count > 0 ? string.Join("\n", notes) : "—", bg,
                                color: notes.Count > 0 ? Danger : Gray, alignRight: true);
                        }
                    });
                });
            });

            if (rows.Count > 0)
            {
                ComposeSection(col, "ملخص التدقيق", section =>
                {
                    var rejected = rows.Count(r => !string.IsNullOrWhiteSpace(r.RejectionNote));
                    var withAdminNote = rows.Count(r => !string.IsNullOrWhiteSpace(r.AdminNote));
                    Observation(section, rejected > 0 ? "warn" : "info", $"عدد المعاملات المرفوضة في الفترة: {rejected}");
                    Observation(section, "info", $"معاملات بملاحظات إدارية: {withAdminNote}");
                    Observation(section, "danger", "تنبيه: هذا التقرير سرّي ومخصص للتدقيق فقط — يحظر تداوله خارج الفريق المعتمد.");
                });
            }
        });
    }

    private static string Truncate(string? text, int maxLen)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= maxLen ? text : text[..maxLen] + "…";
    }

    // ═══════════════════════════════════════════════════════════════
    //  Personal Report (للموظف الفردي — معاملاته فقط)
    // ═══════════════════════════════════════════════════════════════

    public byte[] GeneratePersonalReport(List<PersonalReportRow> rows, ReportSummary summary, string generatedBy, DateTime reportDate)
    {
        return GenerateDocument("تقريري الشخصي", summary, generatedBy, reportDate, col =>
        {
            ComposeSummaryBox(col,
                "ملخص لمعاملاتك الشخصية في الفترة المحددة، يشمل المعاملات الصادرة منك والواردة إليك مع حالتها الحالية.");

            ComposeSection(col, "قائمة معاملاتي", section =>
            {
                section.Item().Element(container =>
                {
                    container.Border(1).BorderColor(GrayLight).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(35);
                            c.RelativeColumn(1.6f);
                            c.RelativeColumn(3);
                            c.RelativeColumn(1.3f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1.4f);
                        });

                        TableHeader(table, ["#", "المرجع", "الموضوع", "النوع", "الاتجاه", "الطرف الآخر", "الحالة"]);

                        for (int i = 0; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var bg = i % 2 == 0 ? White : LightBg;
                            DataCell(table, (i + 1).ToString(), bg, color: Gray);
                            DataCell(table, r.ReferenceNumber, bg, bold: true, color: Primary);
                            DataCell(table, Truncate(r.Subject, 70), bg, alignRight: true);
                            DataCell(table, GetTypeName(r.Type), bg);
                            DataCell(table, r.Direction, bg, color: r.Direction == "صادرة" ? Info : Success);
                            DataCell(table, r.CounterpartName ?? "—", bg, alignRight: true);
                            StatusBadgeCell(table, r.Status, bg);
                        }
                    });
                });
            });
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //  Shariah Compliance Report (للمراجع الشرعي)
    //  يفلتر التحويلات النقدية فقط ويعرض حقول الالتزام الشرعي
    // ═══════════════════════════════════════════════════════════════

    public byte[] GenerateShariahReport(List<AuditTrailReportRow> cashRows, ReportSummary summary, string generatedBy, DateTime reportDate)
    {
        return GenerateDocument("تقرير الرقابة الشرعية", summary, generatedBy, reportDate, col =>
        {
            ComposeSummaryBox(col,
                "تقرير مخصّص للمراجع الشرعي يستعرض المعاملات المالية (التحويلات النقدية) للتحقق من مطابقتها لأحكام الشريعة الإسلامية ومعايير الرقابة الشرعية في البنك.");

            ComposeSection(col, $"المعاملات النقدية ({cashRows.Count} معاملة)", section =>
            {
                section.Item().Element(container =>
                {
                    container.Border(1).BorderColor(GrayLight).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.4f); // المرجع
                            c.RelativeColumn(2.6f); // الموضوع
                            c.RelativeColumn(1.6f); // المرسل
                            c.RelativeColumn(1.6f); // المستقبل
                            c.RelativeColumn(1.2f); // التاريخ
                            c.RelativeColumn(1.2f); // الحالة
                        });

                        TableHeader(table, ["المرجع", "الموضوع", "من", "إلى", "تاريخ", "الحالة"]);

                        for (int i = 0; i < cashRows.Count; i++)
                        {
                            var r = cashRows[i];
                            var bg = i % 2 == 0 ? White : LightBg;
                            DataCell(table, r.ReferenceNumber, bg, bold: true, color: Primary);
                            DataCell(table, Truncate(r.Subject, 70), bg, alignRight: true);
                            DataCell(table, r.SenderBranch ?? "—", bg, alignRight: true);
                            DataCell(table, r.ReceiverBranch ?? "—", bg, alignRight: true);
                            DataCell(table, r.CreatedAt.ToString("yyyy/MM/dd"), bg, color: Gray);
                            StatusBadgeCell(table, r.Status, bg);
                        }
                    });
                });
            });

            ComposeSection(col, "ملاحظات شرعية", section =>
            {
                Observation(section, "info", $"إجمالي التحويلات النقدية في الفترة: {cashRows.Count}");
                Observation(section, summary.Rejected > 0 ? "warn" : "success",
                    $"معاملات مرفوضة (تستوجب المراجعة الشرعية): {summary.Rejected}");
                Observation(section, "info",
                    $"معاملات مكتملة بنجاح: {summary.Completed} ({summary.CompletionRate:F1}%)");
                Observation(section, "danger",
                    "تنبيه: هذا التقرير سرّي ومخصص للرقابة الشرعية فقط — يحظر تداوله خارج الجهة المعتمدة.");
            });
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //  Rejection Analysis Report (تحليل أسباب الرفض)
    // ═══════════════════════════════════════════════════════════════

    public byte[] GenerateRejectionAnalysisReport(
        List<RejectionAnalysisRow> rejected,
        List<RejectionGroup> byType,
        List<RejectionGroup> byBranch,
        ReportSummary summary,
        string generatedBy,
        DateTime reportDate)
    {
        return GenerateDocument("تقرير تحليل أسباب الرفض", summary, generatedBy, reportDate, col =>
        {
            ComposeSummaryBox(col,
                "يُحلل هذا التقرير المعاملات المرفوضة في الفترة المحددة لتحديد الأنماط المتكررة والجهات الأكثر إصداراً للرفض، بهدف تحسين جودة المعاملات وتقليل نسب الرفض.");

            // التوزيع حسب النوع
            if (byType.Count > 0)
            {
                ComposeSection(col, "توزيع الرفض حسب نوع المعاملة", section =>
                {
                    section.Item().Element(c => BuildRejectionGroupTable(c, "نوع المعاملة", byType));
                });
            }

            // التوزيع حسب الفرع
            if (byBranch.Count > 0)
            {
                ComposeSection(col, "توزيع الرفض حسب الفرع", section =>
                {
                    section.Item().Element(c => BuildRejectionGroupTable(c, "الفرع", byBranch));
                });
            }

            // التفاصيل
            ComposeSection(col, $"تفاصيل المعاملات المرفوضة ({rejected.Count})", section =>
            {
                section.Item().Element(container =>
                {
                    container.Border(1).BorderColor(GrayLight).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(2.2f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(2.7f);
                        });

                        TableHeader(table, ["المرجع", "الموضوع", "النوع", "الفرع", "تاريخ الرفض", "سبب الرفض"]);

                        for (int i = 0; i < rejected.Count; i++)
                        {
                            var r = rejected[i];
                            var bg = i % 2 == 0 ? White : LightBg;
                            DataCell(table, r.ReferenceNumber, bg, bold: true, color: Primary);
                            DataCell(table, Truncate(r.Subject, 50), bg, alignRight: true);
                            DataCell(table, GetTypeName(r.Type), bg);
                            DataCell(table, r.SenderBranch ?? "—", bg, alignRight: true);
                            DataCell(table, r.RejectedAt.ToString("yyyy/MM/dd"), bg, color: Gray);
                            DataCell(table, Truncate(r.RejectionNote, 90) ?? "بدون سبب", bg,
                                color: Danger, alignRight: true);
                        }
                    });
                });
            });

            ComposeSection(col, "توصيات", section =>
            {
                if (byType.Count > 0)
                {
                    var topType = byType.OrderByDescending(g => g.Count).First();
                    Observation(section, "warn", $"النوع الأكثر رفضاً: {topType.Label} ({topType.Count} معاملة، {topType.Percentage:F0}%)");
                }
                if (byBranch.Count > 0)
                {
                    var topBranch = byBranch.OrderByDescending(g => g.Count).First();
                    Observation(section, "warn", $"الفرع الأكثر رفضاً: {topBranch.Label} ({topBranch.Count} معاملة)");
                }
                Observation(section, "info", "يُنصح بمراجعة معايير قبول المعاملات وتدريب الموظفين على الأنواع الأكثر رفضاً.");
            });
        });
    }

    private static void BuildRejectionGroupTable(IContainer container, string nameHeader, List<RejectionGroup> groups)
    {
        container.Border(1).BorderColor(GrayLight).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(40);
                c.RelativeColumn(3);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(1.5f);
                c.RelativeColumn(3);
            });

            TableHeader(table, ["#", nameHeader, "العدد", "النسبة", "الرسم البياني"]);

            var maxCount = groups.Max(g => g.Count);
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                var bg = i % 2 == 0 ? White : LightBg;
                DataCell(table, (i + 1).ToString(), bg, color: Gray);
                DataCell(table, g.Label, bg, bold: true, color: Primary, alignRight: true);
                DataCell(table, g.Count.ToString(), bg, bold: true);
                DataCell(table, $"{g.Percentage:F1}%", bg, color: Danger);

                // bar
                var barPct = maxCount > 0 ? (float)g.Count / maxCount : 0;
                table.Cell().Background(bg).BorderBottom(1).BorderColor(GrayLight)
                    .PaddingVertical(8).PaddingHorizontal(8).AlignMiddle()
                    .Height(10).Background(GrayLight).Row(r =>
                    {
                        if (barPct > 0)
                            r.RelativeItem(Math.Max(barPct, 0.01f)).Background(Danger);
                        if (barPct < 1)
                            r.RelativeItem(Math.Max(1 - barPct, 0.01f)).Background(GrayLight);
                    });
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //  Executive Report (للإدارة العليا — مفصّل وشامل)
    // ═══════════════════════════════════════════════════════════════

    public byte[] GenerateExecutiveReport(ExecutiveReportData data, string generatedBy, DateTime reportDate)
    {
        return GenerateDocument($"التقرير التنفيذي — {data.PeriodLabel}", data.Summary, generatedBy, reportDate, col =>
        {
            ComposeSummaryBox(col,
                $"تقرير تنفيذي شامل للإدارة العليا يستعرض الأداء الإجمالي للبنك خلال الفترة ({data.PeriodLabel})، يشمل أداء الفروع والإدارات وتوزيع أنواع المعاملات والاتجاهات الزمنية.");

            // أداء الفروع
            if (data.Branches.Count > 0)
            {
                ComposeSection(col, "أداء الفروع", section =>
                {
                    section.Item().Element(c => BuildTransactionTable(c, "الفرع",
                        data.Branches.Take(10).Select(r => (r.BranchName, r.Outgoing, r.Incoming, r.Total, r.Pending, r.Completed, r.Rejected)).ToList()));
                });
            }

            // أداء الإدارات
            if (data.Departments.Count > 0)
            {
                ComposeSection(col, "أداء الإدارات", section =>
                {
                    section.Item().Element(c => BuildTransactionTable(c, "الإدارة",
                        data.Departments.Take(10).Select(r => (r.DepartmentName, r.Outgoing, r.Incoming, r.Total, r.Pending, r.Completed, r.Rejected)).ToList()));
                });
            }

            // توزيع الأنواع
            if (data.TypeBreakdown.Count > 0)
            {
                ComposeSection(col, "توزيع المعاملات حسب النوع", section =>
                {
                    var total = data.TypeBreakdown.Values.Sum();
                    var groups = data.TypeBreakdown
                        .OrderByDescending(kv => kv.Value)
                        .Select(kv => new RejectionGroup
                        {
                            Label = GetTypeName(kv.Key),
                            Count = kv.Value,
                            Percentage = total > 0 ? (double)kv.Value / total * 100 : 0
                        }).ToList();
                    section.Item().Element(c => BuildExecutiveTypeTable(c, groups));
                });
            }

            // الخلاصة التنفيذية
            ComposeSection(col, "الخلاصة التنفيذية", section =>
            {
                Observation(section, "info", $"إجمالي المعاملات في الفترة: {data.Summary.Total}");
                Observation(section, data.Summary.CompletionRate >= 80 ? "success" : "warn",
                    $"نسبة الإنجاز: {data.Summary.CompletionRate:F1}% — {(data.Summary.CompletionRate >= 80 ? "أداء ممتاز" : "يحتاج تحسين")}");
                if (data.Summary.AvgProcessingHours > 0)
                    Observation(section, "info", $"متوسط وقت المعالجة: {data.Summary.AvgProcessingHours:F1} ساعة");
                if (data.TotalRejected > 0)
                    Observation(section, "warn", $"عدد المعاملات المرفوضة: {data.TotalRejected} ({(data.Summary.Total > 0 ? (double)data.TotalRejected / data.Summary.Total * 100 : 0):F1}%)");
                if (data.Branches.Count > 0)
                {
                    var top = data.Branches.OrderByDescending(b => b.Total).First();
                    Observation(section, "success", $"أعلى فرع نشاطاً: {top.BranchName} ({top.Total} معاملة)");
                }
            });
        });
    }

    private static void BuildExecutiveTypeTable(IContainer container, List<RejectionGroup> groups)
    {
        container.Border(1).BorderColor(GrayLight).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(40);
                c.RelativeColumn(3);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(1.5f);
                c.RelativeColumn(3);
            });

            TableHeader(table, ["#", "نوع المعاملة", "العدد", "النسبة", "التوزيع"]);

            var maxCount = groups.Max(g => g.Count);
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                var bg = i % 2 == 0 ? White : LightBg;
                DataCell(table, (i + 1).ToString(), bg, color: Gray);
                DataCell(table, g.Label, bg, bold: true, color: Primary, alignRight: true);
                DataCell(table, g.Count.ToString(), bg, bold: true);
                DataCell(table, $"{g.Percentage:F1}%", bg, color: Info);

                var barPct = maxCount > 0 ? (float)g.Count / maxCount : 0;
                table.Cell().Background(bg).BorderBottom(1).BorderColor(GrayLight)
                    .PaddingVertical(8).PaddingHorizontal(8).AlignMiddle()
                    .Height(10).Background(GrayLight).Row(r =>
                    {
                        if (barPct > 0)
                            r.RelativeItem(Math.Max(barPct, 0.01f)).Background(Primary);
                        if (barPct < 1)
                            r.RelativeItem(Math.Max(1 - barPct, 0.01f)).Background(GrayLight);
                    });
            }
        });
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

    private static void StatusBadgeCell(TableDescriptor table, TransactionStatus status, string rowBg)
    {
        var (color, bgColor, label) = status switch
        {
            TransactionStatus.Received or TransactionStatus.Archived => (Success, SuccessBg, "مستلمة"),
            TransactionStatus.Sent or TransactionStatus.InTransit => (Warn, WarnBg, "قيد التسليم"),
            TransactionStatus.Rejected => (Danger, DangerBg, "مرفوضة"),
            _ => (Gray, GrayLight, status.ToString())
        };

        table.Cell().Background(rowBg).BorderBottom(1).BorderColor(GrayLight)
            .PaddingVertical(6).PaddingHorizontal(6).AlignCenter().AlignMiddle()
            .Background(bgColor).PaddingVertical(3).PaddingHorizontal(8)
            .Text(label).FontSize(8.5f).Bold().FontColor(color).AlignCenter();
    }
}
