using BFASenado.Services;
using System.Text;

namespace BFASenado.Middleware
{
    public class NodeValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<NodeValidationMiddleware> _logger;
        private readonly ILogService _logService;
        private readonly string _urlNodoPrueba;

        public NodeValidationMiddleware(
            RequestDelegate next,
            ILogger<NodeValidationMiddleware> logger,
            ILogService logService,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _logService = logService;
            _urlNodoPrueba = configuration["UrlNodoBFA"];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!await IsNodeAvailable() || !await IsNodeSynced())
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync(Constantes.Constants.LogMessages.ServicioDisponibleError);
                return;
            }

            await _next(context);
        }

        private async Task<bool> IsNodeAvailable()
        {
            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync(_urlNodoPrueba);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                var log = _logService.CrearLog(
                    null,
                    ex.StackTrace,
                    Constantes.Constants.LogMessages.NodoDisponibleError,
                    ex.Message);
                _logger.LogError("{@Log}", log);
                return false;
            }
        }

        private async Task<bool> IsNodeSynced()
        {
            using var httpClient = new HttpClient();
            var requestContent = new
            {
                jsonrpc = "2.0",
                method = "eth_syncing",
                @params = new object[] { },
                id = 1
            };

            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestContent),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await httpClient.PostAsync(_urlNodoPrueba, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonResponse = System.Text.Json.JsonDocument.Parse(responseBody);

                    if (jsonResponse.RootElement.TryGetProperty("result", out var result))
                    {
                        return result.ValueKind == System.Text.Json.JsonValueKind.False;
                    }
                }
            }
            catch (Exception ex)
            {
                var log = _logService.CrearLog(
                    null,
                    ex.StackTrace,
                    Constantes.Constants.LogMessages.NodoSincronizadoError,
                    ex.Message);
                _logger.LogError("{@Log}", log);
            }

            return false;
        }
    }

}
