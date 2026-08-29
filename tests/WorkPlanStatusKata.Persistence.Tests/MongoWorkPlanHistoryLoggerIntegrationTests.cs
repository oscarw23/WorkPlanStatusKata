using FluentAssertions;
using MongoDB.Driver;
using WorkPlanStatusKata.Domain;
using Xunit;

namespace WorkPlanStatusKata.Persistence.Tests;

// Prueba de integración manual: requiere Mongo real corriendo en
// localhost:27017 (contenedor mongo-kata). No forma parte del ciclo
// TDD del dominio ni del handler — verifica plomería contra el driver real.
[Trait("Category", "Integration")]
public class MongoWorkPlanHistoryLoggerIntegrationTests
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private const string DatabaseName = "WorkPlanStatusKataTests";

    [Fact]
    public async Task RegistrarAsync_DebeInsertarDocumentoEnLaColeccionWorkplanStatusHistory()
    {
        var logger = new MongoWorkPlanHistoryLogger(ConnectionString, DatabaseName);
        var workPlan = new WorkPlan(
            Guid.NewGuid(),
            WorkPlanStatus.SinAsignar,
            TecnicoId: Guid.NewGuid(),
            Actividades: [new Activity(Guid.NewGuid(), ActivityStatus.Creada, EsFisicoQuimico: false)]);
        var resultado = new DerivationResult(WorkPlanStatus.Asignada, Cambio: true, "CR02", "Se asignó técnico a la orden");
        var registro = StatusChangeRecord.Crear(workPlan, resultado, usuario: "integracion-test", DateTimeOffset.UtcNow);

        await logger.RegistrarAsync(registro);

        var cliente = new MongoClient(ConnectionString);
        var coleccion = cliente.GetDatabase(DatabaseName)
            .GetCollection<WorkPlanStatusHistoryDocument>(MongoWorkPlanHistoryLogger.NombreColeccion);
        var filtro = Builders<WorkPlanStatusHistoryDocument>.Filter.Eq(d => d.WorkPlanId, workPlan.Id);

        try
        {
            var documento = await coleccion.Find(filtro).FirstOrDefaultAsync();

            documento.Should().NotBeNull();
            documento!.EstadoAnterior.Should().Be("SinAsignar");
            documento.EstadoNuevo.Should().Be("Asignada");
            documento.ReglaAplicada.Should().Be("CR02");
            documento.Usuario.Should().Be("integracion-test");
            documento.ActividadesSnapshot.Should().ContainSingle();
        }
        finally
        {
            // No dejar basura en la colección real entre corridas.
            await coleccion.DeleteOneAsync(filtro);
        }
    }
}
