/* =============================================================
   MarketLink - Database Schema Script
   Theme: eGreen Basket
   Target: Microsoft SQL Server (tested on LocalDB / MSSQLLocalDB)

   This script contains the database and table definitions for
   MarketLink. It was generated from the Entity Framework Core
   model so it exactly matches the application's schema.

   NOTE: The running application creates and migrates this
   database automatically on first launch (see README). Run this
   script only if you want to create the schema manually in SSMS
   or seed a fresh server without the app.
   ============================================================= */

IF DB_ID(N'MarketLinkDb') IS NULL
    CREATE DATABASE [MarketLinkDb];
GO
USE [MarketLinkDb];
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Announcements] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(150) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Announcements] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(60) NOT NULL,
    [Description] nvarchar(250) NULL,
    [IconClass] nvarchar(60) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE TABLE [Markets] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(120) NOT NULL,
    [Address] nvarchar(250) NOT NULL,
    [Latitude] float NOT NULL,
    [Longitude] float NOT NULL,
    [MapProvider] nvarchar(30) NOT NULL,
    [OperatingDays] nvarchar(120) NOT NULL,
    [OpenTime] nvarchar(20) NOT NULL,
    [CloseTime] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Markets] PRIMARY KEY ([Id])
);

CREATE TABLE [PlatformReports] (
    [Id] int NOT NULL IDENTITY,
    [GeneratedBy] nvarchar(max) NULL,
    [ReportType] nvarchar(60) NOT NULL,
    [Parameters] nvarchar(300) NULL,
    [GeneratedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PlatformReports] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [FarmerProfiles] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [StallName] nvarchar(120) NOT NULL,
    [ContactPerson] nvarchar(100) NOT NULL,
    [ContactNumber] nvarchar(20) NOT NULL,
    [Bio] nvarchar(600) NULL,
    [AddressText] nvarchar(250) NULL,
    [Latitude] float NOT NULL,
    [Longitude] float NOT NULL,
    [MapProvider] nvarchar(30) NOT NULL,
    [ProfileImageUrl] nvarchar(250) NULL,
    [IsApproved] bit NOT NULL,
    [IsSuspended] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FarmerProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FarmerProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [Title] nvarchar(120) NOT NULL,
    [Message] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsRead] bit NOT NULL,
    [LinkUrl] nvarchar(250) NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [FarmerMarkets] (
    [Id] int NOT NULL IDENTITY,
    [FarmerProfileId] int NOT NULL,
    [MarketId] int NOT NULL,
    [OperatingDay] nvarchar(20) NOT NULL,
    [PickupStartTime] nvarchar(20) NOT NULL,
    [PickupEndTime] nvarchar(20) NOT NULL,
    [StallNumber] nvarchar(40) NULL,
    CONSTRAINT [PK_FarmerMarkets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FarmerMarkets_FarmerProfiles_FarmerProfileId] FOREIGN KEY ([FarmerProfileId]) REFERENCES [FarmerProfiles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FarmerMarkets_Markets_MarketId] FOREIGN KEY ([MarketId]) REFERENCES [Markets] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] nvarchar(450) NOT NULL,
    [FarmerProfileId] int NOT NULL,
    [MarketId] int NULL,
    [Status] nvarchar(20) NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [PickupDate] datetime2 NOT NULL,
    [PickupTimeSlot] nvarchar(40) NOT NULL,
    [TotalAmount] decimal(10,2) NOT NULL,
    [CutoffTime] datetime2 NOT NULL,
    [CustomerNote] nvarchar(300) NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Orders_FarmerProfiles_FarmerProfileId] FOREIGN KEY ([FarmerProfileId]) REFERENCES [FarmerProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_Markets_MarketId] FOREIGN KEY ([MarketId]) REFERENCES [Markets] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [FarmerProfileId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(600) NULL,
    [Price] decimal(10,2) NOT NULL,
    [Unit] nvarchar(30) NOT NULL,
    [StockQuantity] int NOT NULL,
    [ImageUrl] nvarchar(250) NULL,
    [IsAvailable] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Products_FarmerProfiles_FarmerProfileId] FOREIGN KEY ([FarmerProfileId]) REFERENCES [FarmerProfiles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Favorites] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] nvarchar(450) NOT NULL,
    [FarmerProfileId] int NULL,
    [ProductId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Favorites] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Favorites_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Favorites_FarmerProfiles_FarmerProfileId] FOREIGN KEY ([FarmerProfileId]) REFERENCES [FarmerProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Favorites_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrderItems] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(10,2) NOT NULL,
    [LineTotal] decimal(10,2) NOT NULL,
    [IsReviewed] bit NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Reviews] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] nvarchar(450) NOT NULL,
    [ProductId] int NULL,
    [FarmerProfileId] int NULL,
    [OrderId] int NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(600) NULL,
    [ReviewDate] datetime2 NOT NULL,
    [FarmerReply] nvarchar(600) NULL,
    [FarmerReplyDate] datetime2 NULL,
    [IsHidden] bit NOT NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reviews_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Reviews_FarmerProfiles_FarmerProfileId] FOREIGN KEY ([FarmerProfileId]) REFERENCES [FarmerProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_FarmerMarkets_FarmerProfileId] ON [FarmerMarkets] ([FarmerProfileId]);

CREATE INDEX [IX_FarmerMarkets_MarketId] ON [FarmerMarkets] ([MarketId]);

CREATE UNIQUE INDEX [IX_FarmerProfiles_UserId] ON [FarmerProfiles] ([UserId]);

CREATE INDEX [IX_Favorites_CustomerId_FarmerProfileId_ProductId] ON [Favorites] ([CustomerId], [FarmerProfileId], [ProductId]);

CREATE INDEX [IX_Favorites_FarmerProfileId] ON [Favorites] ([FarmerProfileId]);

CREATE INDEX [IX_Favorites_ProductId] ON [Favorites] ([ProductId]);

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);

CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);

CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);

CREATE INDEX [IX_Orders_FarmerProfileId] ON [Orders] ([FarmerProfileId]);

CREATE INDEX [IX_Orders_MarketId] ON [Orders] ([MarketId]);

CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);

CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);

CREATE INDEX [IX_Products_FarmerProfileId] ON [Products] ([FarmerProfileId]);

CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);

CREATE INDEX [IX_Reviews_CustomerId] ON [Reviews] ([CustomerId]);

CREATE INDEX [IX_Reviews_FarmerProfileId] ON [Reviews] ([FarmerProfileId]);

CREATE INDEX [IX_Reviews_ProductId] ON [Reviews] ([ProductId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260923210833_InitialCreate', N'9.0.0');

COMMIT;
GO

