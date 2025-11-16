using ClothingStore.Core.Entities;
using ClothingStoreApp.Models;
using ClothingStoreApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStoreApp.Controllers
{
        public class AccountController : Controller
        {
            private readonly SignInManager<ApplicationUser> _signInManager;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly RoleManager<IdentityRole> _roleManager;

            public AccountController(
                SignInManager<ApplicationUser> signInManager,
                UserManager<ApplicationUser> userManager,
                RoleManager<IdentityRole> roleManager)
            {
                _signInManager = signInManager;
                _userManager = userManager;
                _roleManager = roleManager;
            }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
            public async Task<IActionResult> Login(string email, string password,string returnUrl = "/")
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    ViewBag.Error = "Invalid login attempt";
                    return View();
                }

                var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
                if (result.Succeeded)
                {
                // after successful sign-in:
                await _signInManager.SignInAsync(user, isPersistent: false);

                // merge session cart into DB cart
                var cartService = HttpContext.RequestServices.GetRequiredService<ICartService>();
                await cartService.MergeSessionCartToDbAsync();

                return Redirect(returnUrl);
               // return RedirectToAction("Index", "Home");

                }
 

                ViewBag.Error = "Invalid login attempt";
                return View();
            }

            [HttpGet]
        public IActionResult Register(string returnUrl = "/")
        {
            ViewBag.ReturnUrl = returnUrl+ "Account/Login";
            return View();
        }

            [HttpPost]
            public async Task<IActionResult> Register(string fullName, string email, string password ,string Role, string returnUrl = "/")
            {
                var user = new ApplicationUser
                {
                    FullName = fullName,
                    Email = email,
                    UserName = email
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Role);
                //return RedirectToAction("Login",returnUrl);
                return Redirect(returnUrl);
                }

                ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
                return View();
            }

            public async Task<IActionResult> Logout()
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }
        }
}
