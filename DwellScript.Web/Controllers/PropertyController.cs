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

    public IActionResult Create()
    {
        ViewData["Title"] = "Add Property";
        ViewData["IsEdit"] = false;
        return View();
    }

    public IActionResult Edit(int id)
    {
        ViewData["Title"] = "Edit Property";
        ViewData["IsEdit"] = true;
        ViewData["PropertyId"] = id;
        return View("Create");
    }

    public IActionResult Detail(int id)
    {
        ViewData["Title"] = "Property";
        ViewData["PropertyId"] = id;
        return View();
    }
}
