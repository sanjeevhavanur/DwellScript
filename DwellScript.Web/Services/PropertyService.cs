using DwellScript.Web.Data;
using Microsoft.Extensions.Logging;

namespace DwellScript.Web.Services;

public class PropertyService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PropertyService> _logger;

    public PropertyService(AppDbContext db, ILogger<PropertyService> logger)
    {
        _db = db;
        _logger = logger;
    }
}
