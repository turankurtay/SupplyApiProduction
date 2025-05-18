namespace SupplyApi.Model
{
    public class WalletSettings
    {
        public string ContractAddress { get; set; }
        public string ExcludeAddresses { get; set; }
        public string BurnAddresses { get; set; }
        public string MaxSupply { get; set; }
        public string BscScanApiKey { get; set; }
        public string BscScanApiUrl { get; set; }

    }
}
