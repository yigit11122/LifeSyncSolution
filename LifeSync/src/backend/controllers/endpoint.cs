using backend.models;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly LifeSyncDbContext _context;

    public TestController(LifeSyncDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var tasks = _context.Tasks.ToList();
        return Ok(tasks);
    }
}
