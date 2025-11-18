using ClothingStore.Core.Entities;
using ClothingStoreApp.Data;
using ClothingStoreApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStoreApp.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserAddress<ApplicationDbContext> _context;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // LIST USERS
        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // USER DETAILS + ADDRESSES + ROLES
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var addresses = _context.UserAddresses.Where(a => a.UserId == id).ToList();

            var vm = new UserDetailsViewModel
            {
                User = user,
                Roles = roles.ToList(),
                Addresses = addresses
            };

            return View(vm);
        }

        // DISABLE ACCOUNT
        public async Task<IActionResult> Disable(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTime.UtcNow.AddYears(100);

            await _userManager.UpdateAsync(user);
            return RedirectToAction("Index");
        }

        // ENABLE ACCOUNT
        public async Task<IActionResult> Enable(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnd = null;

            await _userManager.UpdateAsync(user);
            return RedirectToAction("Index");
        }
    }
}
