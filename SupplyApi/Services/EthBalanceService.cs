using Microsoft.AspNetCore.Mvc;
using Nethereum.Web3;
using System.Numerics;
using Microsoft.Extensions.Options;
using SupplyApi.Model;

namespace SupplyApi.Services
{
    public interface IEthBalanceService
    {
        Task<string?> GetCirculationSupply();
        Task<string?> GetTotalSupply();
        ActionResult<string> GetMaxSupply();
    }

    public class EthBalanceService : IEthBalanceService
    {
        private readonly Web3 _web3;
        private readonly HttpClient _httpClient;
        private readonly WalletSettings _walletSettings;

        public EthBalanceService(IOptions<WalletSettings> options, HttpClient httpClient, Web3 web3)
        {
            _walletSettings = options.Value;
            _httpClient = httpClient;
            _web3 = web3;
        }

        #region Public
        public ActionResult<string> GetMaxSupply()
        {
            return _walletSettings.MaxSupply;
        }

        public async Task<string?> GetTotalSupply()
        {
            try
            {
                var contract = _web3.Eth.GetContract(Constants.ERC20ABI, _walletSettings.ContractAddress);
                var totalSupplyFunction = contract.GetFunction("totalSupply");
                var totalSupply = await totalSupplyFunction.CallAsync<BigInteger>();

                BigInteger burnedTotal = 0;
                if (!string.IsNullOrEmpty(_walletSettings.BurnAddresses))
                {
                    var burnAddressesArray = _walletSettings.BurnAddresses.Split(',');
                    foreach (var address in burnAddressesArray)
                    {
                        var balanceFunction = contract.GetFunction("balanceOf");
                        var balance = await balanceFunction.CallAsync<BigInteger>(address);
                        burnedTotal += balance;
                    }
                }

                var adjustedTotalSupplyEth = Web3.Convert.FromWei(totalSupply - burnedTotal);

                return adjustedTotalSupplyEth.ToString();
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
                var contract = _web3.Eth.GetContract(Constants.ERC20ABI, _walletSettings.ContractAddress);
                var totalSupplyFunction = contract.GetFunction("totalSupply");
                var totalSupply = await totalSupplyFunction.CallAsync<BigInteger>();

                BigInteger excludedTotal = 0;
                if (!string.IsNullOrEmpty(_walletSettings.ExcludeAddresses))
                {
                    var excludeAddressesArray = _walletSettings.ExcludeAddresses.Split(',');
                    foreach (var address in excludeAddressesArray)
                    {
                        var balanceFunction = contract.GetFunction("balanceOf");
                        var balance = await balanceFunction.CallAsync<BigInteger>(address);
                        excludedTotal += balance;
                    }
                }

                BigInteger burnedTotal = 0;
                if (!string.IsNullOrEmpty(_walletSettings.BurnAddresses))
                {
                    var burnAddressesArray = _walletSettings.BurnAddresses.Split(',');
                    foreach (var address in burnAddressesArray)
                    {
                        var balanceFunction = contract.GetFunction("balanceOf");
                        var balance = await balanceFunction.CallAsync<BigInteger>(address);
                        burnedTotal += balance;
                    }
                }

                var adjustedTotalSupply = totalSupply - excludedTotal - burnedTotal;
                var adjustedTotalSupplyEth = Web3.Convert.FromWei(adjustedTotalSupply);

                return adjustedTotalSupplyEth.ToString();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return "Internal Server Error";
            }
        }
        #endregion

        #region Private
        private static class Constants
        {
            public const string ERC20ABI = @"[
            {
                'constant': true,
                'inputs': [],
                'name': 'totalSupply',
                'outputs': [{'name': '', 'type': 'uint256'}],
                'stateMutability': 'view',
                'type': 'function'
            },
            {
                'constant': true,
                'inputs': [{'name': 'owner', 'type': 'address'}],
                'name': 'balanceOf',
                'outputs': [{'name': '', 'type': 'uint256'}],
                'stateMutability': 'view',
                'type': 'function'
            }
        ]";
        }
        #endregion

    }
}