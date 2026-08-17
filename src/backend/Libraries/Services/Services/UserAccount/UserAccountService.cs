using Data.Data;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Dto;

namespace Services.Services;

/// <summary>
/// Administrative lifecycle for local accounts. See <see cref="IUserAccountService"/>.
/// </summary>
public class UserAccountService : IUserAccountService
{
    /// <summary>
    /// Fallback when <c>LocalIdentity:MinPasswordLength</c> is not configured for the Main API.
    /// Must stay in step with <c>LocalIdentityOptions.MinPasswordLength</c> in the Auth API -
    /// a shorter password accepted here would be refused at sign-in change time.
    /// </summary>
    private const int DefaultMinPasswordLength = 12;

    private readonly MainDbContext _context;
    private readonly IPasswordHasher<UserAccount> _passwordHasher;
    private readonly int _minPasswordLength;

    public UserAccountService(
        MainDbContext context,
        IPasswordHasher<UserAccount> passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _minPasswordLength = configuration.GetValue("LocalIdentity:MinPasswordLength", DefaultMinPasswordLength);
    }

    /// <inheritdoc />
    public async Task<List<UserAccountDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserAccounts
            .AsNoTracking()
            .OrderBy(account => account.UserId)
            .Select(account => new UserAccountDto
            {
                Id = account.Id,
                UserId = account.UserId,
                FullName = account.Name,
                Email = account.Email,
                Department = account.Department,
                IsActive = account.IsActive,
                MustChangePassword = account.MustChangePassword,
                LockoutEndOn = account.LockoutEndOn,
                LastLoginOn = account.LastLoginOn
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserAccountDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var account = await _context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return account is null ? null : ToDto(account);
    }

    /// <inheritdoc />
    public async Task<(bool Ok, string? Error, UserAccountDto? Account)> RegisterAsync(
        RegisterUserAccountDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = dto.UserId?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(userId))
            return (false, "A user ID is required.", null);

        if (userId.Length > 100)
            return (false, "User ID must be 100 characters or fewer.", null);

        if (string.IsNullOrEmpty(dto.InitialPassword))
            return (false, "An initial password is required.", null);

        if (dto.InitialPassword.Length < _minPasswordLength)
            return (false, $"Password must be at least {_minPasswordLength} characters long.", null);

        var taken = await _context.UserAccounts
            .AnyAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (taken)
            return (false, "That user ID is already taken.", null);

        var account = new UserAccount
        {
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(dto.FullName) ? userId : dto.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Department = string.IsNullOrWhiteSpace(dto.Department) ? null : dto.Department.Trim(),
            IsActive = true,
            // An administrator knows this password, so it is a handover value, not a secret the
            // user owns. Force it to be replaced on first use.
            MustChangePassword = true
        };

        account.PasswordHash = _passwordHasher.HashPassword(account, dto.InitialPassword);

        _context.UserAccounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return (true, null, ToDto(account));
    }

    /// <inheritdoc />
    public async Task<UserAccountDto?> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.UserAccounts
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (account is null)
            return null;

        account.IsActive = isActive;

        if (isActive)
        {
            // Approving an account should also clear a lockout left over from failed sign-ins,
            // otherwise the user is approved but still cannot get in.
            account.FailedLoginCount = 0;
            account.LockoutEndOn = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(account);
    }

    private static UserAccountDto ToDto(UserAccount account) => new()
    {
        Id = account.Id,
        UserId = account.UserId,
        FullName = account.Name,
        Email = account.Email,
        Department = account.Department,
        IsActive = account.IsActive,
        MustChangePassword = account.MustChangePassword,
        LockoutEndOn = account.LockoutEndOn,
        LastLoginOn = account.LastLoginOn
    };
}
