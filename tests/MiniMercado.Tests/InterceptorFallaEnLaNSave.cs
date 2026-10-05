using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MiniMercado.Tests
{
    /// <summary>
    /// Falla la N-esima llamada a SaveChangesAsync para simular un corte de conexion o
    /// un veto de la base en un punto exacto del guardado.
    ///
    /// Sirve para probar atomicidad (P-033) sin tener que romper SQL Server de verdad:
    /// si el codigo esta dentro de una transaccion, el fallo tiene que dejar la base
    /// como estaba.
    /// </summary>
    public class InterceptorFallaEnLaNSave : SaveChangesInterceptor
    {
        private readonly int _nesima;
        private int _contador;

        public InterceptorFallaEnLaNSave(int nesima)
        {
            _nesima = nesima;
        }

        /// <summary>Cantidad de veces que llego a Guardar.</summary>
        public int Llamadas { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Llamadas = ++_contador;

            if (Llamadas == _nesima)
                throw new InvalidOperationException(
                    $"Fallo simulado en la llamada numero {Llamadas} a SaveChangesAsync.");

            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            // Synchronous SaveChanges, el que usa AuditoriaService.Registrar.
            throw new InvalidOperationException("Fallo simulado en SaveChanges sincrono.");
        }
    }
}