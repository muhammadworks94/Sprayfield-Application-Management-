using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Services.Models;

namespace SAM.Services.Interfaces;

public interface IBreadcrumbService
{
    IReadOnlyList<BreadcrumbItem> Build(ViewContext viewContext);
}
