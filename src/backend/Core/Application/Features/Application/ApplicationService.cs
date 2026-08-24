using Application.Abstractions;
using Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

public sealed class ApplicationService : IApplicationService
{
    private readonly IApplicationDbContext _context;

    public ApplicationService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<ApplicationDto>> GetActiveAsync() =>
        _context.Applications
            .AsNoTracking()
            .Where(application => application.IsActive)
            .OrderBy(application => application.Name)
            .Select(application => new ApplicationDto
            {
                Id = application.Id,
                Name = application.Name,
                Description = application.Description,
                Repository = application.Repository,
                Branch = application.Branch,
                ProjectKey = application.ProjectKey,
                IsActive = application.IsActive
            })
            .ToListAsync();
}
