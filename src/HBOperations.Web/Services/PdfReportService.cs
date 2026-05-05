using HBOperations.Application.Common.DTOs;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace HBOperations.Web.Services;

public class PdfReportService
{
    private readonly IWebHostEnvironment _env;
    private static bool _browserDownloaded;
    private static readonly SemaphoreSlim _downloadLock = new(1, 1);

    public PdfReportService(IWebHostEnvironment env)
    {
        _env = env;
    }

    private async Task EnsureBrowserAsync()
    {
        if (_browserDownloaded) return;
        await _downloadLock.WaitAsync();
        try
        {
            if (!_browserDownloaded)
            {
                var fetcher = new BrowserFetcher();
                await fetcher.DownloadAsync();
                _browserDownloaded = true;
            }
        }
        finally
        {
            _downloadLock.Release();
        }
    }

    private string GetLogoBase64()
    {
        var path = Path.Combine(_env.WebRootPath, "Images", "HeaderLogo.png");
        if (!File.Exists(path)) return "";
        var bytes = File.ReadAllBytes(path);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    private async Task<byte[]> RenderHtmlToPdf(string html)
    {
        await EnsureBrowserAsync();
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-setuid-sandbox"]
        });
        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new NavigationOptions { WaitUntil = [WaitUntilNavigation.Networkidle0] });
        var pdf = await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions { Top = "15mm", Bottom = "15mm", Left = "12mm", Right = "12mm" }
        });
        return pdf;
    }

    // ═══════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════

    public async Task<byte[]> GenerateBranchReportAsync(List<BranchReportRow> data, ReportSummary summary, string generatedBy, DateTime reportDate)
    {
        var tableRows = string.Join("", data.Select((r, i) => $@"
            <tr class=""{(i % 2 == 1 ? "alt" : "")}"">
                <td class=""name"">{r.BranchName}</td>
                <td>{r.Outgoing}</td>
                <td>{r.Incoming}</td>
                <td class=""bold"">{r.Total}</td>
                <td class=""warning"">{r.Pending}</td>
                <td class=""success"">{r.Completed}</td>
                <td class=""danger"">{r.Rejected}</td>
            </tr>"));

        var totals = new BranchReportRow
        {
            BranchName = "المجموع",
            Outgoing = data.Sum(d => d.Outgoing),
            Incoming = data.Sum(d => d.Incoming),
            Total = data.Sum(d => d.Total),
            Pending = data.Sum(d => d.Pending),
            Completed = data.Sum(d => d.Completed),
            Rejected = data.Sum(d => d.Rejected)
        };

        var totalRow = $@"
            <tr class=""total-row"">
                <td class=""name"">{totals.BranchName}</td>
                <td>{totals.Outgoing}</td>
                <td>{totals.Incoming}</td>
                <td>{totals.Total}</td>
                <td>{totals.Pending}</td>
                <td>{totals.Completed}</td>
                <td>{totals.Rejected}</td>
            </tr>";

        var tableHtml = $@"
            <table>
                <thead>
                    <tr>
                        <th>الفرع</th>
                        <th>الصادرة</th>
                        <th>الواردة</th>
                        <th>الإجمالي</th>
                        <th>معلّقة</th>
                        <th>مستلمة</th>
                        <th>مرفوضة</th>
                    </tr>
                </thead>
                <tbody>
                    {tableRows}
                    {totalRow}
                </tbody>
            </table>";

        var html = BuildFullHtml("تقرير الفروع", summary, generatedBy, reportDate, tableHtml);
        return await RenderHtmlToPdf(html);
    }

    public async Task<byte[]> GenerateDepartmentReportAsync(List<DepartmentReportRow> data, ReportSummary summary, string generatedBy, DateTime reportDate)
    {
        var tableRows = string.Join("", data.Select((r, i) => $@"
            <tr class=""{(i % 2 == 1 ? "alt" : "")}"">
                <td class=""name"">{r.DepartmentName}</td>
                <td>{r.Outgoing}</td>
                <td>{r.Incoming}</td>
                <td class=""bold"">{r.Total}</td>
                <td class=""warning"">{r.Pending}</td>
                <td class=""success"">{r.Completed}</td>
                <td class=""danger"">{r.Rejected}</td>
            </tr>"));

        var tableHtml = $@"
            <table>
                <thead>
                    <tr>
                        <th>الإدارة</th>
                        <th>الصادرة</th>
                        <th>الواردة</th>
                        <th>الإجمالي</th>
                        <th>معلّقة</th>
                        <th>مستلمة</th>
                        <th>مرفوضة</th>
                    </tr>
                </thead>
                <tbody>{tableRows}</tbody>
            </table>";

        var html = BuildFullHtml("تقرير الإدارات", summary, generatedBy, reportDate, tableHtml);
        return await RenderHtmlToPdf(html);
    }

    public async Task<byte[]> GeneratePerformanceReportAsync(List<PerformanceReportRow> data, ReportSummary summary, string generatedBy, DateTime reportDate, string scopeLabel)
    {
        var tableRows = string.Join("", data.Select((r, i) => $@"
            <tr class=""{(i % 2 == 1 ? "alt" : "")}"">
                <td class=""name"">{r.Name}</td>
                <td>{r.TotalHandled}</td>
                <td class=""success"">{r.Completed}</td>
                <td class=""danger"">{r.Rejected}</td>
                <td>{(r.AvgHours > 0 ? r.AvgHours.ToString("F1") : "—")}</td>
                <td>
                    <div class=""progress-cell"">
                        <div class=""progress-bar"" style=""width:{r.CompletionRate:F0}%""></div>
                        <span>{r.CompletionRate:F0}%</span>
                    </div>
                </td>
            </tr>"));

        var tableHtml = $@"
            <table>
                <thead>
                    <tr>
                        <th>الاسم</th>
                        <th>المعاملات</th>
                        <th>مكتملة</th>
                        <th>مرفوضة</th>
                        <th>متوسط (ساعة)</th>
                        <th>نسبة الإنجاز</th>
                    </tr>
                </thead>
                <tbody>{tableRows}</tbody>
            </table>";

        var html = BuildFullHtml($"تقرير الأداء — {scopeLabel}", summary, generatedBy, reportDate, tableHtml);
        return await RenderHtmlToPdf(html);
    }

    public async Task<byte[]> GenerateAdminAffairsReportAsync(List<AdminAffairsReportRow> data, ReportSummary summary, string generatedBy, DateTime reportDate)
    {
        var tableRows = string.Join("", data.Select((r, i) => $@"
            <tr class=""{(i % 2 == 1 ? "alt" : "")}"">
                <td class=""name"">{r.HandlerName}</td>
                <td>{r.PickedUp}</td>
                <td class=""success"">{r.Delivered}</td>
                <td class=""warning"">{r.Pending}</td>
                <td>{(r.AvgDeliveryHours > 0 ? r.AvgDeliveryHours.ToString("F1") : "—")}</td>
            </tr>"));

        var tableHtml = $@"
            <table>
                <thead>
                    <tr>
                        <th>الموظف</th>
                        <th>تم الاستلام</th>
                        <th>تم التسليم</th>
                        <th>معلّقة</th>
                        <th>متوسط التوصيل (ساعة)</th>
                    </tr>
                </thead>
                <tbody>{tableRows}</tbody>
            </table>";

        var html = BuildFullHtml("تقرير الشؤون الإدارية", summary, generatedBy, reportDate, tableHtml);
        return await RenderHtmlToPdf(html);
    }

    // ═══════════════════════════════════════════════
    // HTML Builder
    // ═══════════════════════════════════════════════

    private string BuildFullHtml(string title, ReportSummary summary, string generatedBy, DateTime reportDate, string tableContent)
    {
        var logo = GetLogoBase64();
        var completionRate = summary.Total > 0 ? (double)summary.Completed / summary.Total * 100 : 0;

        return $@"<!DOCTYPE html>
<html dir=""rtl"" lang=""ar"">
<head>
    <meta charset=""utf-8"">
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: 'Segoe UI', Tahoma, Arial, sans-serif;
            background: #ffffff;
            color: #1f2937;
            font-size: 12px;
            line-height: 1.6;
            direction: rtl;
        }}

        /* Header */
        .header {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 20px 0;
            border-bottom: 3px solid #003d7a;
            margin-bottom: 24px;
        }}
        .header-right {{
            display: flex;
            align-items: center;
            gap: 16px;
        }}
        .header-logo {{
            width: 60px;
            height: auto;
        }}
        .header-text h1 {{
            font-size: 20px;
            color: #003d7a;
            font-weight: 800;
            margin-bottom: 2px;
        }}
        .header-text p {{
            font-size: 11px;
            color: #6b7280;
        }}
        .header-meta {{
            text-align: left;
            font-size: 10px;
            color: #6b7280;
        }}
        .header-meta span {{
            display: block;
            margin-bottom: 2px;
        }}

        /* KPI Cards */
        .kpi-grid {{
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 12px;
            margin-bottom: 24px;
        }}
        .kpi-card {{
            background: linear-gradient(135deg, #f8fafc 0%, #ffffff 100%);
            border: 1px solid #e2e8f0;
            border-radius: 10px;
            padding: 16px;
            text-align: center;
            position: relative;
            overflow: hidden;
        }}
        .kpi-card::before {{
            content: '';
            position: absolute;
            top: 0;
            right: 0;
            left: 0;
            height: 3px;
        }}
        .kpi-card.blue::before {{ background: #003d7a; }}
        .kpi-card.green::before {{ background: #2e7d32; }}
        .kpi-card.orange::before {{ background: #e65100; }}
        .kpi-card.red::before {{ background: #c62828; }}
        .kpi-value {{
            font-size: 28px;
            font-weight: 800;
            margin-bottom: 4px;
        }}
        .kpi-card.blue .kpi-value {{ color: #003d7a; }}
        .kpi-card.green .kpi-value {{ color: #2e7d32; }}
        .kpi-card.orange .kpi-value {{ color: #e65100; }}
        .kpi-card.red .kpi-value {{ color: #c62828; }}
        .kpi-label {{
            font-size: 11px;
            color: #6b7280;
            font-weight: 500;
        }}

        /* Completion bar */
        .completion-section {{
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 10px;
            padding: 14px 20px;
            margin-bottom: 24px;
            display: flex;
            align-items: center;
            gap: 16px;
        }}
        .completion-label {{
            font-size: 11px;
            color: #374151;
            font-weight: 600;
            white-space: nowrap;
        }}
        .completion-track {{
            flex: 1;
            height: 10px;
            background: #e2e8f0;
            border-radius: 10px;
            overflow: hidden;
        }}
        .completion-fill {{
            height: 100%;
            background: linear-gradient(90deg, #003d7a, #1565c0);
            border-radius: 10px;
            transition: width 0.3s;
        }}
        .completion-pct {{
            font-size: 14px;
            font-weight: 800;
            color: #003d7a;
            white-space: nowrap;
        }}

        /* Table */
        table {{
            width: 100%;
            border-collapse: separate;
            border-spacing: 0;
            border-radius: 10px;
            overflow: hidden;
            border: 1px solid #e2e8f0;
            margin-bottom: 20px;
        }}
        thead tr {{
            background: linear-gradient(135deg, #003d7a 0%, #004d99 100%);
        }}
        thead th {{
            color: #ffffff;
            font-size: 10.5px;
            font-weight: 700;
            padding: 12px 10px;
            text-align: center;
            letter-spacing: 0.3px;
        }}
        tbody td {{
            padding: 10px;
            text-align: center;
            font-size: 11px;
            border-bottom: 1px solid #f1f5f9;
            color: #374151;
        }}
        tbody td.name {{
            text-align: right;
            font-weight: 600;
            color: #1f2937;
        }}
        tbody td.bold {{ font-weight: 700; color: #003d7a; }}
        tbody td.success {{ color: #2e7d32; font-weight: 600; }}
        tbody td.warning {{ color: #e65100; font-weight: 600; }}
        tbody td.danger {{ color: #c62828; font-weight: 600; }}
        tbody tr.alt {{ background: #f8fafc; }}
        tbody tr:hover {{ background: #eef4fb; }}
        tbody tr.total-row {{
            background: linear-gradient(135deg, #e8f4fd 0%, #dbeafe 100%);
            font-weight: 700;
        }}
        tbody tr.total-row td {{
            font-weight: 700;
            color: #003d7a;
            border-top: 2px solid #003d7a;
            font-size: 11.5px;
        }}

        /* Progress bar in cells */
        .progress-cell {{
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        .progress-bar {{
            height: 6px;
            background: linear-gradient(90deg, #2e7d32, #4caf50);
            border-radius: 6px;
            min-width: 4px;
        }}
        .progress-cell span {{
            font-size: 10px;
            font-weight: 600;
            color: #2e7d32;
        }}

        /* Footer */
        .footer {{
            margin-top: 30px;
            padding-top: 12px;
            border-top: 1px solid #e2e8f0;
            display: flex;
            justify-content: space-between;
            align-items: center;
            font-size: 9px;
            color: #9ca3af;
        }}
        .footer .bank {{ color: #003d7a; font-weight: 700; }}
        .footer .confidential {{
            background: #fef2f2;
            color: #c62828;
            padding: 2px 10px;
            border-radius: 4px;
            font-size: 8px;
            font-weight: 600;
        }}

        @media print {{
            body {{ -webkit-print-color-adjust: exact; print-color-adjust: exact; }}
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""header-right"">
            {(string.IsNullOrEmpty(logo) ? "" : $@"<img src=""{logo}"" class=""header-logo"" alt=""Logo"">")}
            <div class=""header-text"">
                <h1>{title}</h1>
                <p>بنك حضرموت — نظام إدارة العمليات</p>
            </div>
        </div>
        <div class=""header-meta"">
            <span>تاريخ التقرير: {reportDate:yyyy/MM/dd}</span>
            <span>الوقت: {reportDate:HH:mm}</span>
            <span>أُعدّ بواسطة: {generatedBy}</span>
        </div>
    </div>

    <div class=""kpi-grid"">
        <div class=""kpi-card blue"">
            <div class=""kpi-value"">{summary.Total}</div>
            <div class=""kpi-label"">إجمالي المعاملات</div>
        </div>
        <div class=""kpi-card green"">
            <div class=""kpi-value"">{summary.Completed}</div>
            <div class=""kpi-label"">مستلمة</div>
        </div>
        <div class=""kpi-card orange"">
            <div class=""kpi-value"">{summary.Pending}</div>
            <div class=""kpi-label"">معلّقة</div>
        </div>
        <div class=""kpi-card red"">
            <div class=""kpi-value"">{summary.Rejected}</div>
            <div class=""kpi-label"">مرفوضة</div>
        </div>
    </div>

    <div class=""completion-section"">
        <span class=""completion-label"">نسبة الإنجاز</span>
        <div class=""completion-track"">
            <div class=""completion-fill"" style=""width:{completionRate:F0}%""></div>
        </div>
        <span class=""completion-pct"">{completionRate:F1}%</span>
    </div>

    {tableContent}

    <div class=""footer"">
        <span class=""bank"">بنك حضرموت</span>
        <span class=""confidential"">سري — للاستخدام الداخلي فقط</span>
        <span>طُبع بتاريخ {reportDate:yyyy/MM/dd} الساعة {reportDate:HH:mm}</span>
    </div>
</body>
</html>";
    }
}
