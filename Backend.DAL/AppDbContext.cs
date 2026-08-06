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

        /// Comptes bancaires (SISS, lecture seule)
        public DbSet<Compte> Comptes => Set<Compte>();

        /// Cours de change (SISS, lecture seule)
        public DbSet<Cours> Cours => Set<Cours>();

        /// Mouvements bancaires (SISS, lecture seule)
        public DbSet<Mouvement> Mouvements => Set<Mouvement>();

        /// Journal des synchronisations application ↔ SISS
        public DbSet<SynchronisationLog> SynchronisationLogs => Set<SynchronisationLog>();

        /// Lignes rejetées des synchronisations
        public DbSet<SynchronisationRejet> SynchronisationRejets => Set<SynchronisationRejet>();

        /// Workflows de validation multi-niveaux des documents
        public DbSet<WorkflowValidation> WorkflowValidations => Set<WorkflowValidation>();

        /// Étapes de validation des workflows
        public DbSet<EtapeValidation> EtapeValidations => Set<EtapeValidation>();

        /// Fil d'activité (timeline) des clients
        public DbSet<EvenementClient> EvenementClients => Set<EvenementClient>();

        /// Notes internes des dossiers clients
        public DbSet<NoteClient> NoteClients => Set<NoteClient>();

        /// Contacts associés aux dossiers clients
        public DbSet<ContactClient> ContactClients => Set<ContactClient>();

        /// Règles de détection de fraude (configurables)
        public DbSet<RegleAntifraude> ReglesAntifraude => Set<RegleAntifraude>();

        /// Alertes de fraude / anomalie générées par le moteur de détection
        public DbSet<AlertesFraude> AlertesFraudes => Set<AlertesFraude>();

        /// Scores de risque clients (avec historique)
        public DbSet<ScoreRisque> ScoresRisque => Set<ScoreRisque>();

        /// Messages privés en temps réel entre utilisateurs
        public DbSet<MessagePrive> MessagesPrives => Set<MessagePrive>();

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
                new DocumentTemplate { Id = 16, TypeClient = "VIP",        NomDocument = "Lettre de recommandation",   Description = "Lettre d'un partenaire ou conseiller financier",               EstObligatoire = false, Ordre = 5, FormatsAcceptes = "pdf",           TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                // Professionnel / Indépendant
                new DocumentTemplate { Id = 17, TypeClient = "Professionnel", NomDocument = "Carte d'Identité Nationale (CIN)", Description = "Recto et verso en cours de validité", EstObligatoire = true,  Ordre = 1, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 18, TypeClient = "Professionnel", NomDocument = "Matricule fiscal",           Description = "Attestation d'affectation du matricule fiscal",                 EstObligatoire = true,  Ordre = 2, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 19, TypeClient = "Professionnel", NomDocument = "Registre de commerce",        Description = "Extrait du registre de commerce (si assujetti)",                EstObligatoire = false, Ordre = 3, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 10, IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 20, TypeClient = "Professionnel", NomDocument = "RIB bancaire professionnel",  Description = "RIB du compte professionnel",                                   EstObligatoire = true,  Ordre = 4, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 21, TypeClient = "Professionnel", NomDocument = "Avis d'imposition",           Description = "Dernier avis d'imposition",                                    EstObligatoire = true,  Ordre = 5, FormatsAcceptes = "pdf",           TailleMaxMo = 10, IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                // Association / ONG
                new DocumentTemplate { Id = 22, TypeClient = "Association", NomDocument = "Récépissé de déclaration",      Description = "Récépissé de déclaration à la DGRH",                           EstObligatoire = true,  Ordre = 1, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 10, IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 23, TypeClient = "Association", NomDocument = "Statuts de l'association",      Description = "Statuts déposés et visés",                                     EstObligatoire = true,  Ordre = 2, FormatsAcceptes = "pdf",           TailleMaxMo = 10, IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 24, TypeClient = "Association", NomDocument = "Liste du bureau exécutif",      Description = "Composition du bureau et PV d'élection",                       EstObligatoire = true,  Ordre = 3, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 25, TypeClient = "Association", NomDocument = "CIN du président",              Description = "Pièce d'identité du président",                                EstObligatoire = true,  Ordre = 4, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DocumentTemplate { Id = 26, TypeClient = "Association", NomDocument = "RIB bancaire association",      Description = "RIB du compte association",                                    EstObligatoire = true,  Ordre = 5, FormatsAcceptes = "pdf,jpg,jpeg,png", TailleMaxMo = 5,  IsActive = true, DateCreation = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
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

            // Configuration Compte (SISS, lecture seule)
            modelBuilder.Entity<Compte>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Compte>()
                .HasIndex(c => c.IdClient);

            modelBuilder.Entity<Compte>()
                .HasIndex(c => new { c.NumCompte, c.Agence })
                .IsUnique();

            modelBuilder.Entity<Compte>()
                .Property(c => c.Solde)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Compte>()
                .Property(c => c.SoldeContreValeur)
                .HasPrecision(18, 2);

            // Configuration Cours (SISS, lecture seule)
            modelBuilder.Entity<Cours>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Cours>()
                .HasIndex(c => new { c.Journee, c.Devise })
                .IsUnique();

            modelBuilder.Entity<Cours>()
                .Property(c => c.Valeur)
                .HasPrecision(18, 2);

            // Configuration Mouvement (SISS, lecture seule)
            modelBuilder.Entity<Mouvement>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<Mouvement>()
                .Property(m => m.Montant)
                .HasPrecision(18, 2);

            // Configuration SynchronisationLog
            modelBuilder.Entity<SynchronisationLog>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<SynchronisationLog>()
                .HasIndex(s => s.DateDebut);

            // Configuration SynchronisationRejet
            modelBuilder.Entity<SynchronisationRejet>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<SynchronisationRejet>()
                .HasOne(r => r.Synchronisation)
                .WithMany()
                .HasForeignKey(r => r.IdSynchronisation)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SynchronisationRejet>()
                .HasIndex(r => r.IdSynchronisation);

            // Configuration WorkflowValidation
            modelBuilder.Entity<WorkflowValidation>()
                .HasKey(w => w.Id);

            modelBuilder.Entity<WorkflowValidation>()
                .HasOne(w => w.Document)
                .WithMany()
                .HasForeignKey(w => w.IdDocument)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkflowValidation>()
                .HasOne(w => w.CreatedBy)
                .WithMany()
                .HasForeignKey(w => w.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkflowValidation>()
                .HasIndex(w => new { w.IdDocument, w.StatutGlobal });

            modelBuilder.Entity<WorkflowValidation>()
                .HasIndex(w => w.EtapeCourante);

            modelBuilder.Entity<WorkflowValidation>()
                .Property(w => w.EtapeCourante)
                .HasMaxLength(30);

            modelBuilder.Entity<WorkflowValidation>()
                .Property(w => w.MotifRejet)
                .HasMaxLength(1000);

            // Configuration EtapeValidation
            modelBuilder.Entity<EtapeValidation>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<EtapeValidation>()
                .HasOne(e => e.Workflow)
                .WithMany(w => w.Etapes)
                .HasForeignKey(e => e.IdWorkflow)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EtapeValidation>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EtapeValidation>()
                .HasIndex(e => new { e.IdWorkflow, e.Ordre });

            // Données initiales des rôles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", IsActive = true },
                new Role { Id = 2, Name = "Agent", IsActive = true },
                new Role { Id = 3, Name = "Superviseur", IsActive = true }
            );

            // Configuration EvenementClient (timeline)
            modelBuilder.Entity<EvenementClient>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<EvenementClient>()
                .HasOne(e => e.Client)
                .WithMany()
                .HasForeignKey(e => e.IdClient)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EvenementClient>()
                .HasOne(e => e.Utilisateur)
                .WithMany()
                .HasForeignKey(e => e.UtilisateurId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EvenementClient>()
                .HasIndex(e => new { e.IdClient, e.DateEvenement });

            modelBuilder.Entity<EvenementClient>()
                .HasIndex(e => e.Type);

            // Configuration NoteClient
            modelBuilder.Entity<NoteClient>()
                .HasKey(n => n.Id);

            modelBuilder.Entity<NoteClient>()
                .HasOne(n => n.Client)
                .WithMany()
                .HasForeignKey(n => n.IdClient)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NoteClient>()
                .HasOne(n => n.CreePar)
                .WithMany()
                .HasForeignKey(n => n.CreeParUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NoteClient>()
                .HasIndex(n => new { n.IdClient, n.DateCreation });

            // Configuration ContactClient
            modelBuilder.Entity<ContactClient>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<ContactClient>()
                .HasOne(c => c.Client)
                .WithMany()
                .HasForeignKey(c => c.IdClient)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContactClient>()
                .HasIndex(c => new { c.IdClient, c.TypeContact });

            // Configuration RegleAntifraude
            modelBuilder.Entity<RegleAntifraude>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<RegleAntifraude>()
                .HasIndex(r => r.Code)
                .IsUnique();

            modelBuilder.Entity<RegleAntifraude>()
                .Property(r => r.Seuil)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RegleAntifraude>()
                .Property(r => r.SeuilSecondaire)
                .HasPrecision(18, 2);

            // Configuration AlertesFraude
            modelBuilder.Entity<AlertesFraude>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<AlertesFraude>()
                .HasOne(a => a.Client)
                .WithMany()
                .HasForeignKey(a => a.IdClient)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AlertesFraude>()
                .HasOne(a => a.Regle)
                .WithMany()
                .HasForeignKey(a => a.IdRegle)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AlertesFraude>()
                .HasOne(a => a.TraiteePar)
                .WithMany()
                .HasForeignKey(a => a.TraiteeParUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AlertesFraude>()
                .HasIndex(a => new { a.IdClient, a.DateDetection });

            modelBuilder.Entity<AlertesFraude>()
                .HasIndex(a => a.Statut);

            modelBuilder.Entity<AlertesFraude>()
                .HasIndex(a => a.Fingerprint);

            modelBuilder.Entity<AlertesFraude>()
                .Property(a => a.Montant)
                .HasPrecision(18, 2);

            // Configuration ScoreRisque
            modelBuilder.Entity<ScoreRisque>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<ScoreRisque>()
                .HasOne(s => s.Client)
                .WithMany()
                .HasForeignKey(s => s.IdClient)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScoreRisque>()
                .HasIndex(s => new { s.IdClient, s.DateCalcul });

            // Configuration MessagePrive (messagerie interne temps réel)
            modelBuilder.Entity<MessagePrive>()
                .HasIndex(m => new { m.ExpediteurUserId, m.DestinataireUserId, m.DateEnvoi });

            modelBuilder.Entity<MessagePrive>()
                .HasOne(m => m.Expediteur)
                .WithMany()
                .HasForeignKey(m => m.ExpediteurUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessagePrive>()
                .HasOne(m => m.Destinataire)
                .WithMany()
                .HasForeignKey(m => m.DestinataireUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Données initiales : règles antifraude par défaut
            modelBuilder.Entity<RegleAntifraude>().HasData(
                new RegleAntifraude { Id = 1, Code = "MONTANT_MAXIMAL", Nom = "Mouvement d'un montant élevé", Description = "Alerte si un mouvement dépasse le montant absolu configuré (en dinars).", Type = "MontantMaximal", Seuil = 50000m, PeriodeJours = 30, Poids = 30, Priorite = 1, Actif = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RegleAntifraude { Id = 2, Code = "MONTANT_ANORMAL", Nom = "Montant anormal vs historique", Description = "Alerte si un mouvement dépasse le multiplicateur du montant moyen du client.", Type = "MontantAnormal", Seuil = 5m, PeriodeJours = 90, Poids = 35, Priorite = 2, Actif = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RegleAntifraude { Id = 3, Code = "PIC_DEBIT", Nom = "Pic de débits", Description = "Alerte si le total débité sur une journée dépasse le multiplicateur de la moyenne journalière.", Type = "PicDebit", Seuil = 4m, PeriodeJours = 30, Poids = 25, Priorite = 3, Actif = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RegleAntifraude { Id = 4, Code = "MULTI_COMPTES", Nom = "Multiplication des comptes", Description = "Alerte si le client détient plus de comptes que le seuil configuré.", Type = "MultiComptes", Seuil = 3m, PeriodeJours = 0, Poids = 15, Priorite = 4, Actif = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RegleAntifraude { Id = 5, Code = "FREQUENCE", Nom = "Fréquence anormale de mouvements", Description = "Alerte si le nombre de mouvements dépasse le seuil sur la période.", Type = "Frequence", Seuil = 200m, PeriodeJours = 30, Poids = 15, Priorite = 5, Actif = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RegleAntifraude { Id = 6, Code = "DECOUPAGE_ANORMAL", Nom = "Structuration (découpage de fonds)", Description = "Alerte si plusieurs mouvements juste sous le seuil de déclaration se répètent sur le même compte (smurfing).", Type = "DecoupageAnormal", Seuil = 10000m, SeuilSecondaire = 7m, PeriodeJours = 14, Poids = 35, Priorite = 6, Actif = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RegleAntifraude { Id = 7, Code = "RAPID_IN_OUT", Nom = "Passage de fonds (aller-retour)", Description = "Alerte si un crédit est suivi d'un débit équivalent dans une fenêtre courte.", Type = "RapidInOut", Seuil = 5000m, SeuilSecondaire = 2m, PeriodeJours = 15, Poids = 30, Priorite = 7, Actif = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RegleAntifraude { Id = 8, Code = "COMPTE_REACTIVE", Nom = "Compte dormant réactivé", Description = "Alerte si un compte inactif depuis longtemps reçoit soudainement un crédit important.", Type = "CompteReactive", Seuil = 180m, SeuilSecondaire = 50000m, PeriodeJours = 200, Poids = 30, Priorite = 8, Actif = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RegleAntifraude { Id = 9, Code = "ACTIVITE_NOCTURNE", Nom = "Activité hors horaires bancaires", Description = "Alerte si de nombreux mouvements ont lieu la nuit ou le week-end.", Type = "ActiviteNocturne", Seuil = 5m, SeuilSecondaire = 500m, PeriodeJours = 30, Poids = 20, Priorite = 9, Actif = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
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
