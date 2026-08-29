using FluentAssertions;
using WorkPlanStatusKata.Domain;
using WorkPlanStatusKata.Persistence;
using Xunit;

namespace WorkPlanStatusKata.Persistence.Tests;

public class WorkPlanStatusChangeHandlerTests
{
    [Fact]
    public async Task CaminoFeliz_LoggerNoFalla_RegistraHistoricoYDevuelveResultadoCorrecto()
    {
        // Arrange: orden Sin asignar a la que se le asigna un técnico (CR02).
        var tecnicoId = Guid.NewGuid();
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.SinAsignar, TecnicoId: tecnicoId, Actividades: []);
        var fechaHora = new DateTimeOffset(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);

        var sqlRepository = new FakeSqlWorkPlanRepository();
        var historyLogger = new FakeHistoryLogger();
        var sut = new WorkPlanStatusChangeHandler(new WorkPlanStatusDerivator(), sqlRepository, historyLogger);

        // Act
        var resultado = await sut.ProcesarAsync(workPlan, StatusChangeTrigger.AsignacionTecnico, usuario: "tecnico1", fechaHora);

        // Assert: el resultado de la derivación es el esperado.
        resultado.NuevoEstado.Should().Be(WorkPlanStatus.Asignada);
        resultado.ReglaAplicada.Should().Be("CR02");
        resultado.Cambio.Should().BeTrue();

        // Assert: el SQL (fuente de verdad) quedó actualizado.
        sqlRepository.Actualizaciones.Should().ContainSingle()
            .Which.Should().Be((workPlan.Id, WorkPlanStatus.Asignada));

        // Assert: el histórico quedó registrado con los datos correctos.
        historyLogger.Registros.Should().ContainSingle();
        var registro = historyLogger.Registros[0];
        registro.WorkPlanId.Should().Be(workPlan.Id);
        registro.EstadoAnterior.Should().Be(WorkPlanStatus.SinAsignar);
        registro.EstadoNuevo.Should().Be(WorkPlanStatus.Asignada);
        registro.ReglaAplicada.Should().Be("CR02");
        registro.Usuario.Should().Be("tecnico1");
        registro.FechaHora.Should().Be(fechaHora);
    }

    [Fact]
    public async Task FalloDelLogger_NoDebePropagarLaExcepcion()
    {
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.SinAsignar, TecnicoId: Guid.NewGuid(), Actividades: []);
        var sqlRepository = new FakeSqlWorkPlanRepository();
        var historyLogger = new FakeHistoryLogger { DebeFallar = true };
        var sut = new WorkPlanStatusChangeHandler(new WorkPlanStatusDerivator(), sqlRepository, historyLogger);

        Func<Task> act = () => sut.ProcesarAsync(workPlan, StatusChangeTrigger.AsignacionTecnico, usuario: "tecnico1", DateTimeOffset.UtcNow);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FalloDelLogger_NoDebeImpedirLaActualizacionEnSqlNiCambiarElResultado()
    {
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.SinAsignar, TecnicoId: Guid.NewGuid(), Actividades: []);
        var sqlRepository = new FakeSqlWorkPlanRepository();
        var historyLogger = new FakeHistoryLogger { DebeFallar = true };
        var sut = new WorkPlanStatusChangeHandler(new WorkPlanStatusDerivator(), sqlRepository, historyLogger);

        var resultado = await sut.ProcesarAsync(workPlan, StatusChangeTrigger.AsignacionTecnico, usuario: "tecnico1", DateTimeOffset.UtcNow);

        resultado.NuevoEstado.Should().Be(WorkPlanStatus.Asignada);
        resultado.ReglaAplicada.Should().Be("CR02");
        sqlRepository.Actualizaciones.Should().ContainSingle()
            .Which.Should().Be((workPlan.Id, WorkPlanStatus.Asignada));
    }
}
