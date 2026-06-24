using System.Text;
using LeaveManagement.Api.Data;
using LeaveManagement.Api.Helpers;
using LeaveManagement.Api.Models.DTOs;
using LeaveManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaveSummaryRowDto>> GetLeaveSummaryAsync(ReportQuery query, int callerRole, int callerId)
    {
        var scopedDeptId = await ResolveScopedDepartmentIdAsync(callerRole, callerId, query.DepartmentId);

        var q = _context.LeaveRequests
            .Where(lr => lr.Status == LeaveRequestStatus.Approved)
            .Include(lr => lr.User).ThenInclude(u => u.Department)
            .Include(lr => lr.LeaveType)
            .AsQueryable();

        if (scopedDeptId.HasValue)
            q = q.Where(lr => lr.User.DepartmentId == scopedDeptId.Value);

        if (query.LeaveTypeId.HasValue)
            q = q.Where(lr => lr.LeaveTypeId == query.LeaveTypeId.Value);

        if (query.StartDate.HasValue)
            q = q.Where(lr => lr.StartDate >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            q = q.Where(lr => lr.EndDate <= query.EndDate.Value);

        if (query.Year.HasValue)
            q = q.Where(lr => lr.StartDate.Year == query.Year.Value);

        var rows = await q
            .GroupBy(lr => new
            {
                DeptName = lr.User.Department != null ? lr.User.Department.Name : "ไม่มีแผนก",
                LeaveTypeName = lr.LeaveType.Name,
                LeaveTypeColor = lr.LeaveType.ColorCode ?? "#6B7280"
            })
            .Select(g => new LeaveSummaryRowDto
            {
                DepartmentName = g.Key.DeptName,
                LeaveTypeName = g.Key.LeaveTypeName,
                LeaveTypeColor = g.Key.LeaveTypeColor,
                RequestCount = g.Count(),
                EmployeeCount = g.Select(lr => lr.UserId).Distinct().Count(),
                TotalDays = g.Sum(lr => lr.TotalDays)
            })
            .OrderBy(r => r.DepartmentName).ThenBy(r => r.LeaveTypeName)
            .ToListAsync();

        return rows;
    }

    public async Task<string> ExportLeaveSummaryCsvAsync(ReportQuery query, int callerRole, int callerId)
    {
        var rows = await GetLeaveSummaryAsync(query, callerRole, callerId);

        var sb = new StringBuilder();
        sb.AppendLine("แผนก,ประเภทการลา,จำนวนคำขอ,จำนวนพนักงาน,รวมวันลา");

        foreach (var r in rows)
        {
            sb.AppendLine($"\"{r.DepartmentName}\",\"{r.LeaveTypeName}\",{r.RequestCount},{r.EmployeeCount},{r.TotalDays}");
        }

        return sb.ToString();
    }

    private async Task<int?> ResolveScopedDepartmentIdAsync(int callerRole, int callerId, int? requestedDeptId)
    {
        if (callerRole == UserRoles.Manager)
        {
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.ManagerId == callerId);
            return dept?.DepartmentId;
        }

        return requestedDeptId;
    }
}
