using Microsoft.AspNetCore.Mvc;

namespace TicTacToe.Web.Controllers;

public class LobbyController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
