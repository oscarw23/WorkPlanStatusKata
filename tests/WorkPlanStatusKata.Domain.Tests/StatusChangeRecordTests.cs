using FluentAssertions;
using WorkPlanStatusKata.Domain;
using Xunit;

namespace WorkPlanStatusKata.Domain.Tests;

public class StatusChangeRecordTests
{
    [Fact]
    public void CR11_CambioDeEstado_DebeQuedarDisponibleParaRegistrarEnHistorico()
    {
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.SinAsignar, TecnicoId: Guid.NewGuid(), Actividades: []);
        var resultado = new DerivationResult(WorkPlanStatus.Asignada, Cambio: true, ReglaAplicada: "CR02", Motivo: "Se asignó técnico a la orden");
        var fechaHora = new DateTimeOffset(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);

        var registro = StatusChangeRecord.Crear(workPlan, resultado, usuario: "tecnico1", fechaHora);

        registro.WorkPlanId.Should().Be(workPlan.Id);
        registro.EstadoAnterior.Should().Be(WorkPlanStatus.SinAsignar);
        registro.EstadoNuevo.Should().Be(WorkPlanStatus.Asignada);
        registro.ReglaAplicada.Should().Be("CR02");
        registro.Usuario.Should().Be("tecnico1");
        registro.FechaHora.Should().Be(fechaHora);
        registro.Motivo.Should().Be("Se asignó técnico a la orden");
    }

    [Fact]
    public void CR11_DebeIncluirSnapshotDeActividadesEnElMomentoDelCambio()
    {
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.EnProceso, EsFisicoQuimico: false),
            new(Guid.NewGuid(), ActivityStatus.Creada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.Asignada, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var resultado = new DerivationResult(WorkPlanStatus.EnProceso, Cambio: true, ReglaAplicada: "CR03", Motivo: "La primera actividad salió de Creada");

        var registro = StatusChangeRecord.Crear(workPlan, resultado, usuario: "tecnico1", DateTimeOffset.UtcNow);

        registro.ActividadesSnapshot.Should().BeEquivalentTo(actividades);
    }

    [Fact]
    public void CR11_MotivoPuedeQuedarVacio_NoDebeFallar()
    {
        // El motivo del cambio puede ir vacío — no hay validación que lo impida.
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.SinAsignar, TecnicoId: Guid.NewGuid(), Actividades: []);
        var resultado = new DerivationResult(WorkPlanStatus.Asignada, Cambio: true, ReglaAplicada: "CR02", Motivo: string.Empty);

        var registro = StatusChangeRecord.Crear(workPlan, resultado, usuario: "tecnico1", DateTimeOffset.UtcNow);

        registro.Motivo.Should().BeEmpty();
    }
}
