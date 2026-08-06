# BT DocCenter — Backend (ASP.NET Core)

API REST de **BT DocCenter**, le système de gestion de dossiers clients bancaires. Elle expose la logique métier, gère l'authentification, le stockage des documents, les notifications temps réel (SignalR), l'intégration avec le pipeline SSIS et des fonctionnalités avancées (OCR, IA, détection de fraude, rapports).

> 📖 Une documentation détaillée (endpoints complets, schéma de base, DTOs, middlewares) est disponible dans [`DOCUMENTATION.md`](DOCUMENTATION.md).

## Aperçu des technologies

| Technologie | Version |
|-------------|---------|
| .NET | 8.0 |
| ASP.NET Core Web API | 8.0 |
| Entity Framework Core | 8.0 (SQL Server) |
| Authentification | JWT Bearer (HMAC-SHA256) |
| SignalR | temps réel (notifications, messagerie, cours) |
| AutoMapper | 12 |
| BCrypt.Net-Next | 4 |
| MailKit | 4.8 (emails SMTP) |
| EPPlus | 7.5 (Excel) |
| QuestPDF | 2024.12 (PDF) |
| Tesseract | 5.2 (OCR français + arabe) |
| Swagger | 6.6 |

## Prérequis

- **.NET SDK 8.0**
- **SQL Server** (SQL Server LocalDB ou une instance distante)
- **Visual Studio 2022** (optionnel, mais recommandé) ou VS Code + C# Dev Kit

## Démarrage rapide

```bash
# 1. Se placer dans le projet API
cd Backend.API

# 2. Créer le fichier de configuration depuis le modèle
copy .env.example .env
# puis éditer .env (chaîne de connexion, clé JWT, ...)

# 3. Lancer l'API
dotnet run
```

Au démarrage, l'application :
1. **Applique automatiquement les migrations EF Core** (crée le schéma `BT_ClientCore` s'il n'existe pas)
2. **Seed le compte Admin initial** : matricule `EMP-0001-A`, mot de passe `Admin@123` (configurable)

L'API est disponible sur **http://localhost:5184** (Swagger : `http://localhost:5184/swagger`).

## Configuration

### Fichier `.env` (secrets — non versionné)

Le projet charge un fichier `.env` depuis `Backend.API/` (via `DotNetEnv`). Copier `.env.example` et ajuster :

| Variable | Description |
|----------|-------------|
| `CONNECTIONSTRINGS__DEFAULTCONNECTION` | Chaîne de connexion SQL Server |
| `JWT__KEY` | Clé secrète de signature JWT (64 caractères hexadécimaux en production) |
| `JWT__ISSUER` / `JWT__AUDIENCE` | Émetteur / audience du token |
| `ALLOWEDORIGINS__0` | Origine CORS autorisée (ex : `http://localhost:4200`) |
| `SMTP__*` | Paramètres SMTP pour l'envoi d'emails |
| `ADMIN__PASSWORD` | Mot de passe de l'admin initial |
| `LLM__APIKEY` | Clé de l'API LLM (Groq/OpenAI/...), laisser vide pour le mode repli |

### `appsettings.json` (configuration non sensible)

| Section | Clés | Description |
|---------|------|-------------|
| `ConnectionStrings` | `DefaultConnection` | Chaîne de connexion par défaut |
| `Jwt` | `Key`, `Issuer`, `Audience` | Paramètres JWT |
| `AllowedOrigins` | tableau | Origines CORS autorisées |
| `Admin` | `Password` | Mot de passe admin initial |
| `Siss` | `PlanningHeure`, `DtexecPath`, `PackagePath`, `TimeoutSeconds` | Planification et exécution du package SSIS |
| `CoursTempsReel` | `ApiUrl`, `CacheMinutes`, `RefreshSeconds` | Cours de change depuis une API externe |
| `Llm` | `ApiKey`, `BaseUrl`, `Model` | Assistant IA compatible OpenAI |
| `Tesseract` | `TessDataPath`, `Language` | OCR (français + arabe) |

## Architecture (Clean Architecture en 4 couches)

```
Backend.API   → Couche présentation (Controllers, Hubs SignalR, Middlewares, Services hébergés)
    │
    └── Backend.BL  → Couche métier (Services, Interfaces, Mapping AutoMapper, Exceptions)
            │
            └── Backend.DAL  → Couche données (DbContext, Repositories, Entités, Migrations, Seed)
                    │
                    └── Backend.DTO  → Objets de transfert partagés
```

Dépendances : `API → BL → DAL → DTO`

### Structure

```
BT_DossierBackend/
├── Backend.API/               # API + Program.cs (DI, pipeline, seed, Swagger)
│   ├── Controllers/           # ~27 contrôleurs REST
│   ├── Hubs/                  # NotificationHub, MessagesHub, CoursHub (SignalR)
│   ├── Middlewares/           # ExceptionHandling, RateLimiting
│   ├── Services/              # Services hébergés (reminders, synchro SISS, cours)
│   └── wwwroot/uploads/       # Fichiers statiques (photos, uploads)
├── Backend.BL/                # ~30 services métier + interfaces + Mapping
├── Backend.DAL/               # ~30 entités, 22 repositories, 11 migrations, Seed
└── Backend.DTO/               # DTOs par domaine (Auth, Client, Finance, ...)
```

## Endpoints (vue d'ensemble)

Base URL : `http://localhost:{port}/api` — Auth : `Authorization: Bearer <JWT>`

| Domaine | Préfixe | Exemples |
|---------|---------|----------|
| Authentification | `/auth` | `login`, `register`, `refresh-token`, `forgot-password`, `change-password`, `profile` |
| Utilisateurs & rôles | `/users`, `/roles` | approbation, activation, assignation de rôles |
| Clients | `/clients` | CRUD, recherche, import/export Excel, stats, archivage |
| Documents | `/documents` | upload, téléchargement, validation, OCR |
| Statut client | `/clients/{id}/clientstatus` | rapport de complétion, blocage/déblocage |
| Templates | `/documenttemplates` | documents requis par type de client |
| Portail client | `/clientportal` | invitation, upload client (accès public par token) |
| Tableau de bord | `/dashboard` | KPIs, évolutions, répartitions, alertes |
| Recherche | `/search`, `/advancedsearch` | recherche globale, multi-critères, suggestions, tags |
| Rappels | `/reminders` | CRUD, équipe, stats, workflow de validation |
| Finance | `/comptes`, `/mouvements`, `/cours` | données SISS en lecture seule, cours temps réel |
| Relevés & rapports | `/releve`, `/reports` | PDF / Excel |
| Messagerie | `/messages` | messages privés avec pièces jointes |
| Fraude & risque | `/fraud`, `/risk` | règles antifraude, alertes, score de risque |
| IA | `/ai` | assistant conversationnel, résumé de dossier |
| Synchronisation | `/synchronisation` | déclenchement manuel, état, historique, rejets |
| Activité | `/activity` | journal d'activité utilisateurs |
| Notifications | `/notifications` | notifications in-app + temps réel |

> La liste **exhaustive** des endpoints est documentée dans [`DOCUMENTATION.md`](DOCUMENTATION.md).

## Services hébergés (Background Services)

| Service | Fréquence | Rôle |
|---------|-----------|------|
| `ReminderOverdueService` | toutes les 5 min | Notifie les rappels en retard (in-app + email) |
| `SissSynchronisationHostedService` | toutes les 30 s (fenêtre de planification) | Déclenche le package SSIS quotidiennement (`Siss:PlanningHeure`) |
| `CoursTempsReelHostedService` | toutes les 60 s | Actualise les cours de change + diffusion SignalR |

## Base de données

- Base : **`BT_ClientCore`** (SQL Server)
- ORM : Entity Framework Core 8, **Code First** — les migrations s'appliquent automatiquement au démarrage
- Entités principales : `User` (`AppReg_Users`), `Client`, `DetailClient`, `DocumentClient`, `DocumentTemplate`, `HistoriqueModification`, `Notification`, `Reminder`, `Compte`, `Cours`, `Mouvement` (`MVT`), `SynchronisationLog`, `SynchronisationRejet`, `MessagePrive`, `WorkflowValidation`, `RegleAntifraude`, `ScoreRisque`, ...

### Migrations

```bash
cd Backend.DAL
dotnet ef migrations add NomDeLaMigration
dotnet ef database update
```

## Sécurité

- Mots de passe hashés **BCrypt**
- Tokens JWT à courte durée (15 min) + **refresh tokens** (7 jours, révocables)
- **Rate limiting** : 10 req/min sur l'auth, 60 req/min sur les endpoints sensibles
- **CORS** restreint aux origines configurées
- Middleware global d'exceptions (aucune stacktrace exposée en production)
- Portail client : tokens opaques tracés (IP, compteur d'accès)
- Autorisation par rôles (`Admin`, `Superviseur`, `Agent`) via `[Authorize(Roles = ...)]`

## Intégrations

### Avec le frontend Angular

| Ressource | URL |
|-----------|-----|
| API REST | `http://localhost:5184/api` |
| Fichiers statiques | `http://localhost:5184/uploads/` |
| Hubs SignalR | `/hubs/notifications`, `/hubs/messages`, `/hubs/cours` |

### Avec le package SSIS (SISS)

**SSIS est l'unique point d'entrée ETL.** Le backend se contente de déclencher le package `Master.dtsx` via `dtexec.exe` et de tracer le résultat :

1. Création d'un `SynchronisationLog` (statut `En cours`)
2. Exécution de `dtexec.exe /F "<Siss:PackagePath>"`
3. Mise à jour du log selon le code de retour (0 = succès)
4. Notifications aux admins (in-app + SignalR)

Déclenchements : **manuel** (`POST /api/synchronisation/declencher`) ou **planifié** (quotidien à `Siss:PlanningHeure`).
