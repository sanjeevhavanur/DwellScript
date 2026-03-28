using DwellScript.Web.Data;
using Microsoft.Extensions.Logging;

namespace DwellScript.Web.Services;

public class GenerationService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<GenerationService> _logger;

    public GenerationService(AppDbContext db, IConfiguration config, ILogger<GenerationService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }
}
