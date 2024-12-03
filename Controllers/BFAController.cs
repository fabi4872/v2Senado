using Microsoft.AspNetCore.Mvc;
using Nethereum.Web3.Accounts;
using System.Numerics;
using Nethereum.Web3;
using Nethereum.Hex.HexConvertors.Extensions;
using BFASenado.Models;
using BFASenado.DTO.RequestDTO;
using BFASenado.Services;
using System.Security.Cryptography;
using System.Text;
using BFASenado.Services.Repository;
using BFASenado.DTO.ResponseDTO;
using BFASenado.DTO.StampDTO;

namespace BFASenado.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BFAController : ControllerBase
    {
        #region Attributes

        // DB
        private readonly BFAContext _context;

        // Logger
        private readonly ILogger<BFAController> _logger;
        private readonly ILogService _logService;

        // Configuration
        private readonly IConfiguration _configuration;

        // TransaccionBFAService
        private readonly ITransaccionBFAService _transaccionBFAService;

        // MessageService
        private readonly IMessageService _messageService;

        // Propiedades de appsettings
        private static string? UrlNodoPrueba;
        private static int ChainID;
        private static string? Sellador;
        private static string? PrivateKey;
        private static string? ContractAddress;
        private static string? ABI;

        #endregion

        #region Constructor

        public BFAController(
            ILogService logService,
            ILogger<BFAController> logger, 
            BFAContext context, 
            IConfiguration configuration,
            ITransaccionBFAService transaccionBFAService,
            IMessageService messageService)
        {
            _logService = logService;
            _logger = logger;
            _context = context;
            _configuration = configuration;
            _transaccionBFAService = transaccionBFAService;
            _messageService = messageService;

            UrlNodoPrueba = _configuration.GetSection("UrlNodoPrueba").Value;
            ChainID = Convert.ToInt32(_configuration.GetSection("ChainID")?.Value);
            Sellador = _configuration.GetSection("Sellador").Value;
            PrivateKey = _configuration.GetSection("PrivateKey").Value;
            ContractAddress = _configuration.GetSection("ContractAddress").Value;
            ABI = _configuration.GetSection("ABI").Value;
        }

        #endregion

        #region Methods

        [HttpGet("Balance")]
        public async Task<ActionResult<decimal>> Balance()
        {
            try
            {
                //bool nodoDisponible = await this.IsNodeAvailable();
                //bool nodoSincronizado = await this.IsNodeSynced();

                //if (!nodoDisponible)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetDisponibilidadBFAError()}");
                //}
                //if (!nodoSincronizado)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetSincronizacionBFAError()}");
                //}

                var web3 = new Web3(UrlNodoPrueba);
                var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(Sellador);
                var balanceEther = Web3.Convert.FromWei(balanceWei);

                var logSuccess = _logService.CrearLog(
                    HttpContext, 
                    Sellador, 
                    Constantes.Constants.LogMessages.GetBalanceSuccess, 
                    null);
                _logger.LogInformation("{@Log}", logSuccess);

                // Retornar el balance
                return Ok(balanceEther);
            }
            catch (Exception ex)
            {
                var logError = _logService.CrearLog(
                    HttpContext, 
                    Sellador, 
                    Constantes.Constants.LogMessages.GetBalanceError, 
                    ex.Message);
                _logger.LogError("{@Log}", logError);
                
                throw new Exception($"{Constantes.Constants.LogMessages.GetBalanceError}. {ex.Message}");
            }
        }

        [HttpPost("ArchivoData")]
        public async Task<ActionResult<GetFileDTO?>> ArchivoData(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(Constantes.Constants.LogMessages.GetPropiedadesArchivoError);
            }

            try
            {
                //bool nodoDisponible = await this.IsNodeAvailable();
                //bool nodoSincronizado = await this.IsNodeSynced();

                //if (!nodoDisponible)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetDisponibilidadBFAError()}");
                //}
                //if (!nodoSincronizado)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetSincronizacionBFAError()}");
                //}

                using (var memoryStream = new MemoryStream())
                {
                    // Leer archivo
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    // Calcular Hash
                    string hash = CalcularHashSHA256(fileBytes);

                    // Convertir a Base64
                    string base64 = Convert.ToBase64String(fileBytes);

                    var logSuccess = _logService.CrearLog(
                        HttpContext,
                        new { file?.FileName, file?.Length },
                        Constantes.Constants.LogMessages.GetPropiedadesArchivoSuccess,
                        null);
                    _logger.LogInformation("{@Log}", logSuccess);

                    // Retornar
                    return Ok(new GetFileDTO()
                    {
                        HashSHA256 = hash,
                        Base64 = base64
                    });
                }
            }
            catch (Exception ex)
            {
                var logError = _logService.CrearLog(
                    HttpContext,
                    new { file?.FileName, file?.Length },
                    Constantes.Constants.LogMessages.GetPropiedadesArchivoError,
                    ex.Message);
                _logger.LogError("{@Log}", logError);

                throw new Exception($"{Constantes.Constants.LogMessages.GetPropiedadesArchivoError}. {ex.Message}");
            }
        }

        [HttpPost("SHA256ByBase64")]
        public ActionResult<string> SHA256ByBase64([FromBody] Base64InputDTO base64Input)
        {
            try
            {
                //bool nodoDisponible = await this.IsNodeAvailable();
                //bool nodoSincronizado = await this.IsNodeSynced();

                //if (!nodoDisponible)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetDisponibilidadBFAError()}");
                //}
                //if (!nodoSincronizado)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetSincronizacionBFAError()}");
                //}

                // Convertir Base64 a arreglo de bytes
                byte[] fileBytes = Convert.FromBase64String(base64Input.Base64);

                // Calcular el hash SHA-256
                string hash = CalcularHashSHA256(fileBytes);

                var logSuccess = _logService.CrearLog(
                    HttpContext,
                    null,
                    Constantes.Constants.LogMessages.GetHashSHA256Success,
                    null);
                _logger.LogInformation("{@Log}", logSuccess);

                return Ok(hash);
            }
            catch (Exception ex)
            {
                var logError = _logService.CrearLog(
                    HttpContext,
                    null,
                    Constantes.Constants.LogMessages.GetHashSHA256Error,
                    ex.Message);
                _logger.LogError("{@Log}", logError);

                throw new Exception($"{Constantes.Constants.LogMessages.GetHashSHA256Error}. {ex.Message}");
            }
        }

        [HttpGet("GetHashBaseDatos")]
        public async Task<ActionResult<TransaccionBFA?>> GetHashBaseDatos([FromQuery] string hash)
        {
            if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrEmpty(hash))
            {
                return BadRequest(Constantes.Constants.DataAnnotationsErrorMessages.FormatoIncorrecto);
            }

            try
            {
                //bool nodoDisponible = await this.IsNodeAvailable();
                //bool nodoSincronizado = await this.IsNodeSynced();

                //if (!nodoDisponible)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetDisponibilidadBFAError()}");
                //}
                //if (!nodoSincronizado)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetSincronizacionBFAError()}");
                //}

                if (hash.StartsWith("0x"))
                    hash = hash.Substring(2);
                hash = hash.ToLower();
                TransaccionBFA? transaccion = await _transaccionBFAService.GetByHash(hash);

                if (transaccion != null)
                {
                    var logSuccess = _logService.CrearLog(
                        HttpContext,
                        hash,
                        Constantes.Constants.LogMessages.GetHashSuccess,
                        null);
                    _logger.LogInformation("{@Log}", logSuccess);

                    return Ok(transaccion);
                }

                var logWarning = _logService.CrearLog(
                        HttpContext,
                        hash,
                        Constantes.Constants.LogMessages.GetHashNoExisteError,
                        null
                    );
                _logger.LogWarning("{@Log}", logWarning);

                return BadRequest(Constantes.Constants.LogMessages.GetHashNoExisteError);
            }
            catch (Exception ex)
            {
                var logError = _logService.CrearLog(
                    HttpContext,
                    hash,
                    Constantes.Constants.LogMessages.GetHashError,
                    ex.Message);
                _logger.LogError("{@Log}", logError);

                return StatusCode(500, $"{Constantes.Constants.LogMessages.GetHashError}. {ex.Message}");
            }
        }

        [HttpGet("GetHashBFA")]
        public async Task<ActionResult<GetHashDTO>> GetHashBFA([FromQuery] string hash)
        {
            if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrEmpty(hash))
            {
                return BadRequest(Constantes.Constants.DataAnnotationsErrorMessages.FormatoIncorrecto);
            }

            try
            {
                //bool nodoDisponible = await this.IsNodeAvailable();
                //bool nodoSincronizado = await this.IsNodeSynced();

                //if (!nodoDisponible)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetDisponibilidadBFAError()}");
                //}
                //if (!nodoSincronizado)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetSincronizacionBFAError()}");
                //}

                var result = await this.GetHashDTO(hash);

                if (result == null)
                {
                    var logWarning = _logService.CrearLog(
                       HttpContext,
                       hash,
                       Constantes.Constants.LogMessages.GetHashNoExisteError,
                       null
                   );
                    _logger.LogWarning("{@Log}", logWarning);

                    return BadRequest(Constantes.Constants.LogMessages.GetHashNoExisteError);
                }

                var logSuccess = _logService.CrearLog(
                    HttpContext,
                    hash,
                    Constantes.Constants.LogMessages.GetHashSuccess,
                    null
                );
                _logger.LogInformation("{@Log}", logSuccess);

                return Ok(result);
            }
            catch (Exception ex)
            {
                var logError = _logService.CrearLog(
                    HttpContext,
                    hash,
                    Constantes.Constants.LogMessages.GetHashError,
                    ex.Message
                );
                _logger.LogError("{@Log}", logError);

                return StatusCode(500, $"{Constantes.Constants.LogMessages.GetHashError}. {ex.Message}");
            }
        }

        [HttpGet("GetHashesAll")]
        public async Task<ActionResult<List<GetHashDTO>>> GetHashesAll()
        {
            try
            {
                //bool nodoDisponible = await this.IsNodeAvailable();
                //bool nodoSincronizado = await this.IsNodeSynced();

                //if (!nodoDisponible)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetDisponibilidadBFAError()}");
                //}
                //if (!nodoSincronizado)
                //{
                //    throw new InvalidOperationException($"{_messageService.GetSincronizacionBFAError()}");
                //}

                var account = new Account(PrivateKey, ChainID);
                var web3 = new Web3(account, UrlNodoPrueba);
                List<GetHashDTO> hashes = new List<GetHashDTO>();

                // Activar transacciones de tipo legacy
                web3.TransactionManager.UseLegacyAsDefault = true;

                // Cargar el contrato en la dirección especificada
                var contract = web3.Eth.GetContract(ABI, ContractAddress);

                // Llamar a la función "getAllHashes" del contrato
                var getAllHashesFunction = contract.GetFunction("getAllHashes");
                var hashesList = await getAllHashesFunction.CallAsync<List<BigInteger>>();

                // Convertir cada BigInteger en una cadena hexadecimal
                var hashStrings = hashesList?
                    .Select(h => "0x" + h.ToString("X").ToLower())
                    .ToList();

                // Insertar hashStrings en lista de hashes
                foreach (var h in hashStrings)
                {
                    var hashDTO = await this.GetHashDTO(h);
                    if (hashDTO != null)
                    {
                        hashes.Add(hashDTO);
                    }
                }

                var logSuccess = _logService.CrearLog(
                    HttpContext,
                    null,
                    Constantes.Constants.LogMessages.GetHashesSinFiltroSuccess,
                    null);
                _logger.LogInformation("{@Log}", logSuccess);

                return Ok(hashes);
            }
            catch (Exception ex)
            {
                var logError = _logService.CrearLog(
                    HttpContext,
                    null,
                    Constantes.Constants.LogMessages.GetHashesSinFiltroError,
                    ex.Message);
                _logger.LogError("{@Log}", logError);

                return StatusCode(500, $"{Constantes.Constants.LogMessages.GetHashesSinFiltroError}. {ex.Message}");
            }
        }

        [HttpPost("SaveHash")]
        public async Task<ActionResult<GetHashDTO?>> SaveHash([FromBody] GuardarHashDTO input)
        {
            try
            {
                var account = new Account(PrivateKey);
                var web3 = new Web3(account, UrlNodoPrueba);
                web3.TransactionManager.UseLegacyAsDefault = true;
                var contract = web3.Eth.GetContract(ABI, ContractAddress);
                var putFunction = contract.GetFunction("put");
                var result = await this.GetHashDTO(input.HashSHA256);
                BigInteger hashValue = input.HashSHA256.HexToBigInteger(false);
                string hashHex = input.HashSHA256;

                if (!hashHex.StartsWith("0x"))
                    hashHex = "0x" + hashHex;
                hashHex = hashHex.ToLower();

                // Verificar si el hash ya existe en la DB o en la BFA
                var transaccionDB = await _transaccionBFAService.GetByHash(input.HashSHA256);
                
                if (result != null || transaccionDB != null)
                {
                    var logWarning = _logService.CrearLog(
                        HttpContext,
                        input.HashSHA256,
                        Constantes.Constants.LogMessages.HashDuplicadoError,
                        null
                    );
                    _logger.LogWarning("{@Log}", logWarning);

                    return BadRequest(Constantes.Constants.LogMessages.HashDuplicadoError);
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Guardar en la base de datos
                        await _transaccionBFAService.Save(new TransaccionBFA
                        {
                            Base64 = input.Base64,
                            Detalles = input.Detalles,
                            FechaAltaBFA = null,
                            FechaAltaTabla = DateTime.Now,
                            HashSHA256 = input.HashSHA256,
                            HashHexa = hashHex,
                            IdTabla = input.IdTabla,
                            NombreTabla = input.NombreTabla,
                            SnAltaBFA = false,
                            TipoDocumento = input.TipoDocumento
                        });

                        // Guardar en la BFA
                        var objectList = new List<BigInteger> { hashValue };
                        var transactionHash = await putFunction.SendTransactionAsync(
                            account.Address,
                            new Nethereum.Hex.HexTypes.HexBigInteger(300000),
                            null,
                            objectList,
                            input.IdTabla,
                            input.NombreTabla,
                            input.Detalles, // Nueva propiedad
                            input.TipoDocumento // Nueva propiedad
                        );

                        if (string.IsNullOrEmpty(transactionHash))
                        {
                            throw new Exception(Constantes.Constants.LogMessages.HashGuardarError);
                        }

                        await transaction.CommitAsync();

                        var logSuccess = _logService.CrearLog(
                            HttpContext,
                            input.HashSHA256,
                            Constantes.Constants.LogMessages.TransaccionGuardarSuccess,
                            null
                        );
                        _logger.LogInformation("{@Log}", logSuccess);

                        // Actualizar registro en base de datos
                        var resultRecuperado = await this.GetHashDTO(input.HashSHA256);
                        TransaccionBFA? recuperado = await _transaccionBFAService.GetByHash(input.HashSHA256);
                        if (resultRecuperado != null && recuperado != null)
                        {
                            recuperado.FechaAltaBFA = resultRecuperado.FechaAlta;
                            recuperado.SnAltaBFA = true;
                            await _transaccionBFAService.Update(recuperado);
                        }

                        // Retornar
                        return Ok(resultRecuperado);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        var logError = _logService.CrearLog(
                            HttpContext,
                            input.HashSHA256,
                            Constantes.Constants.LogMessages.TransaccionGuardarError,
                            ex.Message
                        );
                        _logger.LogError("{@Log}", logError);

                        throw new Exception($"{Constantes.Constants.LogMessages.TransaccionGuardarError}. {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                var log = _logService.CrearLog(
                    HttpContext,
                    input.HashSHA256,
                    Constantes.Constants.LogMessages.HashGuardarError,
                    ex.Message
                );
                _logger.LogError("{@Log}", log);

                return StatusCode(500, $"{Constantes.Constants.LogMessages.HashGuardarError}. {ex.Message}");
            }
        }



        // Métodos privados
        private async Task<GetHashDTO?> GetHashDTO(string hash)
        {
            if (!hash.StartsWith("0x"))
                hash = "0x" + hash;
            hash = hash.ToLower();

            BigInteger hashValue = hash.HexToBigInteger(false);

            var account = new Account(PrivateKey, ChainID);
            var web3 = new Web3(account, UrlNodoPrueba);
            web3.TransactionManager.UseLegacyAsDefault = true;

            var contract = web3.Eth.GetContract(ABI, ContractAddress);
            var getHashDataFunction = contract.GetFunction("getHashData");
            var result = await getHashDataFunction.CallDeserializingToObjectAsync<StampDTO>(hashValue);

            if (result.BlockNumbers == null || result.BlockNumbers.Count == 0)
            {
                return null;
            }

            BigInteger blockNumber = result.BlockNumbers[0];
            var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new Nethereum.Hex.HexTypes.HexBigInteger(blockNumber));

            DateTimeOffset timeStamp = DateTimeOffset.FromUnixTimeSeconds((long)block.Timestamp.Value);
            DateTime argentinaTime = timeStamp.ToOffset(TimeSpan.FromHours(-3)).DateTime;
            string hashRecuperado = result.Objects != null && result.Objects.Count > 0 ? result.Objects[0].ToString() : Constantes.Constants.DataMessages.NoRegistra;
            string signerAddress = result.Stampers != null && result.Stampers.Count > 0 ? result.Stampers[0] : Constantes.Constants.DataMessages.NoRegistra;

            return new GetHashDTO()
            {
                NumeroBloque = blockNumber.ToString(),
                FechaAlta = argentinaTime,
                Hash = hashRecuperado,
                IdTabla = result.IdTablas != null && result.IdTablas.Any() ? result.IdTablas[0].ToString() : Constantes.Constants.DataMessages.NoRegistra,
                NombreTabla = result.NombreTablas?.FirstOrDefault() ?? Constantes.Constants.DataMessages.NoRegistra,
                TipoDocumento = result.TipoDocumentos?.FirstOrDefault() ?? Constantes.Constants.DataMessages.NoRegistra,
                Detalles = result.Detalles?.FirstOrDefault() ?? Constantes.Constants.DataMessages.NoRegistra,
                Sellador = signerAddress,
                Base64 = null
            };
        }

        private string CalcularHashSHA256(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(fileBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private async Task<bool> IsNodeAvailable()
        {
            using var httpClient = new HttpClient();
            try
            {
                // Realizar una solicitud simple GET para verificar disponibilidad
                var response = await httpClient.GetAsync(UrlNodoPrueba);

                if (response.IsSuccessStatusCode)
                {
                    return true; // Nodo está disponible
                }
                else
                {
                    // Log error
                    var log = _logService.CrearLog(
                        HttpContext,
                        null,
                        $"{_messageService.GetDisponibilidadBFAError()}. Error: {response.StatusCode} - {response.ReasonPhrase}",
                        null);
                    _logger.LogInformation("{@Log}", log);
                }
            }
            catch (Exception ex)
            {
                // Log Error
                var log = _logService.CrearLog(
                    HttpContext,
                    ex.StackTrace,
                    $"{_messageService.GetDisponibilidadBFAError()}. {ex.Message}. {ex.StackTrace}",
                    null);
                _logger.LogInformation("{@Log}", log);
            }

            return false; // Nodo no está disponible
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

            // Serializar el objeto a JSON
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestContent),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                // Enviar la solicitud POST
                var response = await httpClient.PostAsync(UrlNodoPrueba, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    // Leer la respuesta JSON
                    var responseBody = await response.Content.ReadAsStringAsync();

                    // Deserializar la respuesta
                    var jsonResponse = System.Text.Json.JsonDocument.Parse(responseBody);

                    // Extraer el campo "result"
                    if (jsonResponse.RootElement.TryGetProperty("result", out var result))
                    {
                        // Si el resultado es "false", el nodo está sincronizado
                        return result.ValueKind == System.Text.Json.JsonValueKind.False;
                    }
                }
                else
                {
                    // Manejo de error si el servidor no responde correctamente
                    // Log Error
                    var log = _logService.CrearLog(
                    HttpContext,
                        null,
                        $"{_messageService.GetSincronizacionBFAError()}. Error: {response.StatusCode} - {response.ReasonPhrase}",
                        null);
                    _logger.LogInformation("{@Log}", log);
                }
            }
            catch (Exception ex)
            {
                // Log Error
                var log = _logService.CrearLog(
                HttpContext,
                ex.StackTrace,
                $"{_messageService.GetSincronizacionBFAError()}. {ex.Message}. {ex.StackTrace}",
                    null);
                _logger.LogInformation("{@Log}", log);
            }

            // Si no se puede determinar
            return false;
        }

        #endregion
    }
}
