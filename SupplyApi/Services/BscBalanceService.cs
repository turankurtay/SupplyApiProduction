using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nethereum.Web3;
using SupplyApi.Model;
using System.Numerics;
using System.Text.Json;

namespace SupplyApi.Services
{
    public interface IBscBalanceService
    {
        string GetCirculationSupply();
        string GetTotalSupply();
        string GetMaxSupply();
    }

    public class BscBalanceService : IBscBalanceService
    {
        private readonly Web3 _web3;
        private readonly HttpClient _httpClient;
        private readonly WalletSettings _walletSettings;

        public BscBalanceService(IOptions<WalletSettings> options, HttpClient httpClient, Web3 web3)
        {
            _walletSettings = options.Value;
            _httpClient = httpClient;
            _web3 = web3;
        }

        public string GetMaxSupply()
        {
            return _walletSettings.MaxSupply ?? "0";
        }

        public string GetTotalSupply()
        {
            try
            {
                return CalculationTotalSupply().ToString();
            }
            catch (Exception ex)
            {
                return $"Internal Server Error {ex.Message}";
            }
        }

        public string GetCirculationSupply()
        {
            try
            {
                decimal maxTotalSupply = Convert.ToDecimal(_walletSettings.MaxSupply);

                var burnAddressesArray = _walletSettings.BurnAddresses.Split(',');

                var excludeAddressesArray = _walletSettings.ExcludeAddresses.Split(',');

                var combinedAddresses = burnAddressesArray.Concat(excludeAddressesArray).ToArray();

                decimal excludedTotalBalance = GetTotalBalance(combinedAddresses.ToList());

                decimal circulationSupply = maxTotalSupply - excludedTotalBalance;

                return circulationSupply.ToString();
            }
            catch (Exception ex)
            {
                return $"Internal Server Error {ex.Message}";
            }
        }

        //Private
        private decimal GetTotalBalance(List<string> addresses)
        {
            BigInteger totalBalanceRaw = BigInteger.Zero;

            foreach (var address in addresses)
            {
                var balanceUrl = $"{_walletSettings.BscScanApiUrl}?module=account&action=tokenbalance" +
                                 $"&contractaddress={_walletSettings.ContractAddress}" +
                                 $"&address={address}" +
                                 $"&tag=latest&apikey={_walletSettings.BscScanApiKey}";

                var response = _httpClient.GetStringAsync(balanceUrl).GetAwaiter().GetResult();
                var balanceData = JsonSerializer.Deserialize<BscScanResponse>(response);

                if (balanceData?.status == "1" && balanceData.result != null)
                {
                    var balance = BigInteger.Parse(balanceData.result);
                    totalBalanceRaw += balance;
                }
            }

            int decimals = 9;

            return (decimal)totalBalanceRaw / (decimal)BigInteger.Pow(10, decimals);
        }

        private decimal CalculationTotalSupply()
        {
            var supplyUrl = $"{_walletSettings.BscScanApiUrl}?module=stats&action=tokensupply" +
                            $"&contractaddress={_walletSettings.ContractAddress}" +
                            $"&apikey={_walletSettings.BscScanApiKey}";

            var response = _httpClient.GetStringAsync(supplyUrl).GetAwaiter().GetResult();
            var supplyData = JsonSerializer.Deserialize<BscScanResponse>(response);

            if (supplyData?.status != "1" || supplyData.result == null)
                throw new Exception("Error fetching total supply");

            var totalSupply = BigInteger.Parse(supplyData.result);
            int decimals = 9;

            return (decimal)totalSupply / (decimal)BigInteger.Pow(10, decimals);
        }

    }
}
