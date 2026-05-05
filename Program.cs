using AuthAPI;
using AuthAPI.DTOs;
using AuthAPI.Entities;
using AuthAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

authItens.MapPost("/login", CreateTodo);
authItens.MapPost("/logout", CreateTodo).RequireAuthorization();
authItens.MapGet("/me", GetAllTodos).RequireAuthorization();
authItens.MapPut("/update", CreateTodo).RequireAuthorization();
authItens.MapGet("/exists", GetAllTodos).RequireAuthorization();
authItens.MapPost("/password-reset-with-otp", CreateTodo);
authItens.MapPost("/password-reset-with-email", CreateTodo);
authItens.MapPost("/password-reset-with-token", CreateTodo);

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

static async Task<IResult> GetAllTodos(AppDbContext db)
{
    return TypedResults.Ok(await db.Todos.ToArrayAsync());
}

static async Task<IResult> GetCompleteTodos(AppDbContext db)
{
    return TypedResults.Ok(await db.Todos.Where(t => t.IsComplete).ToListAsync());
}

static async Task<IResult> GetTodo(int id, AppDbContext db)
{
    return await db.Todos.FindAsync(id)
        is Todo todo
            ? TypedResults.Ok(todo)
            : TypedResults.NotFound();
}

static async Task<IResult> CreateTodo(Todo todo, AppDbContext db)
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/todoitems/{todo.Id}", todo);
}

static async Task<IResult> UpdateTodo(int id, Todo inputTodo, AppDbContext db)
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return TypedResults.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<IResult> DeleteTodo(int id, AppDbContext db)
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}