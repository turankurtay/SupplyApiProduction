using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SupplyApi.Services;

namespace SupplyApi.Controllers
{
    [Route("api/balance")]
    [ApiController]
    public class BalanceController : ControllerBase
    {
        private readonly IEthBalanceService _ethBalanceService;
        private readonly ISolBalanceService _solBalanceService;
        private readonly IBscBalanceService _bscBalanceService;
        private readonly string _blockchainType;

        public BalanceController(
            IEthBalanceService ethBalanceService,
            ISolBalanceService solBalanceService,
            IBscBalanceService bscBalanceService,
            IConfiguration configuration)
        {
            _ethBalanceService = ethBalanceService;
            _solBalanceService = solBalanceService;
            _bscBalanceService = bscBalanceService;
            _blockchainType = configuration["BlockchainSettings:Type"];
        }

        [HttpGet("maxsupply")]
        public ActionResult<string> GetMaxSupply()
        {
            return _blockchainType switch
            {
                "Ethereum" => _ethBalanceService.GetMaxSupply(),
                "Solana" => _solBalanceService.GetMaxSupply(),
                "BSC" => _bscBalanceService.GetMaxSupply(),
                _ => BadRequest("Invalid blockchain type specified.")
            };
        }

        [HttpGet("totalsupply")]
        public async Task<IActionResult> GetTotalSupply()
        {
            return _blockchainType switch
            {
                "Ethereum" => Ok(await _ethBalanceService.GetTotalSupply()),
                "Solana" => Ok(await _solBalanceService.GetTotalSupply()),
                "BSC" => Ok(_bscBalanceService.GetTotalSupply()),
                _ => BadRequest("Invalid blockchain type specified.")
            };
        }

        [HttpGet("circulationsupply")]
        public async Task<IActionResult> GetCirculationSupply()
        {
            return _blockchainType switch
            {
                "Ethereum" => Ok(await _ethBalanceService.GetCirculationSupply()),
                "Solana" => Ok(await _solBalanceService.GetCirculationSupply()),
                "BSC" => Ok(_bscBalanceService.GetCirculationSupply()),
                _ => BadRequest("Invalid blockchain type specified.")
            };
        }
    }
}
