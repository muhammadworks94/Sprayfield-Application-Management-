using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Services.Interfaces;
using SAM.ViewModels.Account;

namespace SAM.Controllers;

/// <summary>
/// Controller for Account management - Login, Logout, Profile, Access Denied.
/// </summary>
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly ICompanyService _companyService;
    private readonly IUserRequestService _userRequestService;
    private readonly ICompanyRequestService _companyRequestService;
    private readonly IEmailService _emailService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger,
        ICompanyService companyService,
        IUserRequestService userRequestService,
        ICompanyRequestService companyRequestService,
        IEmailService emailService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _companyService = companyService;
        _userRequestService = userRequestService;
        _companyRequestService = companyRequestService;
        _emailService = emailService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName ?? model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in.", model.Email);
            return RedirectToLocal(returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", model.Email);
            return RedirectToAction(nameof(Lockout));
        }

        ModelState.AddModelError("", "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        // For security, do not reveal whether the user exists or is active.
        if (user != null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var callbackUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { userId = user.Id, token },
                protocol: Request.Scheme) ?? string.Empty;

            var htmlBody =
                $"<p>You requested to reset your password for your SAM account.</p>" +
                $"<p>Please click the link below to reset your password:</p>" +
                $"<p><a href=\"{callbackUrl}\">Reset your password</a></p>" +
                "<p>If you did not request this, you can safely ignore this email.</p>";

            try
            {
                await _emailService.SendEmailAsync(model.Email, "Reset your SAM password", htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}.", model.Email);
                // Do not disclose email sending failures to avoid leaking information.
            }

            _logger.LogInformation("Password reset requested for email {Email}.", model.Email);
        }

        TempData["SuccessMessage"] = "If an account with that email exists, you will receive password reset instructions.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "Invalid password reset token.";
            return RedirectToAction(nameof(Login));
        }

        var model = new ResetPasswordViewModel
        {
            UserId = userId,
            Token = token
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            // For security, do not reveal that the user does not exist.
            TempData["SuccessMessage"] = "Your password has been reset. You can now log in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Your password has been reset. You can now log in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userEmail = User.Identity?.Name ?? "Unknown";
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {Email} logged out.", userEmail);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Signup()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var viewModel = new SignupViewModel
        {
            SignupType = SignupType.JoinExisting
        };

        // Load companies for dropdown
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name");

        return View(viewModel);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(SignupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var companies = await _companyService.GetAllAsync();
            ViewBag.Companies = new SelectList(companies, "Id", "Name");
            return View(model);
        }

        try
        {
            if (model.SignupType == SignupType.JoinExisting)
            {
                // Validate company selection
                if (!model.SelectedCompanyId.HasValue)
                {
                    ModelState.AddModelError("SelectedCompanyId", "Please select a company.");
                    var companies = await _companyService.GetAllAsync();
                    ViewBag.Companies = new SelectList(companies, "Id", "Name");
                    return View(model);
                }

                var company = await _companyService.GetByIdAsync(model.SelectedCompanyId.Value);
                if (company == null)
                {
                    ModelState.AddModelError("SelectedCompanyId", "Selected company not found.");
                    var companies = await _companyService.GetAllAsync();
                    ViewBag.Companies = new SelectList(companies, "Id", "Name");
                    return View(model);
                }

                // Create UserRequest for existing company
                var userRequest = new UserRequest
                {
                    CompanyId = model.SelectedCompanyId.Value,
                    CompanyName = company.Name,
                    FullName = model.FullName,
                    Email = model.Email,
                    AppRole = AppRoleEnum.@operator, // Default role for self-signup
                    RequestedByEmail = model.Email, // Self-requested
                    Status = RequestStatusEnum.Pending,
                    IsSelfSignup = true
                };

                await _userRequestService.CreateAsync(userRequest);
                TempData["SuccessMessage"] = "Your request to join the company has been submitted. You will be notified via email once approved by the company administrator.";
            }
            else // CreateNew
            {
                // Validate company name
                if (string.IsNullOrWhiteSpace(model.CompanyName))
                {
                    ModelState.AddModelError("CompanyName", "Company name is required.");
                    var companies = await _companyService.GetAllAsync();
                    ViewBag.Companies = new SelectList(companies, "Id", "Name");
                    return View(model);
                }

                // Create CompanyRequest
                var companyRequest = new CompanyRequest
                {
                    CompanyName = model.CompanyName,
                    ContactEmail = model.Email, // Use requester's email as company contact
                    PhoneNumber = model.PhoneNumber ?? string.Empty,
                    Website = model.Website,
                    Description = model.Description,
                    RequesterFullName = model.FullName,
                    RequesterEmail = model.Email,
                    Status = RequestStatusEnum.Pending
                };

                await _companyRequestService.CreateAsync(companyRequest);
                TempData["SuccessMessage"] = "Your company request has been submitted. You will be notified via email once approved by the administrator.";
            }

            _logger.LogInformation("Signup request submitted: {Email}, Type: {SignupType}", model.Email, model.SignupType);
            return RedirectToAction("Login", "Account");
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var companies = await _companyService.GetAllAsync();
            ViewBag.Companies = new SelectList(companies, "Id", "Name");
            return View(model);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        // Load company if user has one
        Company? company = null;
        if (user.CompanyId.HasValue)
        {
            var userWithCompany = await _userManager.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == user.Id);
            company = userWithCompany?.Company;
        }

        var viewModel = new ProfileViewModel
        {
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            CompanyName = company?.Name
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        user.FullName = model.FullName;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(model);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Dashboard");
    }
}

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
}

public class ProfileViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Company")]
    public string? CompanyName { get; set; }
}

