using BFASenado.DTO.ResponseDTO;
using BFASenado.DTO.StampDTO;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System.Numerics;
using System.Security.Cryptography;

namespace BFASenado.Services.BFA
{
    public class BFAService : IBFAService
    {
        #region Attributes

        // Configuration
        private readonly IConfiguration _configuration;

        // Propiedades de appsettings
        private static string? UrlNodoPrueba;
        private static int ChainID;
        private static string? Sellador;
        private static string? PrivateKey;
        private static string? ContractAddress;
        private static string? ABI;

        #endregion

        #region Constructor

        public BFAService(
            IConfiguration configuration)
        {
            _configuration = configuration;
            UrlNodoPrueba = _configuration.GetSection("UrlNodoPrueba").Value;
            ChainID = Convert.ToInt32(_configuration.GetSection("ChainID")?.Value);
            Sellador = _configuration.GetSection("Sellador").Value;
            PrivateKey = _configuration.GetSection("PrivateKey").Value;
            ContractAddress = _configuration.GetSection("ContractAddress").Value;
            ABI = _configuration.GetSection("ABI").Value;
        }

        #endregion

        #region Methods

        public async Task<GetHashDTO?> GetHashDTO(string hash)
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

        public string CalcularHashSHA256(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(fileBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        #endregion
    }
}
