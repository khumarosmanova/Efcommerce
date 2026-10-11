using System.Security.Claims;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Identity.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly JwtSettings _jwtSettings;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        ITokenService tokenService,
        AppDbContext context,
        ICurrentUserService currentUserService,
        IOptions<JwtSettings> jwtOptions,
        IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _context = context;
        _currentUserService = currentUserService;
        _jwtSettings = jwtOptions.Value;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ValidationException("An account with this email address already exists.");
        }

        // Ensure default Customer role exists
        const string defaultRole = "Customer";
        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
            await _roleManager.CreateAsync(new AppRole(defaultRole, "Standard customer role"));
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(e => e.Description).ToList());
        }

        await _userManager.AddToRoleAsync(user, defaultRole);

        // Send welcome email notification in the background
        _ = Task.Run(() => _emailService.SendWelcomeEmailAsync(user.Email!, $"{user.FirstName} {user.LastName}".Trim(), CancellationToken.None));

        var roles = new List<string> { defaultRole };
        var permissions = await GetPermissionsForRolesAsync(roles);

        return await GenerateAuthResponseAsync(user, roles, permissions, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("This account has been locked. Please contact support.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var permissions = await GetPermissionsForRolesAsync(roles);

        return await GenerateAuthResponseAsync(user, roles, permissions, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            throw new UnauthorizedException("Invalid access token.");
        }

        var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                           principal.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedException("Invalid token payload.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new UnauthorizedException("User not found.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("This account has been locked. Please contact support.");
        }

        var storedRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken && r.UserId == userId, cancellationToken);

        if (storedRefreshToken == null || !storedRefreshToken.IsActive)
        {
            throw new UnauthorizedException("Refresh token is invalid, expired, or revoked.");
        }

        // Revoke the old refresh token
        storedRefreshToken.RevokedAt = DateTime.UtcNow;

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var permissions = await GetPermissionsForRolesAsync(roles);

        var (newAccessToken, expiration) = _tokenService.GenerateAccessToken(user, roles, permissions);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

        storedRefreshToken.ReplacedByToken = newRefreshTokenValue;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenValue,
            AccessTokenExpiration = expiration,
            User = new CurrentUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                Permissions = permissions
            }
        };
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User was not found.");
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var permissions = await GetPermissionsForRolesAsync(roles);

        return new CurrentUserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            Roles = roles,
            Permissions = permissions
        };
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(
        AppUser user,
        List<string> roles,
        List<string> permissions,
        CancellationToken cancellationToken)
    {
        var (accessToken, expiration) = _tokenService.GenerateAccessToken(user, roles, permissions);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiration = expiration,
            User = new CurrentUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                Permissions = permissions
            }
        };
    }

    private async Task<List<string>> GetPermissionsForRolesAsync(IEnumerable<string> roles)
    {
        var permissionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in claims.Where(c => c.Type == "permission"))
                {
                    if (!string.IsNullOrWhiteSpace(claim.Value))
                    {
                        permissionSet.Add(claim.Value);
                    }
                }
            }
        }

        return permissionSet.ToList();
    }
}
