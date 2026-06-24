using System.Security.Claims;
using System.Text;
using LeaveManagement.Api.Helpers;
using LeaveManagement.Api.Models.DTOs;
using LeaveManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private int GetUserId() => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    private int GetUserRole() => int.Parse(User.FindFirst(ClaimTypes.Role)?.Value ?? "0");

    [HttpGet("leave-summary")]
    public async Task<ActionResult<List<LeaveSummaryRowDto>>> GetLeaveSummary([FromQuery] ReportQuery query)
    {
        var role = GetUserRole();
        if (role != UserRoles.Manager && role != UserRoles.Admin)
            return Forbid();

        var result = await _reportService.GetLeaveSummaryAsync(query, role, GetUserId());
        return Ok(result);
    }

    [HttpGet("leave-summary/export")]
    public async Task<IActionResult> ExportLeaveSummary([FromQuery] ReportQuery query)
    {
        var role = GetUserRole();
        if (role != UserRoles.Manager && role != UserRoles.Admin)
            return Forbid();

        var csv = await _reportService.ExportLeaveSummaryCsvAsync(query, role, GetUserId());
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"leave-summary-{DateTime.Now:yyyyMMdd}.csv");
    }
}
