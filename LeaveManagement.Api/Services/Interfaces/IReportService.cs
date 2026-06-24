using LeaveManagement.Api.Models.DTOs;

namespace LeaveManagement.Api.Services.Interfaces;

public interface IReportService
{
    Task<List<LeaveSummaryRowDto>> GetLeaveSummaryAsync(ReportQuery query, int callerRole, int callerId);
    Task<string> ExportLeaveSummaryCsvAsync(ReportQuery query, int callerRole, int callerId);
}
