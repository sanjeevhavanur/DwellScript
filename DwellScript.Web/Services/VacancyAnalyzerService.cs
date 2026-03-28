using DwellScript.Web.Data;
using Microsoft.Extensions.Logging;

namespace DwellScript.Web.Services;

public class VacancyAnalyzerService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<VacancyAnalyzerService> _logger;

    public VacancyAnalyzerService(AppDbContext db, IConfiguration config, ILogger<VacancyAnalyzerService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }
}
