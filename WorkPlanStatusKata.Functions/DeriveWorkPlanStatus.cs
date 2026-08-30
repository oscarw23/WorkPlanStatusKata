using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WorkPlanStatusKata.Domain;

namespace WorkPlanStatusKata.Functions;

public record DeriveRequest(WorkPlan WorkPlan, StatusChangeTrigger Trigger);

public class DeriveWorkPlanStatus
{
    private readonly ILogger<DeriveWorkPlanStatus> _logger;
    private readonly WorkPlanStatusDerivator _derivator;

    public DeriveWorkPlanStatus(ILogger<DeriveWorkPlanStatus> logger)
    {
        _logger = logger;
        _derivator = new WorkPlanStatusDerivator();
    }

    [Function("DeriveWorkPlanStatus")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        DeriveRequest? payload;
        try
        {
            payload = await req.ReadFromJsonAsync<DeriveRequest>();
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "Payload JSON invalido");
            return new BadRequestObjectResult(new { error = "JSON invalido", detail = ex.Message });
        }

        if (payload is null)
        {
            return new BadRequestObjectResult(new { error = "Cuerpo de la peticion vacio" });
        }

        try
        {
            var result = _derivator.Derive(payload.WorkPlan, payload.Trigger);
            return new OkObjectResult(result);
        }
        catch (System.NotImplementedException ex)
        {
            _logger.LogInformation("Caso no contemplado: {Message}", ex.Message);
            return new UnprocessableEntityObjectResult(new { error = "Caso no contemplado por las reglas de negocio", detail = ex.Message });
        }
    }
}