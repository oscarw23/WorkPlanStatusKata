using FluentAssertions;
using WorkPlanStatusKata.Domain;
using Xunit;

namespace WorkPlanStatusKata.Domain.Tests;

public class WorkPlanStatusDerivatorTests
{
    [Fact]
    public void CR01_OrdenCreadaSinTecnicoAsignado_DebeQuedarSinAsignar()
    {
        // Arrange: una orden recién creada, sin técnico asignado.
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.SinAsignar, TecnicoId: null, Actividades: []);
        var sut = new WorkPlanStatusDerivator();

        // Act
        var result = sut.Derive(workPlan, StatusChangeTrigger.Creacion);

        // Assert
        result.NuevoEstado.Should().Be(WorkPlanStatus.SinAsignar);
        result.ReglaAplicada.Should().Be("CR01");
        result.Cambio.Should().BeFalse("la creación inicial no es una transición desde un estado anterior real");
    }

    [Fact]
    public void CR02_SeAsignaTecnico_DebeQuedarAsignada()
    {
        // Arrange: una orden sin asignar a la que se le asigna un técnico.
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.SinAsignar, TecnicoId: Guid.NewGuid(), Actividades: []);
        var sut = new WorkPlanStatusDerivator();

        // Act
        var result = sut.Derive(workPlan, StatusChangeTrigger.AsignacionTecnico);

        // Assert
        result.NuevoEstado.Should().Be(WorkPlanStatus.Asignada);
        result.ReglaAplicada.Should().Be("CR02");
        result.Cambio.Should().BeTrue();
    }

    [Fact]
    public void CasoNoContemplado_DebeLanzarNotImplemented()
    {
        // Arrange: combinación trigger/TecnicoId que ninguna regla implementada cubre todavía.
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.Asignada, TecnicoId: Guid.NewGuid(), Actividades: []);
        var sut = new WorkPlanStatusDerivator();

        // Act
        var act = () => sut.Derive(workPlan, StatusChangeTrigger.CambioActividad);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void CR03_PrimeraActividadSaleDeCreada_DebeQuedarEnProceso()
    {
        // Arrange: orden asignada, con una actividad que ya salió de "Creada" y otra que sigue en "Creada".
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.EnProceso, EsFisicoQuimico: false),
            new(Guid.NewGuid(), ActivityStatus.Creada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.Asignada, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        // Act
        var result = sut.Derive(workPlan, StatusChangeTrigger.CambioActividad);

        // Assert
        result.NuevoEstado.Should().Be(WorkPlanStatus.EnProceso);
        result.ReglaAplicada.Should().Be("CR03");
        result.Cambio.Should().BeTrue();
    }

    [Fact]
    public void CR04_SeCierraLaUltimaActividadPendiente_DebeQuedarFinalizada()
    {
        // Arrange: orden en proceso, con TODAS las actividades ya en estado final (Finalizada) —
        // sin mezcla con Creada/EnProceso, para no confundirse con el escenario de CR03.
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.Finalizada, EsFisicoQuimico: false),
            new(Guid.NewGuid(), ActivityStatus.Finalizada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.EnProceso, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        // Act
        var result = sut.Derive(workPlan, StatusChangeTrigger.CambioActividad);

        // Assert
        result.NuevoEstado.Should().Be(WorkPlanStatus.Finalizada);
        result.ReglaAplicada.Should().Be("CR04");
        result.Cambio.Should().BeTrue();
    }

    [Fact]
    public void CR05_CancelacionManualDesdeFinalizada_DebeQuedarCanceladaSinValidarActividades()
    {
        // Arrange: orden YA Finalizada, con actividades en estado final —
        // la cancelación manual debe aplicar igual, sin mirar las actividades.
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.Finalizada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.Finalizada, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        // Act
        var result = sut.Derive(workPlan, StatusChangeTrigger.CancelacionManual);

        // Assert
        result.NuevoEstado.Should().Be(WorkPlanStatus.Cancelada);
        result.ReglaAplicada.Should().Be("CR05");
        result.Cambio.Should().BeTrue();
    }

    [Fact]
    public void CR06_RechazoDeSoporteEnOrdenFinalizada_DebeVolverAEnProceso()
    {
        // Arrange: orden Finalizada; quien llama ya puso la actividad rechazada
        // en PendienteSoporte antes de invocar al derivador.
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.PendienteSoporte, EsFisicoQuimico: false),
            new(Guid.NewGuid(), ActivityStatus.Finalizada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.Finalizada, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        // Act
        var result = sut.Derive(workPlan, StatusChangeTrigger.RechazoSoporte);

        // Assert
        result.NuevoEstado.Should().Be(WorkPlanStatus.EnProceso);
        result.ReglaAplicada.Should().Be("CR06");
        result.Cambio.Should().BeTrue();
    }

    [Fact]
    public void CR07_UltimaActividadFallida_CuentaComoReportada_DebeQuedarFinalizada()
    {
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.Finalizada, EsFisicoQuimico: false),
            new(Guid.NewGuid(), ActivityStatus.Fallida, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.EnProceso, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        var result = sut.Derive(workPlan, StatusChangeTrigger.CambioActividad);

        result.NuevoEstado.Should().Be(WorkPlanStatus.Finalizada);
        result.ReglaAplicada.Should().Be("CR07");
    }

    [Fact]
    public void CR07_PrimeraActividadFallida_CuentaComoReportada_DebeQuedarEnProceso()
    {
        // Confirma que el guard existente de CR03 (Any(a => a.Estado != Creada))
        // ya cubre el caso "la primera en salir de Creada es Fallida" sin
        // necesidad de código nuevo.
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.Fallida, EsFisicoQuimico: false),
            new(Guid.NewGuid(), ActivityStatus.Creada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.Asignada, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        var result = sut.Derive(workPlan, StatusChangeTrigger.CambioActividad);

        result.NuevoEstado.Should().Be(WorkPlanStatus.EnProceso);
        result.ReglaAplicada.Should().Be("CR03");
    }

    [Fact]
    public void CR08_UltimaActividadCanceladaIndividual_CuentaComoReportada_DebeQuedarFinalizada()
    {
        // Una Cancelada individual (mezclada con otras que sí terminaron) cuenta
        // para el cierre — distinto del caso CR09 (TODAS Canceladas), que no
        // debería entrar por este guard.
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.Finalizada, EsFisicoQuimico: false),
            new(Guid.NewGuid(), ActivityStatus.Cancelada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.EnProceso, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        var result = sut.Derive(workPlan, StatusChangeTrigger.CambioActividad);

        result.NuevoEstado.Should().Be(WorkPlanStatus.Finalizada);
        result.ReglaAplicada.Should().Be("CR08");
    }

    [Fact]
    public void CR08_PrimeraActividadCancelada_CuentaComoReportada_DebeQuedarEnProceso()
    {
        // Confirma que el guard existente de CR03 ya cubre "la primera en
        // salir de Creada es Cancelada" sin necesidad de código nuevo.
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.Cancelada, EsFisicoQuimico: false),
            new(Guid.NewGuid(), ActivityStatus.Creada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.Asignada, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        var result = sut.Derive(workPlan, StatusChangeTrigger.CambioActividad);

        result.NuevoEstado.Should().Be(WorkPlanStatus.EnProceso);
        result.ReglaAplicada.Should().Be("CR03");
    }

    [Fact]
    public void CR09_TodasLasActividadesCanceladas_DebeQuedarCanceladaAutomaticamente()
    {
        // 100% Canceladas, ninguna Finalizada/Fallida → cancelación automática,
        // distinta de CR08 (que exige al menos una Finalizada/Fallida).
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.Cancelada, EsFisicoQuimico: false),
            new(Guid.NewGuid(), ActivityStatus.Cancelada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.EnProceso, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        var result = sut.Derive(workPlan, StatusChangeTrigger.CambioActividad);

        result.NuevoEstado.Should().Be(WorkPlanStatus.Cancelada);
        result.ReglaAplicada.Should().Be("CR09");
        result.Motivo.Should().Be("cancelación automática por cierre de todas las actividades");
    }

    [Fact]
    public void CR10_ActividadFisicoQuimicaEnEstadoIntermedio_DebeQuedarEnProceso()
    {
        // Una actividad Físico/Química en un estado intermedio del flujo de
        // muestras mantiene la orden en proceso — atribuido a CR10, no al
        // guard genérico de CR03, por trazabilidad.
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.PendienteEnvioMuestras, EsFisicoQuimico: true),
            new(Guid.NewGuid(), ActivityStatus.Creada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.Asignada, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        var result = sut.Derive(workPlan, StatusChangeTrigger.CambioActividad);

        result.NuevoEstado.Should().Be(WorkPlanStatus.EnProceso);
        result.ReglaAplicada.Should().Be("CR10");
    }

    [Fact]
    public void CR10_ActividadFisicoQuimicaLlegaAEstadoFinal_AplicaMismoConteoDeCR04()
    {
        // Confirma que al llegar a un estado final, la actividad FQ se cuenta
        // igual que cualquier otra para el cierre (CR04) — sin código nuevo.
        var actividades = new List<Activity>
        {
            new(Guid.NewGuid(), ActivityStatus.Finalizada, EsFisicoQuimico: true),
            new(Guid.NewGuid(), ActivityStatus.Finalizada, EsFisicoQuimico: false)
        };
        var workPlan = new WorkPlan(Guid.NewGuid(), WorkPlanStatus.EnProceso, TecnicoId: Guid.NewGuid(), Actividades: actividades);
        var sut = new WorkPlanStatusDerivator();

        var result = sut.Derive(workPlan, StatusChangeTrigger.CambioActividad);

        result.NuevoEstado.Should().Be(WorkPlanStatus.Finalizada);
        result.ReglaAplicada.Should().Be("CR04");
    }
}
