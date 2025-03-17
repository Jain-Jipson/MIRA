using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/visitors")]
[ApiController]
public class VisitorsController : ControllerBase
{
    private readonly VisitorLogService _visitorLogService;

    public VisitorsController(VisitorLogService visitorLogService)
    {
        _visitorLogService = visitorLogService;
    }

    // ✅ GET: /api/visitors → Retrieve all logged visitors
    [HttpGet]
    public async Task<ActionResult<List<Visitor>>> GetVisitors()
    {
        var visitors = await _visitorLogService.GetVisitorsAsync();
        return Ok(visitors);
    }
}
