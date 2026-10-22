using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Firmeza.Application.DTOs.Auth;
using Firmeza.Application.Interfaces.Auth;
using Firmeza.Application.Interfaces.Repositories;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;
using Firmeza.Infrastructure.Identity;
using Firmeza.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Firmeza.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly JwtSettings _jwtSettings;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<JwtSettings> jwtSettings,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtSettings = jwtSettings.Value;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new DomainException($"El correo '{request.Email}' ya se encuentra registrado.");

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new DomainException($"Error al registrar el usuario: {errors}");
        }

        var roleName = string.IsNullOrWhiteSpace(request.Role) ? "Cliente" : request.Role.Trim();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new ApplicationRole(roleName));
        }

        await _userManager.AddToRoleAsync(user, roleName);

        // Asegurar que exista entidad Customer asociada para transacciones de compra
        var existingCustomer = await _customerRepository.GetByEmailAsync(user.Email, cancellationToken);
        Guid customerId;
        if (existingCustomer is null)
        {
            var docNumber = !string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? request.PhoneNumber.Trim()
                : "CC" + user.Id.ToString()[..8].ToUpper();
            
            var identification = new IdentificationDocument(IdentificationType.NationalIdentity, docNumber);
            var newCustomer = new Customer(
                user.FirstName,
                user.LastName,
                identification,
                new Email(user.Email),
                !string.IsNullOrWhiteSpace(user.PhoneNumber) ? new PhoneNumber("+57", user.PhoneNumber) : null);
            
            await _customerRepository.AddAsync(newCustomer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            customerId = newCustomer.Id;
        }
        else
        {
            customerId = existingCustomer.Id;
        }

        // Notificación de Bienvenida vía SMTP (TASK 7)
        try
        {
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, cancellationToken);
        }
        catch
        {
            // Falla de envío SMTP no bloquea registro
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiration) = GenerateJwtToken(user, roles);

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            UserId = user.Id,
            CustomerId = customerId,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToList()
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            throw new DomainException("Credenciales inválidas o usuario inactivo.");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            throw new DomainException("Credenciales inválidas o usuario inactivo.");

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiration) = GenerateJwtToken(user, roles);

        // Resolver CustomerId asociado al usuario
        var customer = await _customerRepository.GetByEmailAsync(user.Email ?? string.Empty, cancellationToken);
        Guid? customerId = customer?.Id;

        if (customerId is null && (roles.Contains("Cliente") || roles.Contains("User")))
        {
            var docNumber = !string.IsNullOrWhiteSpace(user.PhoneNumber)
                ? user.PhoneNumber.Trim()
                : "CC" + user.Id.ToString()[..8].ToUpper();

            var identification = new IdentificationDocument(IdentificationType.NationalIdentity, docNumber);
            var newCustomer = new Customer(
                user.FirstName,
                user.LastName,
                identification,
                new Email(user.Email ?? $"{user.Id}@firmeza.com"),
                !string.IsNullOrWhiteSpace(user.PhoneNumber) ? new PhoneNumber("+57", user.PhoneNumber) : null);

            await _customerRepository.AddAsync(newCustomer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            customerId = newCustomer.Id;
        }

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            UserId = user.Id,
            CustomerId = customerId,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToList()
        };
    }

    private (string token, DateTime expiration) GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: creds);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(tokenDescriptor);

        return (tokenString, expiration);
    }
}
