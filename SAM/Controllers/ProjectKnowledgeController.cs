using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;

namespace SAM.Controllers;

[Authorize(Policy = Policies.RequireAdmin)]
public class ProjectKnowledgeController : BaseController
{
    private readonly IProjectKnowledgeService _projectKnowledgeService;

    public ProjectKnowledgeController(
        IProjectKnowledgeService projectKnowledgeService,
        UserManager<ApplicationUser> userManager,
        ILogger<ProjectKnowledgeController> logger) : base(userManager, logger)
    {
        _projectKnowledgeService = projectKnowledgeService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var viewModel = _projectKnowledgeService.Build();
        return View(viewModel);
    }
}
