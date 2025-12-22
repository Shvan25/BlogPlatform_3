using BlogPlatform.Data.DTOs;
using BlogPlatform.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlogPlatform.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            ViewData["Title"] = "Вход";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["Title"] = "Регистрация";
            return View();
        }
    }
}