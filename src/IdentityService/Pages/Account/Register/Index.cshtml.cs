using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Validation;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityService.Pages.Register
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;  
        }
        [BindProperty]
        public RegisterViewModel Input { get; set; }

        [BindProperty]
        public bool RegisterSuccess { get; set; }


        public IActionResult OnGet(string returnUrl)
        {
            Input = new RegisterViewModel
            {
                ReturnUrl = returnUrl
            };  
            return Page();
        }
        public async Task<IActionResult> OnPost()
        {
            if (Input.Button == "cancel")
            {
                if (string.IsNullOrEmpty(Input.ReturnUrl))
                {
                    return Redirect("~/");
                }
                return Redirect(Input.ReturnUrl);
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Username,
                    Email = Input.Email,
                    EmailConfirmed = true, // for demo purposes only, in production you likely want to require email confirmation   
                };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddClaimsAsync(user, new Claim[]
                    {
                        new Claim(JwtClaimTypes.Name , Input.FullName)
                    });
                    RegisterSuccess = true;
                }
            }

            return Page();  
    }
}

}
