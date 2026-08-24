using Application.Abstractions;
using Application.Contracts;
using BuildingBlocks.Helpers;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

public sealed class UserContactProfileService : IUserContactProfileService
{
    private readonly IApplicationDbContext _context;

    public UserContactProfileService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task UpsertFromStaffAsync(
        StaffDetailsDto staff,
        CancellationToken cancellationToken = default)
    {
        var userId = staff.UserId.Trim().ToLowerInvariant();
        var profile = await _context.UserContactProfiles
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new UserContactProfile { UserId = userId };
            _context.UserContactProfiles.Add(profile);
        }

        profile.DisplayName = Normalize(staff.Name);
        profile.Email = Normalize(staff.Email)?.ToLowerInvariant();
        profile.Department = Normalize(staff.Department);
        profile.DepartmentDescription = Normalize(staff.DepartmentDescription);
        profile.Designation = Normalize(staff.Designation);
        profile.Title = Normalize(staff.Title);
        profile.Source = "NIE staff directory";
        profile.LastVerifiedOn = DateTimeHelper.Now;
        profile.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
