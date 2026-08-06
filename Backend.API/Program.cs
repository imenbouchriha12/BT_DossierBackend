using System.Text;
using Backend.BL.BL;
using Backend.BL.IBL;
using Backend.BL.Mapping;
using Backend.DAL;
using Backend.DAL.DAL;
using Backend.DAL.IDAL;
using Backend.DAL.Seed;
using Backend.API.Hubs;
using Backend.API.Middlewares;
using Backend.API.Services;
using OfficeOpenXml;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

// Point d'entrée de l'application
DotNetEnv.Env.Load();
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
var builder = WebApplication.CreateBuilder(args);

// --- Configuration des services ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configuration Swagger avec support JWT
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

QuestPDF.Settings.License = LicenseType.Community;

// Connexion à la base de données SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Injection des dépendances - Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IHistoriqueRepository, HistoriqueRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IClientPortalRepository, ClientPortalRepository>();
builder.Services.AddScoped<IDocumentTemplateRepository, DocumentTemplateRepository>();
builder.Services.AddScoped<IReminderRepository, ReminderRepository>();
builder.Services.AddScoped<ICompteRepository, CompteRepository>();
builder.Services.AddScoped<ICoursRepository, CoursRepository>();
builder.Services.AddScoped<IMouvementRepository, MouvementRepository>();
builder.Services.AddScoped<ISynchronisationRepository, SynchronisationRepository>();
builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IRegleAntifraudeRepository, RegleAntifraudeRepository>();
builder.Services.AddScoped<IAlertesFraudeRepository, AlertesFraudeRepository>();
builder.Services.AddScoped<IScoreRisqueRepository, ScoreRisqueRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Injection des dépendances - Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IHistoriqueService, HistoriqueService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReleveService, ReleveService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAdvancedSearchService, AdvancedSearchService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IClientPortalService, ClientPortalService>();
builder.Services.AddScoped<IDocumentTemplateService, DocumentTemplateService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<ICompteService, CompteService>();
builder.Services.AddScoped<ICoursService, CoursService>();
builder.Services.AddScoped<IMouvementService, MouvementService>();
builder.Services.AddScoped<ISynchronisationService, SynchronisationService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
// ✅ Cours de change en temps réel (source externe + repli SISS)
builder.Services.AddScoped<ICoursTempsReelService, CoursTempsReelService>();
builder.Services.AddHttpClient<CoursTempsReelService>();
// ✅ Moteur de détection de fraude / anomalie
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();
// ✅ Score de risque client
builder.Services.AddScoped<IRiskScoreService, RiskScoreService>();
// ✅ Assistant IA (chat RAG + résumé de dossier) via un LLM compatible OpenAI
builder.Services.AddScoped<IAiAssistantService, AiAssistantService>();
builder.Services.AddScoped<ILlmApiService, LlmApiService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddHttpClient<LlmApiService>();
// ✅ Gestionnaire automatique du statut client
builder.Services.AddScoped<ClientStatusManager>();

// Portal SignalR notifier (implémente IPortalNotifier depuis Backend.API.Services)
builder.Services.AddSingleton<IPortalNotifier, PortalNotifier>();

// ✅ Vérification automatique des rappels en retard (toutes les 5 minutes)
builder.Services.AddHostedService<ReminderOverdueService>();

// ✅ Synchronisation automatique application ↔ SISS (planifiée)
builder.Services.AddHostedService<SissSynchronisationHostedService>();

// ✅ Actualisation automatique des cours de change en temps réel (+ diffusion SignalR)
builder.Services.AddHostedService<CoursTempsReelHostedService>();

// Un service en arrière-plan qui échoue ne doit JAMAIS arrêter l'API :
// on ignore son exception pour que le host continue de fonctionner.
builder.Services.Configure<HostOptions>(options =>
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

// Authentification JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
    });

builder.Services.AddSignalR();

builder.Services.AddAuthorization();

// CORS (autorise Angular en développement)
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- Pipeline de traitement des requêtes ---

var app = builder.Build();

// Applique les migrations et seed le premier admin au démarrage
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    var adminPassword = builder.Configuration["Admin:Password"] ?? "Admin@123";
    await DbSeeder.SeedAdminAsync(context, adminPassword);
}

// Middleware de gestion globale des exceptions (en premier)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Rate limiting sur les endpoints auth (login/register) : 10 requêtes par minute par IP
app.UseMiddleware<RateLimitingMiddleware>();

// Middleware Swagger en développement
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");

app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<MessagesHub>("/hubs/messages");
app.MapHub<CoursHub>("/hubs/cours");

app.Run();
