using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;

namespace SchemaCheck;

/// <summary>
/// Compara el esquema de una base creada SOLO con migraciones contra el que
/// produce el modelo de EF. Si difieren, existe deriva: hay cosas en el modelo
/// (o en el snapshot) que ninguna migracion crea. Eso es exactamente el
/// patron que produjo P-002 y P-023, y que rompia cualquier entorno nuevo.
///
/// Por que NO alcanza con "dotnet ef migrations add": si el snapshot ya esta
/// adelantado respecto de las migraciones, EF no genera nada y cree que todo
/// esta bien. Hay que contrastar contra la base real.
///
/// Uso:
///   ConnectionStrings__MiniMercadoDB='...' dotnet run --project tools/SchemaCheck
///
/// Devuelve 0 si no hay deriva, 1 si la hay, 2 si falta configuracion.
/// </summary>
public static class Program
{
    private const string TablaMigraciones = "__EFMigrationsHistory";

    public static async Task<int> Main()
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__MiniMercadoDB");
        if (string.IsNullOrWhiteSpace(conn))
        {
            Console.Error.WriteLine("Falta la variable de entorno ConnectionStrings__MiniMercadoDB.");
            return 2;
        }

        var builder = new SqlConnectionStringBuilder(conn);
        var baseOriginal = builder.InitialCatalog;
        var baseMigraciones = $"{baseOriginal}_check_migraciones";
        var baseModelo = $"{baseOriginal}_check_modelo";

        // El nombre viene de configuracion: lo validamos antes de interpolarlo
        // en SQL, aunque esto sea una herramienta de desarrollo.
        foreach (var nombre in new[] { baseOriginal, baseMigraciones, baseModelo })
        {
            if (!Regex.IsMatch(nombre, "^[A-Za-z0-9_]+$"))
            {
                Console.Error.WriteLine($"Nombre de base invalido: {nombre}");
                return 2;
            }
        }

        Console.WriteLine($"Base de referencia: {baseOriginal}");
        Console.WriteLine($"  A) {_Descripcion(baseMigraciones, "creada con las migraciones")}");
        Console.WriteLine($"  B) {_Descripcion(baseModelo, "creada desde el modelo (GenerateCreateScript)")}");

        // Admin en master: CREATE/DROP DATABASE no necesita que exista la base de
        // referencia (en CI no existe) y desde master se puede dropear cualquier cosa.
        await using (var admin = new SqlConnection(ConBase(conn, "master")))
        {
            await admin.OpenAsync();
            foreach (var nombre in new[] { baseMigraciones, baseModelo })
            {
                await RecrearBase(admin, nombre);
            }
        }

        // A) Esquema que produce el historial de migraciones.
        var optsMigraciones = new DbContextOptionsBuilder<MiniMercadoContext>()
            .UseSqlServer(ConBase(conn, baseMigraciones))
            .Options;
        await using (var ctx = new MiniMercadoContext(optsMigraciones, new TenantServiceDeHerramienta()))
        {
            await ctx.Database.MigrateAsync();
        }

        // B) Esquema que produce el modelo. NO se usa EnsureCreated(): en EF Core 6
        // EnsureCreated() aplica las migraciones si el contexto las tiene, o sea
        // daria el mismo esquema que A y no serviria para comparar. GenerateCreateScript()
        // traduce el modelo a DDL ignorando por completo el historial de migraciones.
        var optsModelo = new DbContextOptionsBuilder<MiniMercadoContext>()
            .UseSqlServer(ConBase(conn, baseModelo))
            .Options;
        string scriptModelo;
        await using (var ctx = new MiniMercadoContext(optsModelo, new TenantServiceDeHerramienta()))
        {
            scriptModelo = ctx.Database.GenerateCreateScript();
        }
        var erroresModelo = await EjecutarScript(conn, baseModelo, scriptModelo);

        var esquemaMigraciones = await LeerEsquema(conn, baseMigraciones);
        var esquemaModelo = await LeerEsquema(conn, baseModelo);

        var diferencias = Comparar(esquemaMigraciones, esquemaModelo);

        await EliminarBase(conn, baseMigraciones);
        await EliminarBase(conn, baseModelo);

        if (erroresModelo.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"El esquema que produce el MODELO no se puede crear en SQL Server ({erroresModelo.Count} error(es)).");
            foreach (var e in erroresModelo.Distinct().OrderBy(e => e))
            {
                Console.WriteLine("  - " + e);
            }
            Console.WriteLine("Esto es una contradiccion dentro del modelo de EF, no un problema de migraciones.");
            Console.WriteLine("Lo tipico es un FK con DeleteBehavior.SetNull sobre una columna que el");
            Console.WriteLine("propio modelo declara obligatoria: SQL Server lo acepta al crear el FK y");
            Console.WriteLine("falla recien cuando alguien borra la fila padre.");
            Console.WriteLine("OJO: la comparacion de abajo es PARCIAL. Las tablas que no se pudieron");
            Console.WriteLine("crear desde el modelo no se comparan, asi que la ausencia de diferencias");
            Console.WriteLine("no demuestra que no haya problemas en ellas.");
        }

        if (diferencias.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("Sin deriva: lo que declara el modelo esta todo en las migraciones.");
            return erroresModelo.Count == 0 ? 0 : 1;
        }

        Console.WriteLine();
        Console.WriteLine($"DERENA DETECTADA: {diferencias.Count} diferencia(s).");
        Console.WriteLine("El modelo declara cosas que ninguna migracion crea. Alguien cambio el");
        Console.WriteLine("modelo o el snapshot sin generar la migracion correspondiente.");
        foreach (var d in diferencias)
        {
            Console.WriteLine("  - " + d);
        }
        Console.WriteLine();
        Console.WriteLine("Como corregirlo:");
        Console.WriteLine("  1. Si el snapshot esta adelantado: sacar del snapshot lo que no exista");
        Console.WriteLine("     todavia en las migraciones, correr 'migrations add' y devolver al");
        Console.WriteLine("     snapshot las propiedades (EF las reescribe).");
        Console.WriteLine("  2. Si falta en el modelo: agregar la migracion que lo cree.");
        Console.WriteLine("  Nunca parchear el esquema con SQL suelto en Program.cs: eso es lo que");
        Console.WriteLine("  hacia la app funcionar en la maquina de desarrollo y romperse en");
        Console.WriteLine("  cualquier otro entorno (ver P-002).");
        return 1;
    }
    private static async Task<List<string>> EjecutarScript(string conn, string nombreBase, string script)
    {
        var errores = new List<string>();
        await using var cn = new SqlConnection(ConBase(conn, nombreBase));
        await cn.OpenAsync();

        // GenerateCreateScript() separa los lotes con GO, que no es T-SQL valido
        // para SqlCommand: hay que partir por GO y despues por ';'.
        var lotes = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var lote in lotes)
        {
            foreach (var sentencia in lote.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(sentencia)) continue;
                await using var cmd = cn.CreateCommand();
                cmd.CommandTimeout = 120;
                cmd.CommandText = sentencia;
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    // No se aborta: interesa el reporte completo, no la primera falla.
                    errores.Add($"{ex.Number}: {ex.Message.Split('\n')[0]}");
                }
            }
        }

        return errores;
    }

    private static string _Descripcion(string baseNombre, string como) => $"'{baseNombre}' ({como})";

    private static string ConBase(string conn, string nombre) =>
        new SqlConnectionStringBuilder(conn) { InitialCatalog = nombre }.ConnectionString;

    private static async Task RecrearBase(SqlConnection admin, string nombre)
    {
        await using var cmd = admin.CreateCommand();
        cmd.CommandText =
            "IF DB_ID(@nombre) IS NOT NULL BEGIN " +
            "  ALTER DATABASE [" + nombre + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
            "  DROP DATABASE [" + nombre + "]; END";
        cmd.Parameters.AddWithValue("@nombre", nombre);
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = "CREATE DATABASE [" + nombre + "]";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task EliminarBase(string conn, string nombre)
    {
        await using var admin = new SqlConnection(ConBase(conn, "master"));
        await admin.OpenAsync();
        await using var cmd = admin.CreateCommand();
        cmd.CommandText =
            "IF DB_ID(@nombre) IS NOT NULL BEGIN " +
            "  ALTER DATABASE [" + nombre + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
            "  DROP DATABASE [" + nombre + "]; END";
        cmd.Parameters.AddWithValue("@nombre", nombre);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Lee tablas, columnas, indices y claves foraneas. Se comparan los cuatro
    /// porque P-023 fue justamente un indice y un FK que faltaban.
    /// </summary>
    private static async Task<Esquema> LeerEsquema(string conn, string nombreBase)
    {
        var esquema = new Esquema();

        await using var cn = new SqlConnection(ConBase(conn, nombreBase));
        await cn.OpenAsync();

        await using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE " +
                "FROM INFORMATION_SCHEMA.COLUMNS";
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                var tabla = rd.GetString(0);
                if (tabla == TablaMigraciones) continue;
                esquema.Columnas.Add($"{tabla}.{rd.GetString(1)} {rd.GetString(2)} {(rd.GetString(3) == "YES" ? "NULL" : "NOT NULL")}");
            }
        }

        await using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT t.name, i.name, i.is_unique " +
                "FROM sys.indexes i " +
                "JOIN sys.tables t ON t.object_id = i.object_id " +
                "WHERE i.name IS NOT NULL AND i.is_primary_key = 0 AND t.name <> '__EFMigrationsHistory'";
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                esquema.Indices.Add($"{rd.GetString(0)}.{rd.GetString(1)} UNIQUE={(rd.GetBoolean(2) ? 1 : 0)}");
            }
        }

        await using (var cmd = cn.CreateCommand())
        {
cmd.CommandText =
                "SELECT pt.name, pc.name, rt.name, rc.name, fk.delete_referential_action_desc " +
                "FROM sys.foreign_keys fk " +
                "JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id " +
                "JOIN sys.tables pt ON pt.object_id = fk.parent_object_id " +
                "JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id " +
                "JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id " +
                "JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id";
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                esquema.ForeignKeys.Add($"{rd.GetString(0)}.{rd.GetString(1)} -> {rd.GetString(2)}.{rd.GetString(3)} ON DELETE {rd.GetString(4)}");
            }
        }

        return esquema;
    }

    private static List<string> Comparar(Esquema desdeMigraciones, Esquema desdeModelo)
    {
        var diferencias = new List<string>();

        AgregarFaltantes(diferencias, desdeMigraciones.Columnas, desdeModelo.Columnas, "columna");
        AgregarFaltantes(diferencias, desdeMigraciones.Indices, desdeModelo.Indices, "indice");
        AgregarFaltantes(diferencias, desdeMigraciones.ForeignKeys, desdeModelo.ForeignKeys, "FK");

        return diferencias;
    }

    /// <summary>
    /// El modelo es la verdad: lo que este en las migraciones y no en el modelo
    /// es residuo (por ejemplo la tabla huerfana de MercadoLibre, P-019) y no
    /// deriva. Por eso solo se reporta lo que falta respecto del modelo.
    /// </summary>
    private static void AgregarFaltantes(List<string> diferencias, HashSet<string> desdeMigraciones, HashSet<string> desdeModelo, string etiqueta)
    {
        foreach (var elemento in desdeModelo.Where(e => !desdeMigraciones.Contains(e)).OrderBy(e => e))
        {
            diferencias.Add($"falta {etiqueta}: {elemento}");
        }
    }

    private sealed class Esquema
    {
        public HashSet<string> Columnas { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Indices { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ForeignKeys { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class TenantServiceDeHerramienta : ITenantService
    {
        public int? GetTenantId() => null;
    }
}
