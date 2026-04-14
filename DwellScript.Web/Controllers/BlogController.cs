using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DwellScript.Web.Controllers;

[AllowAnonymous]
public class BlogController : Controller
{
    [Route("Blog")]
    public IActionResult Index()
    {
        return View();
    }

    [Route("Blog/how-to-write-a-rental-listing")]
    public IActionResult HowToWriteARentalListing()
    {
        return View();
    }

    [Route("Blog/vacancy-too-long")]
    public IActionResult VacancyTooLong()
    {
        return View();
    }
}
