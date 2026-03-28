using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DwellScript.Web.Controllers;

[Authorize]
public class BillingController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Billing";
        return View();
    }
}
