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
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260120004522_Inicial')
BEGIN
    CREATE TABLE [Productos] (
        [Id] int NOT NULL IDENTITY,
        [CodigoBarra] nvarchar(max) NOT NULL,
        [Nombre] nvarchar(max) NOT NULL,
        [Precio] decimal(18,2) NOT NULL,
        [Stock] int NOT NULL,
        CONSTRAINT [PK_Productos] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260120004522_Inicial')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260120004522_Inicial', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260121001945_columnaActivo')
BEGIN
    ALTER TABLE [Productos] ADD [Activo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260121001945_columnaActivo')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260121001945_columnaActivo', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260122030125_ValidacionesProducto')
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Productos]') AND [c].[name] = N'Nombre');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Productos] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Productos] ALTER COLUMN [Nombre] nvarchar(100) NOT NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260122030125_ValidacionesProducto')
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Productos]') AND [c].[name] = N'CodigoBarra');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Productos] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Productos] ALTER COLUMN [CodigoBarra] nvarchar(50) NOT NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260122030125_ValidacionesProducto')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260122030125_ValidacionesProducto', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260124135729_CrearMovimientosStock')
BEGIN
    CREATE TABLE [MovimientosStock] (
        [Id] int NOT NULL IDENTITY,
        [ProductoId] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [Cantidad] int NOT NULL,
        [Tipo] nvarchar(20) NOT NULL,
        [Observacion] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_MovimientosStock] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MovimientosStock_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260124135729_CrearMovimientosStock')
BEGIN
    CREATE INDEX [IX_MovimientosStock_ProductoId] ON [MovimientosStock] ([ProductoId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260124135729_CrearMovimientosStock')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260124135729_CrearMovimientosStock', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260126004433_AjusteValidacionMovimientoStock')
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MovimientosStock]') AND [c].[name] = N'Observacion');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [MovimientosStock] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [MovimientosStock] ALTER COLUMN [Observacion] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260126004433_AjusteValidacionMovimientoStock')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260126004433_AjusteValidacionMovimientoStock', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260128025611_Ventas')
BEGIN
    CREATE TABLE [Ventas] (
        [Id] int NOT NULL IDENTITY,
        [Fecha] datetime2 NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Ventas] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260128025611_Ventas')
BEGIN
    CREATE TABLE [VentaDetalles] (
        [Id] int NOT NULL IDENTITY,
        [VentaId] int NOT NULL,
        [ProductoId] int NOT NULL,
        [Cantidad] int NOT NULL,
        [PrecioUnitario] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_VentaDetalles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VentaDetalles_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_VentaDetalles_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260128025611_Ventas')
BEGIN
    CREATE INDEX [IX_VentaDetalles_ProductoId] ON [VentaDetalles] ([ProductoId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260128025611_Ventas')
BEGIN
    CREATE INDEX [IX_VentaDetalles_VentaId] ON [VentaDetalles] ([VentaId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260128025611_Ventas')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128025611_Ventas', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260128035912_ObservacionNullable')
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MovimientosStock]') AND [c].[name] = N'Observacion');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [MovimientosStock] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [MovimientosStock] ALTER COLUMN [Observacion] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260128035912_ObservacionNullable')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128035912_ObservacionNullable', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260128234625_NoRequeridoListaVentaDetalle')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128234625_NoRequeridoListaVentaDetalle', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260129022050_AlterMovimientosProducto')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260129022050_AlterMovimientosProducto', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
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
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260201153755_AddIdentity')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260201153755_AddIdentity', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260203222721_Productos_Peso_PrecioEditable')
BEGIN
    ALTER TABLE [Productos] ADD [EsPorPeso] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260203222721_Productos_Peso_PrecioEditable')
BEGIN
    ALTER TABLE [Productos] ADD [PrecioEditable] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260203222721_Productos_Peso_PrecioEditable')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260203222721_Productos_Peso_PrecioEditable', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260203231453_MovimientoStock_Producto_CantidadDecimal')
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[VentaDetalles]') AND [c].[name] = N'Cantidad');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [VentaDetalles] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [VentaDetalles] ALTER COLUMN [Cantidad] decimal(18,3) NOT NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260203231453_MovimientoStock_Producto_CantidadDecimal')
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Productos]') AND [c].[name] = N'Stock');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Productos] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Productos] ALTER COLUMN [Stock] decimal(18,2) NOT NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260203231453_MovimientoStock_Producto_CantidadDecimal')
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MovimientosStock]') AND [c].[name] = N'Cantidad');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [MovimientosStock] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [MovimientosStock] ALTER COLUMN [Cantidad] decimal(18,3) NOT NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260203231453_MovimientoStock_Producto_CantidadDecimal')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260203231453_MovimientoStock_Producto_CantidadDecimal', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260211000810_Venta_Activa')
BEGIN
    ALTER TABLE [Ventas] ADD [Activa] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260211000810_Venta_Activa')
BEGIN
    ALTER TABLE [Ventas] ADD [MotivoAnulacion] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260211000810_Venta_Activa')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260211000810_Venta_Activa', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217220901_CreateTenantClean')
BEGIN
    CREATE TABLE [Tenants] (
        [Id] int NOT NULL IDENTITY,
        [NombreComercio] nvarchar(max) NOT NULL,
        [Activo] bit NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217220901_CreateTenantClean')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260217220901_CreateTenantClean', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    ALTER TABLE [Ventas] ADD [TenantId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    ALTER TABLE [VentaDetalles] ADD [TenantId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    ALTER TABLE [Productos] ADD [TenantId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    ALTER TABLE [MovimientosStock] ADD [TenantId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    CREATE INDEX [IX_Ventas_TenantId] ON [Ventas] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    CREATE INDEX [IX_VentaDetalles_TenantId] ON [VentaDetalles] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    CREATE INDEX [IX_Productos_TenantId] ON [Productos] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    CREATE INDEX [IX_MovimientosStock_TenantId] ON [MovimientosStock] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    ALTER TABLE [MovimientosStock] ADD CONSTRAINT [FK_MovimientosStock_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    ALTER TABLE [Productos] ADD CONSTRAINT [FK_Productos_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    ALTER TABLE [VentaDetalles] ADD CONSTRAINT [FK_VentaDetalles_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    ALTER TABLE [Ventas] ADD CONSTRAINT [FK_Ventas_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217221827_AddTenantIdNullable')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260217221827_AddTenantIdNullable', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    ALTER TABLE [MovimientosStock] DROP CONSTRAINT [FK_MovimientosStock_Tenants_TenantId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    ALTER TABLE [Productos] DROP CONSTRAINT [FK_Productos_Tenants_TenantId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    ALTER TABLE [VentaDetalles] DROP CONSTRAINT [FK_VentaDetalles_Tenants_TenantId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    ALTER TABLE [Ventas] DROP CONSTRAINT [FK_Ventas_Tenants_TenantId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    DROP INDEX [IX_Ventas_TenantId] ON [Ventas];
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Ventas]') AND [c].[name] = N'TenantId');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Ventas] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [Ventas] ALTER COLUMN [TenantId] int NOT NULL;
    ALTER TABLE [Ventas] ADD DEFAULT 0 FOR [TenantId];
    CREATE INDEX [IX_Ventas_TenantId] ON [Ventas] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    DROP INDEX [IX_VentaDetalles_TenantId] ON [VentaDetalles];
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[VentaDetalles]') AND [c].[name] = N'TenantId');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [VentaDetalles] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [VentaDetalles] ALTER COLUMN [TenantId] int NOT NULL;
    ALTER TABLE [VentaDetalles] ADD DEFAULT 0 FOR [TenantId];
    CREATE INDEX [IX_VentaDetalles_TenantId] ON [VentaDetalles] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    DROP INDEX [IX_Productos_TenantId] ON [Productos];
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Productos]') AND [c].[name] = N'TenantId');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Productos] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [Productos] ALTER COLUMN [TenantId] int NOT NULL;
    ALTER TABLE [Productos] ADD DEFAULT 0 FOR [TenantId];
    CREATE INDEX [IX_Productos_TenantId] ON [Productos] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    DROP INDEX [IX_MovimientosStock_TenantId] ON [MovimientosStock];
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MovimientosStock]') AND [c].[name] = N'TenantId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [MovimientosStock] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [MovimientosStock] ALTER COLUMN [TenantId] int NOT NULL;
    ALTER TABLE [MovimientosStock] ADD DEFAULT 0 FOR [TenantId];
    CREATE INDEX [IX_MovimientosStock_TenantId] ON [MovimientosStock] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    ALTER TABLE [MovimientosStock] ADD CONSTRAINT [FK_MovimientosStock_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    ALTER TABLE [Productos] ADD CONSTRAINT [FK_Productos_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    ALTER TABLE [VentaDetalles] ADD CONSTRAINT [FK_VentaDetalles_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    ALTER TABLE [Ventas] ADD CONSTRAINT [FK_Ventas_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217224527_FixTenantCascade')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260217224527_FixTenantCascade', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217230813_AddTenantToUser')
BEGIN
    ALTER TABLE [AspNetUsers] ADD [TenantId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217230813_AddTenantToUser')
BEGIN
    CREATE INDEX [IX_AspNetUsers_TenantId] ON [AspNetUsers] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217230813_AddTenantToUser')
BEGIN
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260217230813_AddTenantToUser')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260217230813_AddTenantToUser', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260304232429_AddNombreVentaToVenta')
BEGIN
    ALTER TABLE [Ventas] ADD [NombreVenta] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260304232429_AddNombreVentaToVenta')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260304232429_AddNombreVentaToVenta', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260305002129_AddNombreVentaToVenta2')
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Ventas]') AND [c].[name] = N'NombreVenta');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Ventas] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [Ventas] ALTER COLUMN [NombreVenta] nvarchar(max) NOT NULL;
    ALTER TABLE [Ventas] ADD DEFAULT N'' FOR [NombreVenta];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260305002129_AddNombreVentaToVenta2')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260305002129_AddNombreVentaToVenta2', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260305003444_AddNombreVentaToVentaNull')
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Ventas]') AND [c].[name] = N'NombreVenta');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Ventas] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [Ventas] ALTER COLUMN [NombreVenta] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260305003444_AddNombreVentaToVentaNull')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260305003444_AddNombreVentaToVentaNull', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260305005450_AddBorrarNombreVenta')
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Ventas]') AND [c].[name] = N'NombreVenta');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Ventas] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [Ventas] DROP COLUMN [NombreVenta];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260305005450_AddBorrarNombreVenta')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260305005450_AddBorrarNombreVenta', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260308131859_AddCampoNombreVenta')
BEGIN
    ALTER TABLE [Ventas] ADD [NombreVenta] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260308131859_AddCampoNombreVenta')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260308131859_AddCampoNombreVenta', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310232715_TablaAuditoria')
BEGIN
    CREATE TABLE [Auditorias] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioId] nvarchar(max) NOT NULL,
        [Accion] nvarchar(max) NOT NULL,
        [Detalles] nvarchar(max) NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [TenantId] int NOT NULL,
        CONSTRAINT [PK_Auditorias] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Auditorias_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310232715_TablaAuditoria')
BEGIN
    CREATE INDEX [IX_Auditorias_TenantId] ON [Auditorias] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310232715_TablaAuditoria')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260310232715_TablaAuditoria', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310235442_TablaAuditoriaCompleta')
BEGIN
    ALTER TABLE [Auditorias] DROP CONSTRAINT [FK_Auditorias_Tenants_TenantId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310235442_TablaAuditoriaCompleta')
BEGIN
    DROP INDEX [IX_Auditorias_TenantId] ON [Auditorias];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310235442_TablaAuditoriaCompleta')
BEGIN
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Auditorias]') AND [c].[name] = N'Detalles');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Auditorias] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [Auditorias] DROP COLUMN [Detalles];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310235442_TablaAuditoriaCompleta')
BEGIN
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Auditorias]') AND [c].[name] = N'UsuarioId');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Auditorias] DROP CONSTRAINT [' + @var15 + '];');
    ALTER TABLE [Auditorias] ALTER COLUMN [UsuarioId] nvarchar(450) NOT NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310235442_TablaAuditoriaCompleta')
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Auditorias]') AND [c].[name] = N'Accion');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Auditorias] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [Auditorias] ALTER COLUMN [Accion] nvarchar(50) NOT NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310235442_TablaAuditoriaCompleta')
BEGIN
    ALTER TABLE [Auditorias] ADD [Descripcion] nvarchar(500) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310235442_TablaAuditoriaCompleta')
BEGIN
    ALTER TABLE [Auditorias] ADD [RegistroId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310235442_TablaAuditoriaCompleta')
BEGIN
    ALTER TABLE [Auditorias] ADD [Tabla] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260310235442_TablaAuditoriaCompleta')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260310235442_TablaAuditoriaCompleta', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260311224435_VentaUsuario')
BEGIN
    ALTER TABLE [Ventas] ADD [UsuarioId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260311224435_VentaUsuario')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260311224435_VentaUsuario', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312224848_AddModelParaAuditoriasCajas')
BEGIN
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Ventas]') AND [c].[name] = N'UsuarioId');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Ventas] DROP CONSTRAINT [' + @var17 + '];');
    ALTER TABLE [Ventas] ALTER COLUMN [UsuarioId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312224848_AddModelParaAuditoriasCajas')
BEGIN
    CREATE INDEX [IX_Ventas_UsuarioId] ON [Ventas] ([UsuarioId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312224848_AddModelParaAuditoriasCajas')
BEGIN
    ALTER TABLE [Ventas] ADD CONSTRAINT [FK_Ventas_AspNetUsers_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312224848_AddModelParaAuditoriasCajas')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260312224848_AddModelParaAuditoriasCajas', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312232139_AddUserEnVenta')
BEGIN
    ALTER TABLE [Ventas] DROP CONSTRAINT [FK_Ventas_AspNetUsers_UsuarioId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312232139_AddUserEnVenta')
BEGIN
    ALTER TABLE [Ventas] ADD [UsuarioId1] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312232139_AddUserEnVenta')
BEGIN
    CREATE INDEX [IX_Ventas_UsuarioId1] ON [Ventas] ([UsuarioId1]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312232139_AddUserEnVenta')
BEGIN
    ALTER TABLE [Ventas] ADD CONSTRAINT [FK_Ventas_AspNetUsers_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312232139_AddUserEnVenta')
BEGIN
    ALTER TABLE [Ventas] ADD CONSTRAINT [FK_Ventas_AspNetUsers_UsuarioId1] FOREIGN KEY ([UsuarioId1]) REFERENCES [AspNetUsers] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312232139_AddUserEnVenta')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260312232139_AddUserEnVenta', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312234334_AddSuprimirUserIdentityName')
BEGIN
    ALTER TABLE [Ventas] DROP CONSTRAINT [FK_Ventas_AspNetUsers_UsuarioId1];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312234334_AddSuprimirUserIdentityName')
BEGIN
    DROP INDEX [IX_Ventas_UsuarioId1] ON [Ventas];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312234334_AddSuprimirUserIdentityName')
BEGIN
    DECLARE @var18 sysname;
    SELECT @var18 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Ventas]') AND [c].[name] = N'UsuarioId1');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Ventas] DROP CONSTRAINT [' + @var18 + '];');
    ALTER TABLE [Ventas] DROP COLUMN [UsuarioId1];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260312234334_AddSuprimirUserIdentityName')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260312234334_AddSuprimirUserIdentityName', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260313001528_AddListAuditorias')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260313001528_AddListAuditorias', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260314185326_AddUsuarioEnVentas')
BEGIN
    ALTER TABLE [Ventas] ADD [Usuario] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260314185326_AddUsuarioEnVentas')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260314185326_AddUsuarioEnVentas', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260314200815_AddUsuarioEnHistorialYCaja')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260314200815_AddUsuarioEnHistorialYCaja', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260315212409_AgregaMLToken')
BEGIN
    CREATE TABLE [MercadoLibreTokens] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [AccessToken] nvarchar(max) NOT NULL,
        [RefreshToken] nvarchar(max) NOT NULL,
        [Expiracion] datetime2 NOT NULL,
        CONSTRAINT [PK_MercadoLibreTokens] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260315212409_AgregaMLToken')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260315212409_AgregaMLToken', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321165224_AddRubroEntity')
BEGIN
    IF OBJECT_ID('dbo.MercadoLibreTokens','U') IS NOT NULL DROP TABLE [MercadoLibreTokens];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321165224_AddRubroEntity')
BEGIN
    ALTER TABLE [Productos] ADD [RubroId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321165224_AddRubroEntity')
BEGIN
    CREATE TABLE [Rubros] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Rubros] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321165224_AddRubroEntity')
BEGIN
    CREATE INDEX [IX_Productos_RubroId] ON [Productos] ([RubroId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321165224_AddRubroEntity')
BEGIN
    ALTER TABLE [Productos] ADD CONSTRAINT [FK_Productos_Rubros_RubroId] FOREIGN KEY ([RubroId]) REFERENCES [Rubros] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321165224_AddRubroEntity')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260321165224_AddRubroEntity', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321175309_AddTenantToRubro')
BEGIN
    ALTER TABLE [Rubros] ADD [TenantId] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321175309_AddTenantToRubro')
BEGIN
    CREATE INDEX [IX_Rubros_TenantId] ON [Rubros] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321175309_AddTenantToRubro')
BEGIN
    ALTER TABLE [Rubros] ADD CONSTRAINT [FK_Rubros_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321175309_AddTenantToRubro')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260321175309_AddTenantToRubro', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321231347_FixRubroIdNullable')
BEGIN
    ALTER TABLE [Productos] DROP CONSTRAINT [FK_Productos_Rubros_RubroId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321231347_FixRubroIdNullable')
BEGIN
    DECLARE @var19 sysname;
    SELECT @var19 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Productos]') AND [c].[name] = N'RubroId');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Productos] DROP CONSTRAINT [' + @var19 + '];');
    ALTER TABLE [Productos] ALTER COLUMN [RubroId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321231347_FixRubroIdNullable')
BEGIN
    ALTER TABLE [Productos] ADD CONSTRAINT [FK_Productos_Rubros_RubroId] FOREIGN KEY ([RubroId]) REFERENCES [Rubros] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321231347_FixRubroIdNullable')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260321231347_FixRubroIdNullable', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260321232732_FixSnapshot')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260321232732_FixSnapshot', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322122848_AddActivoEnRubro')
BEGIN
    ALTER TABLE [Rubros] ADD [Activo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322122848_AddActivoEnRubro')
BEGIN
    DROP INDEX [IX_Productos_RubroId] ON [Productos];
    DECLARE @var20 sysname;
    SELECT @var20 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Productos]') AND [c].[name] = N'RubroId');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Productos] DROP CONSTRAINT [' + @var20 + '];');
    ALTER TABLE [Productos] ALTER COLUMN [RubroId] int NOT NULL;
    ALTER TABLE [Productos] ADD DEFAULT 0 FOR [RubroId];
    CREATE INDEX [IX_Productos_RubroId] ON [Productos] ([RubroId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322122848_AddActivoEnRubro')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260322122848_AddActivoEnRubro', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322192838_ConfiguracionNegocio')
BEGIN
    ALTER TABLE [Tenants] ADD [CUIT] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322192838_ConfiguracionNegocio')
BEGIN
    ALTER TABLE [Tenants] ADD [Direccion] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322192838_ConfiguracionNegocio')
BEGIN
    ALTER TABLE [Tenants] ADD [Email] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322192838_ConfiguracionNegocio')
BEGIN
    ALTER TABLE [Tenants] ADD [MensajeTicket] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322192838_ConfiguracionNegocio')
BEGIN
    ALTER TABLE [Tenants] ADD [Telefono] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322192838_ConfiguracionNegocio')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260322192838_ConfiguracionNegocio', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322203701_AgregaClienteYFiado')
BEGIN
    ALTER TABLE [Ventas] ADD [ClienteId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322203701_AgregaClienteYFiado')
BEGIN
    ALTER TABLE [Ventas] ADD [EsFiado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322203701_AgregaClienteYFiado')
BEGIN
    CREATE TABLE [Clientes] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(100) NOT NULL,
        [Telefono] nvarchar(20) NULL,
        [LimiteCredito] decimal(18,2) NOT NULL,
        [Activo] bit NOT NULL,
        [TenantId] int NOT NULL,
        CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Clientes_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322203701_AgregaClienteYFiado')
BEGIN
    CREATE TABLE [PagosFiado] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [Monto] decimal(18,2) NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [Observacion] nvarchar(200) NULL,
        [TenantId] int NOT NULL,
        CONSTRAINT [PK_PagosFiado] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PagosFiado_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PagosFiado_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322203701_AgregaClienteYFiado')
BEGIN
    CREATE INDEX [IX_Ventas_ClienteId] ON [Ventas] ([ClienteId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322203701_AgregaClienteYFiado')
BEGIN
    CREATE INDEX [IX_Clientes_TenantId] ON [Clientes] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322203701_AgregaClienteYFiado')
BEGIN
    CREATE INDEX [IX_PagosFiado_ClienteId] ON [PagosFiado] ([ClienteId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322203701_AgregaClienteYFiado')
BEGIN
    CREATE INDEX [IX_PagosFiado_TenantId] ON [PagosFiado] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322203701_AgregaClienteYFiado')
BEGIN
    ALTER TABLE [Ventas] ADD CONSTRAINT [FK_Ventas_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322203701_AgregaClienteYFiado')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260322203701_AgregaClienteYFiado', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260322214216_FixVentaSnapshot')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260322214216_FixVentaSnapshot', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260325233941_AgregarCobrosTenant')
BEGIN
    ALTER TABLE [Tenants] ADD [Bloqueado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260325233941_AgregarCobrosTenant')
BEGIN
    ALTER TABLE [Tenants] ADD [FechaVencimiento] datetime2 NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260325233941_AgregarCobrosTenant')
BEGIN
    ALTER TABLE [Tenants] ADD [PrecioMensual] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260325233941_AgregarCobrosTenant')
BEGIN
    CREATE TABLE [CobrosTenants] (
        [Id] int NOT NULL IDENTITY,
        [Monto] decimal(18,2) NOT NULL,
        [FechaPago] datetime2 NOT NULL,
        [FechaVencimiento] datetime2 NOT NULL,
        [Observacion] nvarchar(500) NULL,
        [TenantId] int NOT NULL,
        CONSTRAINT [PK_CobrosTenants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CobrosTenants_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260325233941_AgregarCobrosTenant')
BEGIN
    CREATE INDEX [IX_CobrosTenants_TenantId] ON [CobrosTenants] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260325233941_AgregarCobrosTenant')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260325233941_AgregarCobrosTenant', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260327010854_AddProductoBaseAndRelation')
BEGIN
    ALTER TABLE [Productos] ADD [ProductoBaseId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260327010854_AddProductoBaseAndRelation')
BEGIN
    CREATE TABLE [ProductosBase] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(200) NOT NULL,
        [CodigoBarras] nvarchar(50) NOT NULL,
        [Marca] nvarchar(100) NULL,
        [Categoria] nvarchar(100) NULL,
        [PrecioReferencia] decimal(18,2) NULL,
        CONSTRAINT [PK_ProductosBase] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260327010854_AddProductoBaseAndRelation')
BEGIN
    CREATE INDEX [IX_Tenants_FechaVencimiento] ON [Tenants] ([FechaVencimiento]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260327010854_AddProductoBaseAndRelation')
BEGIN
    CREATE INDEX [IX_Productos_ProductoBaseId] ON [Productos] ([ProductoBaseId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260327010854_AddProductoBaseAndRelation')
BEGIN
    CREATE INDEX [IX_CobrosTenants_FechaPago] ON [CobrosTenants] ([FechaPago]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260327010854_AddProductoBaseAndRelation')
BEGIN
    CREATE UNIQUE INDEX [IX_ProductosBase_CodigoBarras] ON [ProductosBase] ([CodigoBarras]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260327010854_AddProductoBaseAndRelation')
BEGIN
    ALTER TABLE [Productos] ADD CONSTRAINT [FK_Productos_ProductosBase_ProductoBaseId] FOREIGN KEY ([ProductoBaseId]) REFERENCES [ProductosBase] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260327010854_AddProductoBaseAndRelation')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260327010854_AddProductoBaseAndRelation', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260329223422_Notificaciones')
BEGIN
    CREATE TABLE [Notificaciones] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [Titulo] nvarchar(100) NOT NULL,
        [Mensaje] nvarchar(500) NOT NULL,
        [Tipo] nvarchar(50) NOT NULL,
        [Leida] bit NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [Url] nvarchar(255) NULL,
        CONSTRAINT [PK_Notificaciones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notificaciones_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260329223422_Notificaciones')
BEGIN
    CREATE INDEX [IX_Notificaciones_TenantId] ON [Notificaciones] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260329223422_Notificaciones')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260329223422_Notificaciones', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260329234050_FechaVencimientoProducto')
BEGIN
    ALTER TABLE [ProductosBase] ADD [FechaVencimiento] datetime2 NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260329234050_FechaVencimientoProducto')
BEGIN
    ALTER TABLE [Productos] ADD [FechaVencimiento] datetime2 NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260329234050_FechaVencimientoProducto')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260329234050_FechaVencimientoProducto', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260330001616_RemoveFechaVencimientoProductoBase')
BEGIN
    DECLARE @var21 sysname;
    SELECT @var21 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductosBase]') AND [c].[name] = N'FechaVencimiento');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [ProductosBase] DROP CONSTRAINT [' + @var21 + '];');
    ALTER TABLE [ProductosBase] DROP COLUMN [FechaVencimiento];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260330001616_RemoveFechaVencimientoProductoBase')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260330001616_RemoveFechaVencimientoProductoBase', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260330224427_LotesProducto')
BEGIN
    CREATE TABLE [LotesProducto] (
        [Id] int NOT NULL IDENTITY,
        [ProductoId] int NOT NULL,
        [TenantId] int NOT NULL,
        [Cantidad] decimal(18,2) NOT NULL,
        [FechaVencimiento] datetime2 NULL,
        [FechaIngreso] datetime2 NOT NULL,
        CONSTRAINT [PK_LotesProducto] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LotesProducto_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LotesProducto_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260330224427_LotesProducto')
BEGIN
    CREATE INDEX [IX_LotesProducto_ProductoId] ON [LotesProducto] ([ProductoId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260330224427_LotesProducto')
BEGIN
    CREATE INDEX [IX_LotesProducto_TenantId] ON [LotesProducto] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260330224427_LotesProducto')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260330224427_LotesProducto', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260331221521_AddCajaAndConfiguracion')
BEGIN
    CREATE TABLE [Cajas] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [UsuarioId] nvarchar(450) NOT NULL,
        [FechaApertura] datetime2 NOT NULL,
        [FechaCierre] datetime2 NULL,
        [MontoInicial] decimal(18,2) NOT NULL,
        [TotalVentas] decimal(18,2) NOT NULL,
        [MontoReal] decimal(18,2) NULL,
        [Diferencia] decimal(18,2) NULL,
        [Estado] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Cajas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cajas_AspNetUsers_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cajas_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260331221521_AddCajaAndConfiguracion')
BEGIN
    CREATE TABLE [Configuraciones] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [UsarCaja] bit NOT NULL,
        CONSTRAINT [PK_Configuraciones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Configuraciones_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260331221521_AddCajaAndConfiguracion')
BEGIN
    CREATE INDEX [IX_Cajas_TenantId] ON [Cajas] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260331221521_AddCajaAndConfiguracion')
BEGIN
    CREATE INDEX [IX_Cajas_UsuarioId] ON [Cajas] ([UsuarioId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260331221521_AddCajaAndConfiguracion')
BEGIN
    CREATE INDEX [IX_Configuraciones_TenantId] ON [Configuraciones] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260331221521_AddCajaAndConfiguracion')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331221521_AddCajaAndConfiguracion', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260401004227_AddCajaIdToVenta')
BEGIN
    ALTER TABLE [Ventas] ADD [CajaId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260401004227_AddCajaIdToVenta')
BEGIN
    CREATE INDEX [IX_Ventas_CajaId] ON [Ventas] ([CajaId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260401004227_AddCajaIdToVenta')
BEGIN
    ALTER TABLE [Ventas] ADD CONSTRAINT [FK_Ventas_Cajas_CajaId] FOREIGN KEY ([CajaId]) REFERENCES [Cajas] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260401004227_AddCajaIdToVenta')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260401004227_AddCajaIdToVenta', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260403190827_AddUniqueProductoPorTenant')
BEGIN
    DROP INDEX [IX_Productos_TenantId] ON [Productos];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260403190827_AddUniqueProductoPorTenant')
BEGIN
    CREATE UNIQUE INDEX [IX_Productos_TenantId_CodigoBarra] ON [Productos] ([TenantId], [CodigoBarra]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260403190827_AddUniqueProductoPorTenant')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260403190827_AddUniqueProductoPorTenant', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260403220813_AddEliminarStockYFechaVencProducto')
BEGIN
    DECLARE @var22 sysname;
    SELECT @var22 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MovimientosStock]') AND [c].[name] = N'Tipo');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [MovimientosStock] DROP CONSTRAINT [' + @var22 + '];');
    ALTER TABLE [MovimientosStock] ALTER COLUMN [Tipo] nvarchar(50) NOT NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260403220813_AddEliminarStockYFechaVencProducto')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260403220813_AddEliminarStockYFechaVencProducto', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260410010753_AgregarTablaProveedores')
BEGIN
    CREATE TABLE [Proveedores] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(100) NOT NULL,
        [Telefono] nvarchar(20) NULL,
        [Email] nvarchar(100) NULL,
        [Direccion] nvarchar(200) NULL,
        [CUIT] nvarchar(20) NULL,
        [Activo] bit NOT NULL,
        [FechaAlta] datetime2 NOT NULL,
        [TenantId] int NOT NULL,
        CONSTRAINT [PK_Proveedores] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Proveedores_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260410010753_AgregarTablaProveedores')
BEGIN
    CREATE INDEX [IX_Proveedores_TenantId] ON [Proveedores] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260410010753_AgregarTablaProveedores')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260410010753_AgregarTablaProveedores', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260412131059_AddProveedores')
BEGIN
    DECLARE @var23 sysname;
    SELECT @var23 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Proveedores]') AND [c].[name] = N'Telefono');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Proveedores] DROP CONSTRAINT [' + @var23 + '];');
    ALTER TABLE [Proveedores] ALTER COLUMN [Telefono] nvarchar(20) NOT NULL;
    ALTER TABLE [Proveedores] ADD DEFAULT N'' FOR [Telefono];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260412131059_AddProveedores')
BEGIN
    ALTER TABLE [Proveedores] ADD [Contacto] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260412131059_AddProveedores')
BEGIN
    ALTER TABLE [Proveedores] ADD [Notas] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260412131059_AddProveedores')
BEGIN
    ALTER TABLE [Productos] ADD [ProveedorId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260412131059_AddProveedores')
BEGIN
    CREATE TABLE [ProductoProveedores] (
        [ProductoId] int NOT NULL,
        [ProveedorId] int NOT NULL,
        [PrecioCosto] decimal(18,2) NULL,
        [EsPrincipal] bit NOT NULL,
        CONSTRAINT [PK_ProductoProveedores] PRIMARY KEY ([ProductoId], [ProveedorId]),
        CONSTRAINT [FK_ProductoProveedores_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductoProveedores_Proveedores_ProveedorId] FOREIGN KEY ([ProveedorId]) REFERENCES [Proveedores] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260412131059_AddProveedores')
BEGIN
    CREATE INDEX [IX_Productos_ProveedorId] ON [Productos] ([ProveedorId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260412131059_AddProveedores')
BEGIN
    CREATE INDEX [IX_ProductoProveedores_ProveedorId] ON [ProductoProveedores] ([ProveedorId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260412131059_AddProveedores')
BEGIN
    ALTER TABLE [Productos] ADD CONSTRAINT [FK_Productos_Proveedores_ProveedorId] FOREIGN KEY ([ProveedorId]) REFERENCES [Proveedores] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260412131059_AddProveedores')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260412131059_AddProveedores', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260503164221_AddEsPorPesoToProductoBase')
BEGIN
    ALTER TABLE [ProductosBase] ADD [EsPorPeso] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260503164221_AddEsPorPesoToProductoBase')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503164221_AddEsPorPesoToProductoBase', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260503174539_RemoveEsPorPesoFromProductoBase')
BEGIN
    DECLARE @var24 sysname;
    SELECT @var24 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductosBase]') AND [c].[name] = N'EsPorPeso');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [ProductosBase] DROP CONSTRAINT [' + @var24 + '];');
    ALTER TABLE [ProductosBase] DROP COLUMN [EsPorPeso];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260503174539_RemoveEsPorPesoFromProductoBase')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503174539_RemoveEsPorPesoFromProductoBase', N'6.0.36');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260503195935_AddPagosTable')
BEGIN
    CREATE TABLE [Pagos] (
        [Id] int NOT NULL IDENTITY,
        [VentaId] int NOT NULL,
        [TenantId] int NOT NULL,
        [Metodo] nvarchar(20) NOT NULL,
        [Monto] decimal(18,2) NOT NULL,
        [MontoRecibido] decimal(18,2) NULL,
        [Vuelto] decimal(18,2) NULL,
        [Fecha] datetime2 NOT NULL,
        CONSTRAINT [PK_Pagos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pagos_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pagos_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260503195935_AddPagosTable')
BEGIN
    CREATE INDEX [IX_Pagos_TenantId] ON [Pagos] ([TenantId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260503195935_AddPagosTable')
BEGIN
    CREATE INDEX [IX_Pagos_VentaId] ON [Pagos] ([VentaId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260503195935_AddPagosTable')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503195935_AddPagosTable', N'6.0.36');
END;
GO

COMMIT;
GO

