using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DwellScript.Web.Controllers;

[Authorize]
public class PropertyController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "My Properties";
        return View();
    }
}
