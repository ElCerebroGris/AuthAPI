using AuthAPI;
using AuthAPI.DTOs;
using AuthAPI.Entities;
using AuthAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Supabase.Gotrue;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors();

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
    options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
})
.AddJwtBearer(IdentityConstants.BearerScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
    };
});
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new ()
    {
        Title = "BayQi Authentication API",
        Version = "v1"
    });
});

builder.Services.Configure<SupabaseSettings>(
    builder.Configuration.GetSection("Supabase"));

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("TodoList"));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPhoneOtpService, PhoneOtpService>();
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();
builder.Services.AddSingleton<SupabaseImageStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();

var authItens = app.MapGroup("/auth");

authItens.MapPost("/register", Register);
authItens.MapPost("/login", Login);
authItens.MapPost("/login-bi", LoginWithBi);
authItens.MapPost("/register-bi", RegisterWithBi);

authItens.MapGet("/me", GetAllTodos).RequireAuthorization();

app.Run();

static async Task<IResult> Register(RegisterCustomerUserRequest request, IUserService userService, 
    SupabaseImageStorageService _imageStorage)
{
    try
    {
        if (request.ProfileFile != null)
        {
            var uploadResult = await _imageStorage.UploadImageBytesAsync(request.ProfileFile, "UserPhoto");

            if (!uploadResult.IsSuccess)
                return TypedResults.BadRequest(uploadResult.ErrorMessage);

            request.Profile = uploadResult.PublicUrl!;
        }

        if (request.DiFrontalImageFile != null)
        {
            var uploadResult = await _imageStorage.UploadImageBytesAsync(request.DiFrontalImageFile, "UserPhoto");

            if (!uploadResult.IsSuccess)
                return TypedResults.BadRequest(uploadResult.ErrorMessage);

            request.DiFrontalImage = uploadResult.PublicUrl!;
        }

        if (request.DiBackImageFile != null)
        {
            var uploadResult = await _imageStorage.UploadImageBytesAsync(request.DiBackImageFile, "UserPhoto");

            if (!uploadResult.IsSuccess)
                return TypedResults.BadRequest(uploadResult.ErrorMessage);

            request.DiBackImage = uploadResult.PublicUrl!;
        }

        var user = await userService.RegisterCustomerUserAsync(request);

        return TypedResults.Ok(user);
    }
    catch (Exception ex)
    {
        return TypedResults.BadRequest(new { message = ex.Message });
    }
}

static async Task<IResult> Login(LoginRequest request, IUserService userService)
{
    try
    {
        var result = await userService.LoginCustomerUserAsync(request.PhoneNumber, request.Password);

        return TypedResults.Ok(result);
    }
    catch (UnauthorizedAccessException)
    {
        return TypedResults.Unauthorized();
    }
    catch (Exception ex)
    {
        return TypedResults.BadRequest(new { message = ex.Message });
    }
}

static async Task<IResult> LoginWithBi(LoginByBIRequest request, IUserService userService)
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        var result = await userService.LoginCustomerUserByBiAsync(request.BiNumber, request.Password);

        return TypedResults.Ok(result);
    }
    catch (UnauthorizedAccessException)
    {
        return TypedResults.Unauthorized();
    }
    catch (Exception ex)
    {
        return TypedResults.BadRequest(new { message = ex.Message });
    }
}

static async Task<IResult> RegisterWithBi([FromForm] RegisterCustomerByBIRequest request, IUserService userService,
    SupabaseImageStorageService _imageStorage)
{
    try
    {
        var registerRequest = request.ToRegisterCustomerUserDTO();

        if (registerRequest.ProfileFile != null)
        {
            var uploadResult = await _imageStorage.UploadImageBytesAsync(registerRequest.ProfileFile, "UserPhoto");

            if (!uploadResult.IsSuccess)
                return TypedResults.BadRequest(uploadResult.ErrorMessage);

            registerRequest.Profile = uploadResult.PublicUrl!;
        }

        if (registerRequest.DiFrontalImageFile != null)
        {
            var uploadResult = await _imageStorage.UploadImageBytesAsync(registerRequest.DiFrontalImageFile, "UserPhoto");

            if (!uploadResult.IsSuccess)
                return TypedResults.BadRequest(uploadResult.ErrorMessage);

            registerRequest.DiFrontalImage = uploadResult.PublicUrl!;
        }

        if (registerRequest.DiBackImageFile != null)
        {
            var uploadResult = await _imageStorage.UploadImageBytesAsync(registerRequest.DiBackImageFile, "UserPhoto");

            if (!uploadResult.IsSuccess)
                return TypedResults.BadRequest(uploadResult.ErrorMessage);

            registerRequest.DiBackImage = uploadResult.PublicUrl!;
        }

        var user = await userService.RegisterCustomerUserByBiAsync(registerRequest);
        var createdUserId = user.GetType().GetProperty("Id")?.GetValue(user)?.ToString();

        //await _minorGuardianService.CreateInvitationAsync(
        //    createdUserId,
        //    new CreateGuardianInvitationRequest
        //    {
        //        GuardianPhoneNumber = registerRequest.LegalRepresentativePhoneNumber,
        //        GuardianName = registerRequest.LegalRepresentativeName,
        //        RelationshipType = registerRequest.LegalRepresentativeType
        //    });

        return TypedResults.Ok(user);
    }
    catch (Exception ex)
    {
        return TypedResults.BadRequest(new { message = ex.Message });
    }
}

static async Task<IResult> GetAllTodos(AppDbContext db)
{
    return TypedResults.Ok(await db.Todos.ToArrayAsync());
}