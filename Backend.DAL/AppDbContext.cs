using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Backend.DAL.Entities;

namespace Backend.DAL
{
    /// Contexte EF Core de l'application
    public class AppDbContext : DbContext
    {
        /// Initialise un nouveau contexte avec les options fournies
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        ///Table des utilisateurs.
        public DbSet<User> Users => Set<User>();

        ///Table des rôles.
        public DbSet<Role> Roles => Set<Role>();

        /// Table de liaison User-Role
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        ///Table des clients.
        public DbSet<Client> Clients => Set<Client>();

        ///Table des détails clients.
        public DbSet<DetailClient> DetailClients => Set<DetailClient>();

        ///Table des documents clients.
        public DbSet<DocumentClient> DocumentClients => Set<DocumentClient>();

        /// Table d'historique des modifications.
        public DbSet<HistoriqueModification> HistoriqueModifications => Set<HistoriqueModification>();
        public DbSet<Notification> Notifications => Set<Notification>();

        /// Table d'historique des recherches
        public DbSet<SearchHistory> SearchHistories => Set<SearchHistory>();

        /// Tables des tags
        public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();
        public DbSet<ClientTag> ClientTags => Set<ClientTag>();

        /// Templates de documents requis par type de client
        public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();

        /// Jetons d'accès portail client self-service
        public DbSet<ClientPortalToken> ClientPortalTokens => Set<ClientPortalToken>();

        /// Rappels et tâches agenda
        public DbSet<Reminder> Reminders => Set<Reminder>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration de l'entité User
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Matricule)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configuration de l'entité Role
            modelBuilder.Entity<Role>()
                .HasKey(r => r.Id);

            // Clé primaire sur UserRole
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => ur.UserRoleId);

            // Relation UserRole -> User
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            // Relation UserRole -> Role
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Configuration Client
            modelBuilder.Entity<Client>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.CodeClient)
                .IsUnique();

            var statutConverter = new ValueConverter<StatutClient, string>(
                v => v.ToString(),
                v => ParseStatut(v));

            modelBuilder.Entity<Client>()
                .Property(c => c.Statut)
                .HasConversion(statutConverter)
                .HasMaxLength(20);

            // Configuration DetailClient
            modelBuilder.Entity<DetailClient>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<DetailClient>()
                .HasIndex(d => d.IdClient)
                .IsUnique();

            // Relation Client -> DetailClient (One-to-One)
            modelBuilder.Entity<DetailClient>()
                .HasOne(d => d.Client)
                .WithOne(c => c.Detail)
                .HasForeignKey<DetailClient>(d => d.IdClient)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DetailClient>()
                .Property(d => d.RevenuMensuel)
                .HasPrecision(18, 2);

            // Configuration DocumentClient
            modelBuilder.Entity<DocumentClient>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<DocumentClient>()
                .HasOne(d => d.Client)
                .WithMany()
                .HasForeignKey(d => d.IdClient)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuration SearchHistory
            modelBuilder.Entity<SearchHistory>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<SearchHistory>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SearchHistory>()
                .HasIndex(s => new { s.UserId, s.SearchDate });

            // Configuration DocumentTag
            modelBuilder.Entity<DocumentTag>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<DocumentTag>()
                .HasOne(t => t.Document)
                .WithMany()
                .HasForeignKey(t => t.IdDocument)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentTag>()
                .HasIndex(t => t.Tag);

            // Configuration ClientTag
            modelBuilder.Entity<ClientTag>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<ClientTag>()
                .HasOne(t => t.Client)
                .WithMany()
                .HasForeignKey(t => t.IdClient)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientTag>()
                .HasIndex(t => t.Tag);

            // Configuration DocumentTemplate
            modelBuilder.Entity<DocumentTemplate>()
                .HasKey(dt => dt.Id);

            modelBuilder.Entity<DocumentTemplate>()
                .HasIndex(dt => dt.TypeClient);

            modelBuilder.Entity<DocumentTemplate>()
                .HasIndex(dt => new { dt.TypeClient, dt.NomDocument })
                .IsUnique();

            modelBuilder.Entity<DocumentTemplate>()
                .Property(dt => dt.TypeClient)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<DocumentTemplate>()
                .Property(dt => dt.NomDocument)
                .HasMaxLength(100)
                .IsRequired();

            // Données initiales : templates par défaut pour les 3 types de clients
            modelBuilder.Entity<DocumentTemplate>().HasData(
                // Particulier
                new DocumentTemplate { Id = 1,  TypeClient = "Particulier", NomDocument = "Carte d'Identité Nationale (CIN)", Description = "Recto et verso en cours de validité", EstObligatoire = true,  Ordre = 1, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 2,  TypeClient = "Particulier", NomDocument = "Justificatif de domicile", Description = "Facture eau/électricité ou attestation de résidence (-3 mois)", EstObligatoire = true,  Ordre = 2, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 3,  TypeClient = "Particulier", NomDocument = "Fiche de paie",           Description = "3 derniers bulletins de salaire",                                  EstObligatoire = true,  Ordre = 3, FormatsAcceptes = "pdf",           TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 4,  TypeClient = "Particulier", NomDocument = "RIB bancaire",            Description = "Relevé d'identité bancaire",                                     EstObligatoire = true,  Ordre = 4, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 5,  TypeClient = "Particulier", NomDocument = "Photo d'identité",        Description = "Photo récente fond blanc",                                       EstObligatoire = false, Ordre = 5, FormatsAcceptes = "jpg,jpeg,png",    TailleMaxMo = 2,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                // Entreprise
                new DocumentTemplate { Id = 6,  TypeClient = "Entreprise",  NomDocument = "Registre de commerce (RC)", Description = "Extrait du registre de commerce",                             EstObligatoire = true,  Ordre = 1, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 10, IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 7,  TypeClient = "Entreprise",  NomDocument = "Statuts de la société",    Description = "Statuts constitutifs signés et certifiés",                     EstObligatoire = true,  Ordre = 2, FormatsAcceptes = "pdf",           TailleMaxMo = 10, IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 8,  TypeClient = "Entreprise",  NomDocument = "NIF / Identifiant fiscal", Description = "Numéro d'identification fiscale",                               EstObligatoire = true,  Ordre = 3, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 9,  TypeClient = "Entreprise",  NomDocument = "Bilan comptable",          Description = "Bilan des 2 derniers exercices",                               EstObligatoire = true,  Ordre = 4, FormatsAcceptes = "pdf,xlsx,xls",  TailleMaxMo = 20, IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 10, TypeClient = "Entreprise",  NomDocument = "CIN du gérant",            Description = "Pièce d'identité du représentant légal",                       EstObligatoire = true,  Ordre = 5, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 11, TypeClient = "Entreprise",  NomDocument = "RIB bancaire entreprise",  Description = "RIB du compte professionnel",                                  EstObligatoire = true,  Ordre = 6, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                // VIP
                new DocumentTemplate { Id = 12, TypeClient = "VIP",        NomDocument = "Passeport",                 Description = "Passeport en cours de validité",                               EstObligatoire = true,  Ordre = 1, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 13, TypeClient = "VIP",        NomDocument = "Justificatif de patrimoine", Description = "Titre de propriété ou relevé d'investissement",               EstObligatoire = true,  Ordre = 2, FormatsAcceptes = "pdf",           TailleMaxMo = 20, IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 14, TypeClient = "VIP",        NomDocument = "Déclaration de revenus",     Description = "Déclaration fiscale des 2 dernières années",                  EstObligatoire = true,  Ordre = 3, FormatsAcceptes = "pdf",           TailleMaxMo = 10, IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 15, TypeClient = "VIP",        NomDocument = "RIB bancaire VIP",           Description = "RIB compte bancaire principal",                               EstObligatoire = true,  Ordre = 4, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 16, TypeClient = "VIP",        NomDocument = "Lettre de recommandation",   Description = "Lettre d'un partenaire ou conseiller financier",               EstObligatoire = false, Ordre = 5, FormatsAcceptes = "pdf",           TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Configuration ClientPortalToken
            modelBuilder.Entity<ClientPortalToken>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<ClientPortalToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            modelBuilder.Entity<ClientPortalToken>()
                .HasIndex(t => t.IdClient);

            modelBuilder.Entity<ClientPortalToken>()
                .HasOne(t => t.Client)
                .WithMany()
                .HasForeignKey(t => t.IdClient)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientPortalToken>()
                .Property(t => t.Token)
                .HasMaxLength(200)
                .IsRequired();

            // Configuration Reminder
            modelBuilder.Entity<Reminder>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.Client)
                .WithMany()
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.AssignedToUser)
                .WithMany()
                .HasForeignKey(r => r.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.CreatedByUser)
                .WithMany()
                .HasForeignKey(r => r.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reminder>()
                .HasIndex(r => new { r.AssignedToUserId, r.DueDate });

            modelBuilder.Entity<Reminder>()
                .HasIndex(r => new { r.CreatedByUserId, r.Status });

            // Données initiales des rôles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", IsActive = true },
                new Role { Id = 2, Name = "Agent", IsActive = true },
                new Role { Id = 3, Name = "Superviseur", IsActive = true }
            );
        }
        private static StatutClient ParseStatut(string value)
        {
            value = value.Replace('�', 'é');

            if (Enum.TryParse<StatutClient>(value, ignoreCase: true, out var result))
                return result;

            return value switch
            {
                "En attente" => StatutClient.EnAttente,
                "Bloque" => StatutClient.Bloqué,
                _ => throw new ArgumentException($"Valeur de statut non reconnue : '{value}'")
            };
        }
    }
}
