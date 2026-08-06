# Documentation Complète — BT_DossierBackend

> **Projet** : Système de gestion de dossiers clients bancaires (Lumina Finance)  
> **Technologie** : ASP.NET Core 8 · Entity Framework Core 8 · SQL Server · SignalR · JWT  
> **Architecture** : Clean Architecture en 4 couches (API, BL, DAL, DTO)

---

## Table des Matières

1. [Architecture du projet](#1-architecture-du-projet)
2. [Structure des dossiers](#2-structure-des-dossiers)
3. [Configuration et démarrage](#3-configuration-et-démarrage)
4. [Base de données — Entités et schéma](#4-base-de-données--entités-et-schéma)
5. [API REST — Endpoints par contrôleur](#5-api-rest--endpoints-par-contrôleur)
6. [Services métier (BL)](#6-services-métier-bl)
7. [Repositories (DAL)](#7-repositories-dal)
8. [DTOs (Data Transfer Objects)](#8-dtos-data-transfer-objects)
9. [Middlewares](#9-middlewares)
10. [SignalR — Notifications temps réel](#10-signalr--notifications-temps-réel)
11. [Services hébergés (Background Services)](#11-services-hébergés-background-services)
12. [Authentification et autorisation](#12-authentification-et-autorisation)
13. [Packages NuGet utilisés](#13-packages-nuget-utilisés)
14. [Fonctionnalités détaillées](#14-fonctionnalités-détaillées)

---

## 1. Architecture du projet

Le projet suit une **Clean Architecture** à 4 couches indépendantes :

```
Backend.API      ← Couche présentation (Controllers, Middlewares, Hubs, Services hébergés)
    │
    └── Backend.BL   ← Couche métier (Services, Interfaces, Mapping, Exceptions)
            │
            └── Backend.DAL  ← Couche données (DbContext, Repositories, Entités, Migrations)
                        │
                        └── Backend.DTO  ← Objets de transfert (partagés entre toutes les couches)
```

### Diagramme de dépendances

```
Backend.API  →  Backend.BL  →  Backend.DAL  →  Backend.DTO
```

Le projet `.sln` contient 4 projets ciblant tous `.NET 8.0`.

---

## 2. Structure des dossiers

```
BT_DossierBackend/
├── Backend.API/
│   ├── Controllers/         (20 contrôleurs REST)
│   ├── Hubs/                (NotificationHub.cs — SignalR)
│   ├── Middlewares/         (ExceptionHandling, RateLimiting)
│   ├── Services/            (Services hébergés en arrière-plan)
│   ├── App_Data/Documents/  (Stockage physique des fichiers uploadés)
│   ├── Program.cs           (Point d'entrée, configuration DI, pipeline)
│   └── appsettings.json
│
├── Backend.BL/
│   ├── BL/                  (24 services métier)
│   ├── IBL/                 (23 interfaces de services)
│   ├── Mapping/             (MappingProfile.cs — AutoMapper)
│   └── Exceptions/          (AccountDisabledException)
│
├── Backend.DAL/
│   ├── Entities/            (20 entités EF Core)
│   ├── DAL/                 (13 repositories)
│   ├── IDAL/                (13 interfaces de repositories)
│   ├── Migrations/          (2 migrations EF Core)
│   ├── Seed/                (DbSeeder — admin initial)
│   └── AppDbContext.cs
│
└── Backend.DTO/
    ├── Auth/                (DTOs authentification et utilisateurs)
    ├── Client/              (DTOs clients et documents)
    ├── Dashboard/           (DTOs tableau de bord)
    ├── Finance/             (DTOs comptes, mouvements, cours)
    ├── Historique/          (DTOs journal d'audit)
    ├── Notification/        (DTOs notifications)
    ├── Portal/              (DTOs portail client)
    ├── Reminder/            (DTOs rappels/agenda)
    ├── Reports/             (DTOs rapports et relevés)
    ├── Search/              (DTOs recherche avancée)
    └── Synchronisation/     (DTOs sync SISS)
```

---

## 3. Configuration et démarrage

### appsettings.json — clés de configuration

| Clé | Valeur par défaut | Description |
|-----|-------------------|-------------|
| `ConnectionStrings:DefaultConnection` | `(localdb)\MSSQLLocalDB;Database=BT_ClientCore` | Chaîne de connexion SQL Server |
| `Jwt:Key` | (à remplacer) | Clé secrète de signature JWT |
| `Jwt:Issuer` | `BackendAPI` | Émetteur du token JWT |
| `Jwt:Audience` | `BackendClient` | Audience du token JWT |
| `AllowedOrigins` | `http://localhost:4200` | Origines CORS autorisées (Angular) |
| `Smtp:Host` | (vide) | Serveur SMTP pour l'envoi d'emails |
| `Smtp:Port` | `587` | Port SMTP |
| `Smtp:FromEmail` | `noreply@lumina-finance.com` | Expéditeur des emails |
| `Admin:Password` | `Admin@123` | Mot de passe de l'administrateur initial |
| `Siss:PlanningHeure` | `02:30` | Heure de déclenchement automatique du package SSIS |
| `Siss:DtexecPath` | `C:\Program Files\Microsoft SQL Server\160\DTS\Binn\dtexec.exe` | Chemin de l'exécutable dtexec |
| `Siss:PackagePath` | `C:\SISS\Packages\BT_DossierSISS.dtsx` | Chemin du package SSIS (.dtsx) |
| `Siss:TimeoutSeconds` | `1800` | Timeout global d'exécution du package (30 min) |
| `CoursTempsReel:ApiUrl` | `https://open.er-api.com/v6/latest/TND` | API externe de cours de change |
| `CoursTempsReel:CacheMinutes` | `360` | Durée du cache des cours (6 heures) |
| `Tesseract:TessDataPath` | `tessdata` | Chemin des données OCR Tesseract |
| `Tesseract:Language` | `fra+ara` | Langues OCR (français + arabe) |

### Démarrage (Program.cs)

Au démarrage, l'application :
1. Charge les variables d'environnement via `.env`
2. Configure la licence EPPlus (NonCommercial) et QuestPDF (Community)
3. Enregistre tous les services (DI)
4. Configure JWT, SignalR, CORS, Swagger
5. **Applique automatiquement les migrations EF Core**
6. **Seed le premier compte Admin** (si absent)
7. Monte le pipeline : ExceptionHandling → RateLimiting → Swagger → CORS → Auth → Controllers → SignalR Hub

### Variables d'environnement (.env)

Le projet supporte un fichier `.env` à la racine de `Backend.API/` pour les secrets (non versionné).

---

## 4. Base de données — Entités et schéma

Base de données : **SQL Server** — `BT_ClientCore`  
ORM : **Entity Framework Core 8** (Code First)

### Tableau des entités

| Entité | Table SQL | Description |
|--------|-----------|-------------|
| `User` | `dbo.AppReg_Users` | Utilisateurs de l'application |
| `Role` | *(auto)* | Rôles (Admin, Agent, Superviseur) |
| `UserRole` | *(auto)* | Table de liaison User ↔ Role |
| `Client` | `dbo.Client` | Clients bancaires |
| `DetailClient` | `dbo.DetailClient` | Détails complémentaires du client |
| `DocumentClient` | `dbo.DocumentClient` | Documents uploadés par/pour les clients |
| `DocumentTemplate` | `dbo.DocumentTemplates` | Templates de documents requis par type client |
| `HistoriqueModification` | `dbo.HistoriqueModifications` | Journal d'audit des modifications |
| `Notification` | `dbo.Notifications` | Notifications in-app des utilisateurs |
| `SearchHistory` | *(auto)* | Historique des recherches utilisateur |
| `DocumentTag` | *(auto)* | Tags sur les documents |
| `ClientTag` | *(auto)* | Tags sur les clients |
| `ClientPortalToken` | `dbo.ClientPortalTokens` | Jetons d'accès portail self-service client |
| `Reminder` | `dbo.Reminders` | Rappels et tâches agenda |
| `Compte` | `dbo.Compte` | Comptes bancaires (importés SISS, lecture seule) |
| `Cours` | `dbo.Cours` | Cours de change quotidiens (importés SISS) |
| `Mouvement` | `dbo.MVT` | Mouvements bancaires (importés SISS) |
| `SynchronisationLog` | `dbo.SynchronisationLog` | Journal des synchronisations SISS |
| `SynchronisationRejet` | *(auto)* | Lignes rejetées lors des synchronisations |

### Détail des entités principales

#### User (`dbo.AppReg_Users`)
| Colonne | Type | Description |
|---------|------|-------------|
| `UserId` | int (PK) | Identifiant unique |
| `UserName` | string | Nom d'utilisateur |
| `Matricule` | string (unique) | Matricule format `EMP-XXXX-Y` |
| `Email` | string (unique) | Adresse email |
| `PasswordHash` | string | Hash BCrypt du mot de passe |
| `IsActive` | bool | Compte actif |
| `HasBeenActivated` | bool | A déjà été activé (distingue "en attente" vs "désactivé") |
| `CreatedAt` | DateTime? | Date de création |
| `RefreshToken` | string? | Refresh token JWT actuel |
| `RefreshTokenExpiryTime` | DateTime? | Expiration du refresh token |
| `PasswordResetToken` | string? | Token de réinitialisation (valable 1h) |
| `PhotoUrl` | string? | URL de la photo de profil |

#### Client (`dbo.Client`)
| Colonne | Type | Description |
|---------|------|-------------|
| `IdClient` | int (PK) | Identifiant unique |
| `CodeClient` | string (unique) | Code client numérique (10 chiffres) |
| `Nom` | string | Nom de famille |
| `Prenom` | string | Prénom |
| `DateNaissance` | DateTime | Date de naissance |
| `Sexe` | string | Sexe (M/F) |
| `Nationalite` | string | Nationalité |
| `Telephone` | string | Numéro de téléphone |
| `Email` | string | Email du client |
| `Adresse` | string | Adresse postale |
| `Ville` | string | Ville |
| `DateCreation` | DateTime | Date de création dans le système |
| `Statut` | enum (string) | `Actif`, `EnAttente`, `Bloqué` |
| `EstArchive` | bool | Client archivé (soft delete) |
| `MotifBlocage` | string? | Raison du blocage |
| `TypeClient` | string | `Particulier`, `Entreprise`, `VIP` |
| `EstImporteSISS` | bool | Importé depuis le SISS |
| `DateDerniereSynchroSISS` | DateTime? | Date de la dernière synchro SISS |
| `HashReferentiel` | string? | Hash SHA-256 pour détection des changements SISS |

#### Reminder (`dbo.Reminders`)
| Colonne | Type | Valeurs possibles |
|---------|------|------------------|
| `Type` | string | `RenouvellementDocument`, `AppelSuivi`, `RendezVous`, `Tache`, `Autre` |
| `Priority` | string | `Basse`, `Normale`, `Haute`, `Critique` |
| `Status` | string | `EnAttente`, `EnCours`, `Termine`, `Annule` |

### Données initiales (seed)

**Rôles** (créés automatiquement) :
- `Admin` (Id=1)
- `Agent` (Id=2)
- `Superviseur` (Id=3)

**Templates de documents** (16 templates, créés automatiquement) :

*Particulier (5 docs)* : CIN, Justificatif de domicile, Fiche de paie, RIB bancaire, Photo d'identité  
*Entreprise (6 docs)* : RC, Statuts société, NIF, Bilan comptable, CIN gérant, RIB entreprise  
*VIP (5 docs)* : Passeport, Justificatif de patrimoine, Déclaration revenus, RIB VIP, Lettre de recommandation  

**Migrations EF Core** :
- `20260731192248_InitialCreate` — création du schéma complet
- `20260801131337_AddSissEntities` — ajout des entités SISS (Compte, Cours, Mouvement, SynchronisationLog)

---

## 5. API REST — Endpoints par contrôleur

> Base URL : `http://localhost:{port}/api`  
> Authentification : `Authorization: Bearer <JWT>` (sauf mention `[Public]`)

---

### 5.1 AuthController — `/api/auth`

| Méthode | Route | Auth | Description |
|---------|-------|------|-------------|
| POST | `/register` | Public | Inscription — crée un compte en attente d'activation |
| POST | `/login` | Public | Connexion — retourne `accessToken` + `refreshToken` |
| POST | `/refresh-token` | Public | Renouvellement du token JWT |
| GET | `/me` | JWT | Infos de l'utilisateur connecté |
| POST | `/change-password` | JWT | Changement du mot de passe |
| POST | `/logout` | JWT | Déconnexion — révoque le refresh token |
| GET | `/profile` | JWT | Profil complet |
| PUT | `/profile` | JWT | Mise à jour du profil (nom, email) |
| POST | `/profile/photo` | JWT | Upload de la photo de profil (jpg/png/gif/webp) |
| POST | `/forgot-password` | Public | Demande de réinitialisation par email |
| POST | `/reset-password` | Public | Réinitialisation avec le token reçu par email |

**Détails du login** : connexion par `Matricule` + `Password`. Retourne un Access Token (15 min) + Refresh Token (7 jours).

---

### 5.2 UsersController — `/api/users`

Rôles requis : `Admin` ou `Superviseur`

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/` | Liste tous les utilisateurs |
| GET | `/{id}` | Détail d'un utilisateur |
| GET | `/pending` | Utilisateurs en attente d'activation |
| POST | `/{id}/approve` | Approuver + assigner un rôle + envoyer email |
| PUT | `/{id}/activate` | Activer un utilisateur |
| PUT | `/{id}/deactivate` | Désactiver un utilisateur |
| POST | `/{userId}/roles/{roleId}` | Assigner un rôle |
| DELETE | `/{userId}/roles/{roleId}` | Retirer un rôle |
| PUT | `/{id}` | Modifier les infos d'un utilisateur |
| DELETE | `/{id}` | Supprimer un utilisateur |
| POST | `/{id}/send-email` | Envoyer un email direct à un utilisateur |

---

### 5.3 RolesController — `/api/roles`

Rôles requis : `Admin`

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/` | Liste tous les rôles |
| GET | `/{id}` | Détail d'un rôle |
| POST | `/` | Créer un rôle |
| PUT | `/{id}` | Modifier un rôle |
| PUT | `/{id}/activate` | Activer un rôle |
| PUT | `/{id}/deactivate` | Désactiver un rôle |

---

### 5.4 ClientsController — `/api/clients`

| Méthode | Route | Rôles | Description |
|---------|-------|-------|-------------|
| GET | `/` | Tous | Recherche paginée multi-critères |
| GET | `/archived` | Admin, Superviseur | Liste des clients archivés |
| GET | `/{id}` | Tous | Détail d'un client avec ses détails |
| POST | `/` | Admin, Agent, Superviseur | Créer un client |
| PUT | `/{id}` | Admin, Agent, Superviseur | Modifier un client |
| DELETE | `/{id}` | Admin | Archiver un client (soft delete) |
| POST | `/{id}/restore` | Admin | Restaurer un client archivé |
| POST | `/import` | Admin, Agent, Superviseur | Import en masse depuis un fichier Excel (.xlsx) |
| GET | `/export` | Tous | Export Excel filtré des clients |
| GET | `/stats` | Tous | Statistiques globales clients + documents |
| GET | `/villes` | Tous | Liste des villes distinctes |
| GET | `/import-template` | Admin, Agent, Superviseur | Télécharger le modèle Excel d'import |

**Paramètres de recherche** (`ClientSearchDto`) :
- `CodeClient`, `Nom`, `Prenom`, `Ville`, `Statut`, `DateDebut`, `DateFin`, `Page`, `PageSize`

---

### 5.5 DocumentsController — `/api/documents`

| Méthode | Route | Rôles | Description |
|---------|-------|-------|-------------|
| GET | `/` | Admin, Superviseur | Tous les documents |
| GET | `/client/{clientId}` | Admin, Superviseur, Agent | Documents d'un client |
| GET | `/client/{clientId}/paged` | Admin, Superviseur, Agent | Documents paginés d'un client |
| GET | `/{id}` | Tous | Détail d'un document |
| POST | `/` | Admin, Agent, Superviseur | Upload d'un document (multipart/form-data) |
| DELETE | `/{id}` | Admin | Supprimer un document |
| GET | `/{id}/download` | Tous | Télécharger un document |
| PUT | `/{id}/validation` | Admin, Superviseur | Changer le statut : `Validé`, `En attente`, `Rejeté` |

**Formats acceptés** : `.pdf`, `.jpg`, `.jpeg`, `.png`, `.doc`, `.docx`, `.xls`, `.xlsx`  
**Statut de validation** : Auto-assigné `Validé` si Admin, `En attente` sinon.

---

### 5.6 ClientStatusController — `/api/clients/{clientId}/clientstatus`

| Méthode | Route | Rôles | Description |
|---------|-------|-------|-------------|
| GET | `/report` | Tous | Rapport détaillé du statut (docs validés/en attente/rejetés) |
| GET | `/missing-documents` | Tous | Liste des documents non validés |
| POST | `/recalculate` | Admin, Superviseur | Recalculer le statut automatiquement |
| POST | `/block` | Admin, Superviseur | Bloquer manuellement un client |
| POST | `/unblock` | Admin, Superviseur | Débloquer un client |

---

### 5.7 DocumentTemplatesController — `/api/documenttemplates`

Rôles requis : `Admin` ou `Superviseur` (lecture), `Admin` uniquement (écriture)

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/` | Tous les templates |
| GET | `/types` | Types de clients distincts |
| GET | `/by-type/{typeClient}` | Templates d'un type de client |
| GET | `/{id}` | Détail d'un template |
| POST | `/` | Créer un template (Admin) |
| PUT | `/{id}` | Modifier un template (Admin) |
| DELETE | `/{id}` | Supprimer un template (Admin) |

---

### 5.8 ClientPortalController — `/api/clientportal`

| Méthode | Route | Auth | Description |
|---------|-------|------|-------------|
| POST | `/{clientId}/invite` | Admin/Agent/Superviseur | Générer + envoyer un lien portail par email |
| GET | `/{clientId}/token-info` | Admin/Agent/Superviseur | Infos sur le token actif |
| DELETE | `/{clientId}/revoke` | Admin/Superviseur | Révoquer l'accès portail |
| GET | `/completion` | Admin/Superviseur | Taux de complétion documentaire |
| GET | `/portal/{token}` | **Public** | Vue portail client (liste des documents requis) |
| POST | `/portal/{token}/upload` | **Public** | Upload d'un document depuis le portail (max 50 Mo) |
| GET | `/portal/{token}/validate` | **Public** | Vérifier la validité d'un token |

---

### 5.9 DashboardController — `/api/dashboard`

| Méthode | Route | Rôles | Description |
|---------|-------|-------|-------------|
| GET | `/stats` | Tous | Statistiques complètes du tableau de bord |
| GET | `/finance` | Tous | Agrégats financiers (Comptes, Mouvements, Cours) |
| GET | `/kpis` | Tous | KPIs principaux |
| GET | `/client-evolution` | Tous | Évolution clients sur N mois (1-24, défaut 6) |
| GET | `/geographic-distribution` | Tous | Répartition géographique (top N villes, défaut 10) |
| GET | `/top-revenue-clients` | Tous | Top N clients par revenu mensuel |
| GET | `/pending-documents` | Tous | Documents en attente de validation |
| GET | `/agent-performance` | Admin, Superviseur | Performance des agents |
| GET | `/alerts` | Tous | Alertes actives |
| POST | `/config` | Tous | Sauvegarder la configuration du dashboard |
| GET | `/config/{userId}` | Tous | Récupérer la configuration personnalisée |

---

### 5.10 HistoriqueController — `/api/historique`

Rôles requis : `Admin` ou `Superviseur`

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/` | Recherche dans le journal d'audit |
| GET | `/tables` | Liste des tables auditées |
| GET | `/actions` | Liste des types d'actions |

**Paramètres de recherche** (`HistoriqueSearchDto`) : table, action, date, utilisateur...

---

### 5.11 NotificationsController — `/api/notifications`

| Méthode | Route | Rôles | Description |
|---------|-------|-------|-------------|
| GET | `/` | Tous | Notifications de l'utilisateur connecté (paginé) |
| GET | `/non-lu/count` | Tous | Nombre de notifications non lues |
| POST | `/` | Admin, Superviseur | Créer une notification |
| PUT | `/{id}/lu` | Tous | Marquer une notification comme lue |
| PUT | `/marquer-tout-lu` | Tous | Marquer toutes les notifications comme lues |

---

### 5.12 RemindersController — `/api/reminders`

| Méthode | Route | Rôles | Description |
|---------|-------|-------|-------------|
| GET | `/` | Tous | Rappels de l'utilisateur connecté (paginé) |
| GET | `/filtered` | Tous | Rappels filtrés |
| GET | `/stats` | Tous | Statistiques des rappels |
| GET | `/{id}` | Tous | Détail d'un rappel |
| POST | `/` | Tous | Créer un rappel |
| PUT | `/{id}` | Tous | Modifier un rappel |
| PUT | `/{id}/complete` | Tous | Marquer comme terminé |
| PUT | `/{id}/sign` | Tous | Signer et terminer une tâche assignée |
| GET | `/assigned-to-me` | Tous | Rappels assignés à l'utilisateur connecté |
| PUT | `/{id}/status` | Tous | Changer le statut |
| POST | `/from-document` | Tous | Créer un rappel depuis un document client |
| GET | `/team` | Admin, Superviseur | Rappels de toute l'équipe |
| GET | `/team/stats` | Admin, Superviseur | Statistiques de l'équipe |
| DELETE | `/{id}` | Tous | Supprimer un rappel |

---

### 5.13 ComptesController — `/api/comptes`

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/` | Recherche paginée de comptes |
| GET | `/client/{idClient}` | Comptes d'un client |
| GET | `/client/{idClient}/totaux` | Totaux (solde consolidé) d'un client |
| GET | `/{id}` | Détail d'un compte |
| GET | `/{id}/mouvements` | Mouvements d'un compte (filtres date, sens, montant) |

**Paramètres de recherche** : `numCompte`, `agence`, `devise`, `codeClient`, `page`, `pageSize`

---

### 5.14 MouvementsController — `/api/mouvements`

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/client/{idClient}` | Mouvements d'un client (filtres date, sens, montant) |

**Paramètres** : `dateDebut`, `dateFin`, `sens` (D/C), `montantMin`, `montantMax`, `page`, `pageSize`

---

### 5.15 CoursController — `/api/cours`

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/temps-reel` | Cours du jour depuis l'API externe (avec cache 6h) |
| GET | `/` | Derniers cours SISS en base |
| GET | `/{devise}/historique` | Historique d'une devise |

**Devises supportées** : TND, EUR, USD, GBP, CHF, CAD, AUD, JPY, SAR, AED, KWD, CNY, MAD, EGP, SEK, NOK, DKK

---

### 5.16 ReleveController — `/api/releve`

| Méthode | Route | Description |
|---------|-------|-------------|
| POST | `/pdf` | Générer un relevé de compte en PDF |
| POST | `/excel` | Générer un relevé de compte en Excel |

**Corps** (`ReleveRequestDto`) : `CompteId`, période de dates

---

### 5.17 ReportsController — `/api/reports`

Rôles requis : `Admin` ou `Superviseur`

| Méthode | Route | Description |
|---------|-------|-------------|
| POST | `/clients/pdf` | Rapport PDF de la liste clients |
| POST | `/clients/excel` | Rapport Excel de la liste clients |
| GET | `/stats/pdf` | Rapport PDF des statistiques générales |

---

### 5.18 AdvancedSearchController — `/api/advancedsearch`

| Méthode | Route | Rôles | Description |
|---------|-------|-------|-------------|
| POST | `/search` | Tous | Recherche multi-critères avancée |
| GET | `/suggestions` | Tous | Suggestions en temps réel (min 2 caractères) |
| GET | `/history` | Tous | Historique des recherches de l'utilisateur |
| DELETE | `/history` | Tous | Effacer l'historique de recherche |
| POST | `/clients/{clientId}/tags` | Admin/Agent/Superviseur | Ajouter un tag à un client |
| DELETE | `/clients/tags/{tagId}` | Admin/Agent/Superviseur | Supprimer un tag d'un client |
| GET | `/clients/{clientId}/tags` | Tous | Tags d'un client |
| POST | `/documents/{documentId}/tags` | Admin/Agent/Superviseur | Ajouter un tag à un document |
| DELETE | `/documents/tags/{tagId}` | Admin/Agent/Superviseur | Supprimer un tag d'un document |
| GET | `/documents/{documentId}/tags` | Tous | Tags d'un document |
| GET | `/tags/popular` | Tous | Tags les plus populaires |

---

### 5.19 SearchController — `/api/search`

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/?q=&limit=` | Recherche globale rapide (clients, documents, users) |

---

### 5.20 SynchronisationController — `/api/synchronisation`

Rôles requis : `Admin` ou `Superviseur`

| Méthode | Route | Description |
|---------|-------|-------------|
| POST | `/declencher` | Déclencher manuellement une synchronisation SISS |
| GET | `/etat` | État de la synchronisation (en cours, dernière, rejets) |
| GET | `/historique` | Historique des synchronisations (paginé) |
| GET | `/rejets/{id}` | Lignes rejetées d'une synchronisation |

---

## 6. Services métier (BL)

Tous les services sont enregistrés en `Scoped` dans le conteneur DI.

| Interface | Implémentation | Responsabilité |
|-----------|---------------|----------------|
| `IAuthService` | `AuthService` | Inscription, connexion, JWT, refresh token, reset password, profil, photo |
| `IUserService` | `UserService` | CRUD utilisateurs, activation, assignation de rôles |
| `IRoleService` | `RoleService` | CRUD rôles |
| `IClientService` | `ClientService` | CRUD clients, import/export Excel, statistiques, archivage |
| `IDocumentService` | `DocumentService` | Upload/download/delete documents, validation, stockage physique |
| `IDocumentTemplateService` | `DocumentTemplateService` | CRUD templates de documents |
| `IHistoriqueService` | `HistoriqueService` | Journalisation des modifications (audit trail) |
| `INotificationService` | `NotificationService` | Création et lecture des notifications in-app |
| `IAdvancedSearchService` | `AdvancedSearchService` | Recherche avancée, suggestions, historique, tags |
| `IDashboardService` | `DashboardService` | Statistiques, KPIs, évolutions, alertes dashboard |
| `IClientPortalService` | `ClientPortalService` | Portail self-service client (token, upload, complétion) |
| `IReminderService` | `ReminderService` | CRUD rappels, filtres, stats, assignation équipe |
| `ICompteService` | `CompteService` | Consultation comptes SISS, totaux |
| `IMouvementService` | `MouvementService` | Consultation mouvements SISS |
| `ICoursService` | `CoursService` | Cours de change SISS en base |
| `ICoursTempsReelService` | `CoursTempsReelService` | Cours temps réel (API externe + repli SISS, cache 6h) |
| `ISynchronisationService` | `SynchronisationService` | Synchronisation application ↔ SISS (CSV) |
| `IReleveService` | `ReleveService` | Génération relevés de compte PDF/Excel |
| `IReportService` | `ReportService` | Génération rapports PDF/Excel clients et stats |
| `IEmailService` | `EmailService` | Envoi d'emails (MailKit/SMTP) |
| `ICurrentUserService` | `CurrentUserService` | Récupération de l'utilisateur courant depuis le JWT |
| `IPortalNotifier` | `PortalNotifier` | Notifications temps réel via SignalR Hub |
| `ClientStatusManager` | *(concret)* | Gestion automatique du statut client selon ses documents |

### AuthService — Détails techniques

- **Hashage** : BCrypt.Net (coût par défaut)
- **JWT Access Token** : durée de vie **15 minutes**, signé HMAC-SHA256
- **Refresh Token** : 64 bytes aléatoires, durée **7 jours**
- **Matricule** : format `EMP-XXXX-Y` (4 chiffres + 1 lettre), unique en base
- **Reset password** : token Base64 URL-safe, valable **1 heure**
- **Photo de profil** : stockée dans `wwwroot/uploads/profiles/profile_{userId}.{ext}`

### ClientService — Comportements importants

- À la **création** : génère un `CodeClient` numérique unique (9-10 chiffres)
- À la **modification** : trace chaque champ modifié dans l'historique
- Si le **statut change** : envoie une notification aux admins/superviseurs
- **Import Excel** : lecture positionnelle (colonnes 1-7), gestion des erreurs ligne par ligne
- **Export Excel** : 18 colonnes (données client + détails)

### SynchronisationService — Fonctionnement (Option A — conforme au cahier des charges)

**SSIS est l'unique point d'entrée ETL.** Le pipeline Visual Studio (drag-and-drop) est seul responsable du chargement des données en SQL Server. Le service C# se contente de déclencher ce pipeline et de tracer le résultat.

#### Rôle du package SSIS

Le package SSIS charge directement dans SQL Server à partir des fichiers CSV exportés par le système bancaire :
- `Client.csv` → `dbo.Client` + `dbo.DetailClient` (via Lookup / Conditional Split / gestion d'erreurs)
- `DocumentClient.csv` → `dbo.DocumentClient`
- `Compte.csv` → `dbo.Compte`
- `Cours.csv` → `dbo.Cours`
- `MVT.csv` → `dbo.MVT`

#### Rôle du SynchronisationService C#

1. Vérifie qu'aucune synchronisation n'est déjà en cours
2. Crée une entrée `SynchronisationLog` (statut `En cours`)
3. Lance `dtexec.exe /F "<PackagePath>"` et attend la fin du processus
4. Met à jour le log selon le code de retour dtexec (0 = `Reussie`, autre = `Echec`)
5. Enregistre dans l'historique applicatif
6. Notifie les admins via notifications in-app + SignalR

#### Codes de retour dtexec

| Exit code | Signification |
|-----------|--------------|
| `0` | Succès — toutes les tâches SSIS se sont exécutées |
| `1` | Succès partiel |
| `3` | Interrompu |
| `4` | Impossible de se connecter |
| `5` | Package introuvable |
| `-1073741819` | Crash / timeout |

#### SissValidationRules

Ce fichier C# est conservé comme **référence documentaire** des règles métier appliquées dans le package SSIS (format CodeClient 10 chiffres, majorité 18 ans, sexe H/F, types de documents valides). Ces règles ne sont plus exécutées côté C# — elles doivent être répliquées dans les composants SSIS (Derived Column, Conditional Split, Script Component).

---

## 7. Repositories (DAL)

Tous les repositories sont enregistrés en `Scoped`. Ils implémentent le pattern Repository avec `AppDbContext`.

| Interface | Implémentation | Tables principales |
|-----------|---------------|--------------------|
| `IUserRepository` | `UserRepository` | `AppReg_Users` |
| `IRoleRepository` | `RoleRepository` | Roles, UserRoles |
| `IClientRepository` | `ClientRepository` | `Client`, `DetailClient` |
| `IDocumentRepository` | `DocumentRepository` | `DocumentClient` |
| `IDocumentTemplateRepository` | `DocumentTemplateRepository` | `DocumentTemplates` |
| `IHistoriqueRepository` | `HistoriqueRepository` | `HistoriqueModifications` |
| `INotificationRepository` | `NotificationRepository` | `Notifications` |
| `IClientPortalRepository` | `ClientPortalRepository` | `ClientPortalTokens` |
| `IReminderRepository` | `ReminderRepository` | `Reminders` |
| `ICompteRepository` | `CompteRepository` | `Compte` |
| `ICoursRepository` | `CoursRepository` | `Cours` |
| `IMouvementRepository` | `MouvementRepository` | `MVT` |
| `ISynchronisationRepository` | `SynchronisationRepository` | `SynchronisationLog`, `SynchronisationRejet` |

---

## 8. DTOs (Data Transfer Objects)

### Dossier Auth

| DTO | Usage |
|-----|-------|
| `RegisterDto` | Inscription : `Username`, `Email`, `Password` |
| `LoginDto` | Connexion : `Matricule`, `Password` |
| `AuthResponseDto` | Réponse login : `AccessToken`, `RefreshToken`, `User` |
| `RefreshTokenDto` | Renouvellement : `AccessToken`, `RefreshToken` |
| `UserDto` | Lecture utilisateur : `Id`, `Matricule`, `Username`, `Email`, `IsActive`, `Roles[]`, `PhotoUrl` |
| `ChangePasswordDto` | `OldPassword`, `NewPassword` |
| `ForgotPasswordDto` | `Email` |
| `ResetPasswordDto` | `Email`, `Token`, `NewPassword` |
| `UpdateProfileDto` | `Username`, `Email` |
| `UpdateUserDto` | Modification admin d'un utilisateur |
| `ApproveUserDto` | `RoleId` |
| `SendEmailDto` | `Subject`, `Body` |
| `RoleDto` | `Id`, `Name`, `IsActive` |
| `CreateRoleDto` | `Name` |
| `UpdateRoleDto` | `Name` |

### Dossier Client

| DTO | Usage |
|-----|-------|
| `ClientDto` | Lecture client complet avec `DetailClientDto` |
| `CreateClientDto` | Création avec validation (`[Required]`, `[EmailAddress]`, etc.) |
| `UpdateClientDto` | Modification |
| `ClientSearchDto` | Filtres de recherche paginée |
| `PagedResult<T>` | Résultat paginé générique : `Items`, `Total`, `Page`, `PageSize`, `TotalPages` |
| `ClientStatsDto` | Statistiques : totaux, répartitions, tops, recents |
| `DocumentDto` | Lecture document : Id, type, nom fichier, statut, client |

### Dossier Finance

| DTO | Usage |
|-----|-------|
| `CompteDto` | Compte bancaire SISS |
| `MouvementDto` | Mouvement bancaire |
| `CoursDto` | Cours de change SISS |
| `CoursTempsReelDto` | Cours temps réel : `Devise`, `Nom`, `Symbole`, `Valeur`, `Date` |

### Dossier Portal

| DTO | Usage |
|-----|-------|
| `DocumentTemplateDto` | Template de document |
| `ClientPortalViewDto` | Vue complète du portail (liste docs requis + statuts) |
| `SendPortalInviteDto` | `DureeJours` |

### Dossier Reminder

| DTO | Usage |
|-----|-------|
| `ReminderDto` | Rappel complet |
| `CreateReminderDto` | Création |
| `UpdateReminderDto` | Modification |
| `ReminderFilterDto` | Filtres |
| `UpdateReminderStatusDto` | `Status` |
| `CreateDocumentReminderDto` | `ClientId`, `Title` |

### Dossier Synchronisation

| DTO | Usage |
|-----|-------|
| `SynchronisationResultDto` | Résultat : insert, update, rejets, statut, durée |
| `SynchronisationStateDto` | État courant : en cours, dernière synchro, rejets non résolus |
| `SynchronisationRejetDto` | Détail d'une ligne rejetée : fichier, ligne, motif, contenu brut |

---

## 9. Middlewares

### ExceptionHandlingMiddleware

Intercepte toutes les exceptions non gérées et retourne une réponse JSON structurée.

| Exception | Code HTTP |
|-----------|-----------|
| `AccountDisabledException` | 403 Forbidden |
| `UnauthorizedAccessException` | 401 Unauthorized |
| `KeyNotFoundException` | 404 Not Found |
| `InvalidOperationException` | 400 Bad Request |
| `ArgumentException` | 400 Bad Request |
| Autres | 500 Internal Server Error |

**Format de réponse** :
```json
{
  "statusCode": 404,
  "message": "Client introuvable."
}
```

### RateLimitingMiddleware

Limite le débit des requêtes par IP, par endpoint.

| Type d'endpoint | Limite | Fenêtre |
|-----------------|--------|---------|
| Auth (`/api/auth/login`, `/api/auth/register`) | **10 req** | 60 secondes |
| Sensibles (`/api/clients`, `/api/documents`, `/api/reports`, `/api/search`, etc.) | **60 req** | 60 secondes |

**Headers de réponse** : `X-RateLimit-Limit`, `X-RateLimit-Remaining`  
**En cas de dépassement** : HTTP 429 + header `RetryAfter`

---

## 10. SignalR — Notifications temps réel

### NotificationHub — `/hubs/notifications`

- **Authentification** : JWT requis (`[Authorize]`)
- **Groupes** : chaque utilisateur rejoint automatiquement le groupe `user_{id}` à la connexion
- **Événement client** : `ReceiveNotification` (payload : titre, message, type, lien, dateCreation)
- **Méthode diagnostique** : `Ping()` → répond `Pong` avec l'heure UTC

### PortalNotifier

Implémentation de `IPortalNotifier` qui utilise le Hub SignalR pour envoyer des notifications :
- `NotifyUserAsync(userId, titre, message, type, lien)` → groupe `user_{userId}`
- `NotifyGroupAsync(groupName, ...)` → groupe nommé

**Utilisation** : les services BL injectent `IPortalNotifier` pour notifier les utilisateurs en temps réel lors d'événements critiques (upload portail, fin de synchronisation, etc.)

---

## 11. Services hébergés (Background Services)

### ReminderOverdueService

- **Fréquence** : toutes les **5 minutes**
- **Rôle** : vérifie les rappels dont la `DueDate` est dépassée et qui n'ont pas encore été notifiés (`NotifiedAt == null`)
- **Actions** :
  1. Crée une notification in-app pour l'utilisateur assigné (ou créateur)
  2. Envoie un email HTML formaté avec les détails du rappel
  3. Marque le rappel `NotifiedAt = UtcNow`
- **Priorités** dans l'email : Critique (rouge), Haute (orange), autres (jaune)

### SissSynchronisationHostedService

- **Fréquence de vérification** : toutes les **30 secondes**
- **Déclenchement** : si l'heure courante est dans une fenêtre de ±5 min par rapport à `Siss:PlanningHeure` (défaut 02:30)
- **Rôle** : appelle `SynchronisationService.DeclencherAsync("Planifie", null)` qui lui-même lance `dtexec.exe`. Ce service **ne lit aucun CSV** et **ne manipule aucune entité** directement.
- **Garde-fous** :
  - Ne se déclenche pas si une synchro est déjà en cours
  - Ne se déclenche pas si une synchro planifiée date de moins de 6 heures
  - Attend 6 heures après chaque déclenchement réussi
- **Journalisation** : chaque exécution est tracée dans `SynchronisationLog`

---

## 12. Authentification et autorisation

### Flux d'authentification

```
1. POST /api/auth/register
   → Compte créé (inactif) + notification aux admins

2. Admin : POST /api/users/{id}/approve  (roleId)
   → Compte activé + email envoyé à l'utilisateur

3. POST /api/auth/login  (matricule + mot de passe)
   → { accessToken (15 min), refreshToken (7 jours) }

4. POST /api/auth/refresh-token
   → Nouveau pair de tokens

5. POST /api/auth/logout
   → Révocation du refresh token
```

### Claims du JWT

| Claim | Valeur |
|-------|--------|
| `ClaimTypes.NameIdentifier` | `User.Id` |
| `ClaimTypes.Email` | `User.Email` |
| `ClaimTypes.Name` | `User.Username` |
| `ClaimTypes.Role` | Un claim par rôle (ex: `Admin`) |

### Rôles et permissions résumées

| Rôle | Droits |
|------|--------|
| `Admin` | Accès complet à toutes les fonctionnalités |
| `Superviseur` | Gestion clients, documents, rapports, dashboard, historique, synchronisation |
| `Agent` | Création/modification clients et documents, rappels, portail |
| *(tout utilisateur authentifié)* | Lecture clients, notifications, recherche, cours, comptes, rappels personnels |

### Portail client (sans compte)

Le portail client utilise un système de **token sécurisé** (GUID Base64URL) indépendant du JWT :
- Généré par l'admin/agent et envoyé par email au client
- Durée configurable (en jours)
- Traçabilité : compteur d'accès, dernière IP, date de dernier accès
- Révocable à tout moment
- Endpoints `/portal/{token}/*` accessibles sans authentification JWT

---

## 13. Packages NuGet utilisés

### Backend.API

| Package | Version | Usage |
|---------|---------|-------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.10 | Authentification JWT |
| `Swashbuckle.AspNetCore` | 6.6.2 | Swagger UI |
| `DotNetEnv` | 3.2.0 | Chargement du fichier `.env` |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.10 | Outils migrations EF (dev) |

### Backend.DAL

| Package | Version | Usage |
|---------|---------|-------|
| `Microsoft.EntityFrameworkCore` | 8.0.10 | ORM |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.10 | Provider SQL Server |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.10 | CLI migrations |
| `BCrypt.Net-Next` | 4.0.3 | Hashage des mots de passe |

### Backend.BL

| Package | Version | Usage |
|---------|---------|-------|
| `AutoMapper` | 12.0.1 | Mapping Entités ↔ DTOs |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | 12.0.1 | Intégration DI |
| `BCrypt.Net-Next` | 4.0.3 | Hashage mots de passe |
| `MailKit` | 4.8.0 | Envoi d'emails SMTP |
| `EPPlus` | 7.5.1 | Lecture/écriture fichiers Excel |
| `QuestPDF` | 2024.12.0 | Génération de PDFs |
| `PdfPig` | 0.1.15 | Lecture de PDFs |
| `Tesseract` | 5.2.0 | OCR (reconnaissance de texte, français + arabe) |
| `System.IdentityModel.Tokens.Jwt` | 8.0.2 | Génération/validation JWT |
| `Microsoft.AspNetCore.SignalR.Core` | 1.1.0 | Notifications temps réel |
| `Microsoft.AspNetCore.Http.Abstractions` | 2.2.0 | Accès HttpContext |

---

## 14. Fonctionnalités détaillées

### 14.1 Gestion des utilisateurs

- **Inscription** : auto-enregistrement avec matricule généré automatiquement (`EMP-XXXX-Y`)
- **Activation** : workflow d'approbation par l'Admin/Superviseur. Les admins reçoivent une notification in-app à chaque inscription
- **Rôles** : 3 rôles prédéfinis (Admin, Agent, Superviseur), extensibles
- **Profil** : modification du nom/email + upload de photo (jpg/png/gif/webp/bmp)
- **Sécurité** : mots de passe hashés BCrypt, tokens JWT à courte durée de vie, refresh tokens révocables

### 14.2 Gestion des clients

- **CRUD complet** avec enregistrement de l'historique de chaque modification
- **Types** : Particulier, Entreprise, VIP (détermine les documents requis)
- **Statuts** : `Actif`, `EnAttente`, `Bloqué` (avec motif de blocage)
- **Archivage** (soft delete) : le client reste en base mais n'apparaît pas dans la liste principale
- **Import Excel** : jusqu'à N clients depuis un fichier `.xlsx` (modèle téléchargeable)
- **Export Excel** : 18 colonnes, filtré selon les critères de recherche
- **Détails complémentaires** : profession, employeur, secteur d'activité, revenu mensuel, situation familiale, type de résidence (relation 1:1)
- **Intégration SISS** : marquage `EstImporteSISS`, hash SHA-256 pour détection des changements, date de dernière synchronisation

### 14.3 Gestion des statuts clients (ClientStatusManager)

Calcul **automatique** du statut basé sur les documents :
- **Aucun document** → `EnAttente`
- **Tous les documents validés** → `Actif`
- **Au moins un document non validé** → `EnAttente`
- **Bloqué manuellement** → ne change pas automatiquement, nécessite un déblocage explicite

### 14.4 Gestion documentaire

- **Upload** : stockage physique dans `App_Data/Documents/{idClient}/`
- **Types supportés** : PDF, JPG, JPEG, PNG, DOC, DOCX, XLS, XLSX
- **Statut de validation** : `En attente` → `Validé` ou `Rejeté`
- **Origine** : `Application` (upload manuel) ou `SISS` (importé par synchronisation)
- **Auto-validation** : si l'uploader est Admin, le document est directement `Validé`
- **OCR intégré** : Tesseract (langues: français + arabe) pour la lecture des documents

### 14.5 Templates de documents

Définissent les documents obligatoires et optionnels par type de client :
- **Particulier** : 5 documents (4 obligatoires + 1 optionnel)
- **Entreprise** : 6 documents (tous obligatoires)
- **VIP** : 5 documents (4 obligatoires + 1 optionnel)

Chaque template spécifie : nom, description, obligatoire/optionnel, ordre, formats acceptés, taille max en Mo.

### 14.6 Portail client self-service

Permet à un client d'uploader ses documents **sans compte utilisateur** :
1. L'agent génère un lien sécurisé (token GUID) envoyé par email au client
2. Le client accède au portail via `GET /api/clientportal/portal/{token}`
3. Le portail affiche la liste des documents requis selon le type du client
4. Le client uploade ses documents via `POST /api/clientportal/portal/{token}/upload`
5. Les uploads sont tracés (IP, date, compteur d'accès)
6. Une notification SignalR est envoyée en temps réel aux agents/admins

### 14.7 Tableau de bord analytique

KPIs et visualisations disponibles :
- Totaux clients (actifs, en attente, bloqués, nouveaux du mois)
- Répartition par statut, sexe, ville
- Évolution mensuelle des créations (jusqu'à 24 mois)
- Répartition géographique (top N villes)
- Top N clients par revenu mensuel
- Documents en attente de validation
- Agrégats financiers (soldes SISS, cours du jour)
- Performance des agents (Admin/Superviseur)
- Alertes actives
- Configuration personnalisable par utilisateur

### 14.8 Recherche avancée

- **Recherche multi-critères** (`POST /api/advancedsearch/search`) : combine plusieurs filtres
- **Suggestions temps réel** : auto-complétion à partir de 2 caractères
- **Historique** : sauvegarde des dernières recherches par utilisateur (effaçable)
- **Tags clients** : étiquetage libre pour classification et filtrage
- **Tags documents** : étiquetage des documents
- **Tags populaires** : top des tags les plus utilisés
- **Recherche globale rapide** (`GET /api/search?q=`) : résultats cross-entités

### 14.9 Rappels et agenda

Types : `RenouvellementDocument`, `AppelSuivi`, `RendezVous`, `Tache`, `Autre`  
Priorités : `Basse`, `Normale`, `Haute`, `Critique`  
Statuts : `EnAttente`, `EnCours`, `Termine`, `Annule`

Fonctionnalités :
- Création de rappels liés à un client et/ou assignés à un autre utilisateur
- Signature de tâches (workflow validation)
- Vue "équipe" pour Admin/Superviseur
- Statistiques d'équipe
- Rappels en retard : notification automatique in-app + email toutes les 5 minutes
- Création rapide depuis un document client

### 14.10 Finance SISS (lecture seule)

Données importées depuis le système bancaire SISS :
- **Comptes** : consultation, recherche, totaux par client (solde + contre-valeur)
- **Mouvements** : filtrage par date, sens (Débit/Crédit), montant
- **Cours de change** : 
  - Source primaire : API externe `open.er-api.com` (cache 6 heures)
  - Source de repli : derniers cours SISS en base
  - 12 devises affichées : EUR, USD, GBP, CHF, CAD, AUD, JPY, SAR, AED, KWD, CNY, MAD

### 14.11 Relevés de compte

- Génération PDF (QuestPDF) et Excel (EPPlus) des relevés de compte
- Paramètres : compte, période
- Nom de fichier horodaté

### 14.12 Rapports

- Liste clients en PDF et Excel (avec filtres)
- Rapport de statistiques générales en PDF
- Réservé aux Admin/Superviseur

### 14.13 Journal d'audit (Historique)

Chaque modification importante est tracée automatiquement :
- Table modifiée, ID de l'entité, action (Création/Modification/Archivage/etc.)
- Ancienne et nouvelle valeur (texte libre)
- Utilisateur (id + nom), date et heure

### 14.14 Notifications in-app

- Générées automatiquement par les services BL lors d'événements :
  - Nouvelle inscription
  - Changement de statut client
  - Fin de synchronisation SISS
  - Rappels en retard
  - Uploads portail
- Envoyées en temps réel via SignalR
- Marquage lu/non-lu, marquage global "tout lire"
- Badge de comptage

### 14.15 Emails

Envoyés via MailKit (SMTP) dans les cas suivants :
- Approbation d'un utilisateur (identifiants)
- Demande de réinitialisation de mot de passe (lien sécurisé 1h)
- Invitation portail client (lien sécurisé)
- Rappels en retard (HTML formaté)

Template d'email HTML : design professionnel avec gradient bleu/violet (branding Lumina Finance).

### 14.16 Synchronisation SISS

**Architecture Option A — SSIS est l'unique point d'entrée ETL (conforme au cahier des charges).**

#### Pipeline SSIS (Visual Studio — drag-and-drop)

Le package `.dtsx` construit avec SSIS Designer est l'unique responsable du chargement des données en SQL Server. Il traite 6 fichiers CSV exportés par le système bancaire :

| Fichier source | Table SQL cible | Composants SSIS |
|----------------|-----------------|-----------------|
| `Client.csv` | `dbo.Client` | Flat File Source → Lookup (CodeClient) → Conditional Split → OLE DB Destination / Destination erreurs |
| `DetailClient.csv` | `dbo.DetailClient` | Flat File Source → Lookup → Conditional Split → OLE DB Destination |
| `DocumentClient.csv` | `dbo.DocumentClient` | Flat File Source → Lookup Client → Conditional Split → OLE DB Destination |
| `Compte.csv` | `dbo.Compte` | Flat File Source → OLE DB Destination |
| `Cours.csv` | `dbo.Cours` | Flat File Source → OLE DB Destination |
| `MVT.csv` | `dbo.MVT` | Flat File Source → OLE DB Destination |

Règles de validation appliquées dans SSIS (Conditional Split / Script Component) :
- `CodeClient` : 10 chiffres exactement
- `DateNaissance` : format `yyyy-MM-dd`, client ≥ 18 ans
- `Sexe` : `H` ou `F`
- `TypeDocument` : valeur parmi la liste autorisée

Les lignes invalides sont redirigées vers des fichiers `rejets_*.csv` (sortie d'erreur SSIS).

#### Rôle du SynchronisationService C#

Le service C# **ne lit aucun CSV** et **ne charge aucune donnée**. Son rôle se limite à :
1. Vérifier qu'aucune synchronisation n'est déjà en cours (table `SynchronisationLog`)
2. Créer une entrée de log (statut `En cours`)
3. Lancer `dtexec.exe /F "<Siss:PackagePath>"` et attendre sa fin (timeout : `Siss:TimeoutSeconds`)
4. Mettre à jour le log selon le code de retour dtexec (0 = `Reussie`, autre = `Echec`)
5. Tracer dans l'historique applicatif
6. Notifier les admins (in-app + SignalR)

#### Déclenchement

- **Manuel** : `POST /api/synchronisation/declencher` (Admin/Superviseur) → réponse synchrone (attend la fin de dtexec)
- **Automatique** : `SissSynchronisationHostedService` déclenche chaque jour à `Siss:PlanningHeure` (02:30 par défaut)

#### Configuration (appsettings.json)

```json
"Siss": {
  "PlanningHeure": "02:30",
  "DtexecPath": "C:\\Program Files\\Microsoft SQL Server\\160\\DTS\\Binn\\dtexec.exe",
  "PackagePath": "C:\\SISS\\Packages\\BT_DossierSISS.dtsx",
  "TimeoutSeconds": 1800
}
```

---

---

## Résumé des technologies et dépendances

```
ASP.NET Core 8.0 (Web API)
  ├── Entity Framework Core 8.0 (SQL Server)
  ├── JWT Bearer Authentication
  ├── SignalR (temps réel)
  ├── AutoMapper 12
  ├── BCrypt.Net-Next 4 (hashage)
  ├── MailKit 4.8 (emails SMTP)
  ├── EPPlus 7.5 (Excel)
  ├── QuestPDF 2024.12 (PDF)
  ├── PdfPig 0.1.15 (lecture PDF)
  ├── Tesseract 5.2 (OCR fr+ar)
  ├── DotNetEnv 3.2 (variables d'env)
  └── Swashbuckle 6.6 (Swagger)
```

## Sécurité

- Mots de passe hashés **BCrypt**
- Tokens JWT signés **HMAC-SHA256**, durée courte (15 min) + refresh token (7 jours)
- **Rate limiting** : 10 req/min sur l'auth, 60 req/min sur les endpoints sensibles
- **CORS** : origines explicitement listées (Angular sur localhost:4200 en dev)
- **Middleware global d'exceptions** : aucune stacktrace exposée en production
- Portail client : tokens opaques tracés avec IP et compteur d'accès
- Autorisation basée sur les rôles (`[Authorize(Roles = "...")]`) sur chaque endpoint sensible

---

*Documentation générée automatiquement par Kiro — Lumina Finance BT_DossierBackend*
