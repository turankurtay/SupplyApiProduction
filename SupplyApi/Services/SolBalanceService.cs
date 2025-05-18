using Microsoft.AspNetCore.Mvc;
using Solnet.Rpc;
using Solnet.Wallet;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SupplyApi.Model;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System;

namespace SupplyApi.Services
{
    public interface ISolBalanceService
    {
        Task<string?> GetCirculationSupply();
        Task<string?> GetTotalSupply();
        ActionResult<string> GetMaxSupply();
    }

    public class SolBalanceService : ISolBalanceService
    {
        private readonly IRpcClient _client;
        private readonly WalletSettings _walletSettings;

        public SolBalanceService(IOptions<WalletSettings> options)
        {
            _walletSettings = options.Value;
            _client = ClientFactory.GetClient(Cluster.MainNet); // Solana ana ağı
        }

        public ActionResult<string> GetMaxSupply()
        {
            return _walletSettings.MaxSupply;
        }

        public async Task<string?> GetTotalSupply()
        {
            try
            {
                var supplyResponse = await _client.GetTokenSupplyAsync(new PublicKey(_walletSettings.ContractAddress));
                if (supplyResponse.WasSuccessful)
                {
                    // totalSupply'yi BigInteger'a dönüştür
                    var totalSupply = BigInteger.Parse(supplyResponse.Result.Value.Amount);

                    return FormatSolanaSupply(totalSupply);
                }

                return "Error retrieving Total Supply";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return "Internal Server Error";
            }
        }

        public async Task<string?> GetCirculationSupply()
        {
            try
            {
                var supplyResponse = await _client.GetTokenSupplyAsync(new PublicKey(_walletSettings.ContractAddress));
                if (!supplyResponse.WasSuccessful || supplyResponse.Result.Value.Amount == null)
                {
                    return "Error retrieving Total Supply";
                }

                // totalSupply'yi BigInteger'a dönüştür
                var totalSupply = BigInteger.Parse(supplyResponse.Result.Value.Amount);

                // Yakılan veya hariç tutulacak adreslerin bakiyesini düşelim
                BigInteger excludedTotal = 0;
                if (!string.IsNullOrEmpty(_walletSettings.ExcludeAddresses))
                {
                    var excludeAddressesArray = _walletSettings.ExcludeAddresses.Split(',');
                    foreach (var address in excludeAddressesArray)
                    {
                        var balanceResponse = await _client.GetTokenAccountBalanceAsync(new PublicKey(address));
                        if (balanceResponse.WasSuccessful && balanceResponse.Result.Value.Amount != null)
                        {
                            excludedTotal += BigInteger.Parse(balanceResponse.Result.Value.Amount);
                        }
                    }
                }

                BigInteger burnedTotal = 0;
                if (!string.IsNullOrEmpty(_walletSettings.BurnAddresses))
                {
                    var burnAddressesArray = _walletSettings.BurnAddresses.Split(',');
                    foreach (var address in burnAddressesArray)
                    {
                        var balanceResponse = await _client.GetTokenAccountBalanceAsync(new PublicKey(address));
                        if (balanceResponse.WasSuccessful && balanceResponse.Result.Value.Amount != null)
                        {
                            burnedTotal += BigInteger.Parse(balanceResponse.Result.Value.Amount);
                        }
                    }
                }

                // Dolaşımdaki arzı hesapla
                var adjustedCirculationSupply = totalSupply - excludedTotal - burnedTotal;

                return FormatSolanaSupply(adjustedCirculationSupply);

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return "Internal Server Error";
            }
        }

        private static string FormatSolanaSupply(BigInteger rawAmount)
        {
            // rawAmount: Solnet kütüphanesinden gelen büyük sayı (ör. 9990332573412983)
            // decimals: Token'ın ondalık basamak sayısı (ör. 9)

            // Büyük sayıyı ondalık formata çevir
            var formattedValue = (decimal)rawAmount / (decimal)Math.Pow(10, 8);

            // Ondalık ayracı '.' olacak şekilde formatla, binlik ayraç olmadan
            return formattedValue.ToString();
        }

    }
}
