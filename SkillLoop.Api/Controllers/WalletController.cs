using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;
using SkillLoop.Api.Services;
using System.Security.Claims;

namespace SkillLoop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {

        private readonly WalletService _walletService;
        private readonly IWebHostEnvironment _environment;

        public WalletController( WalletService walletService,
            IWebHostEnvironment environment)
        {
            _walletService = walletService;
            _environment = environment;
        }


        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var balance = await _walletService.GetBalanceAsync(userId);

            return Ok(new
            {
                Balance = balance,
                EquivalentInLE = balance * 10
            });
        }


        [HttpPost("buy-credits")]
        public async Task<IActionResult> BuyCredits(
            [FromBody] BuyCreditsRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!_environment.IsDevelopment())
                return StatusCode(403, new
                {
                    error = "Simulated purchases are disabled",
                    code = "PURCHASE_DISABLED"
                });

            var balance = await _walletService
                .BuyCreditsAsync(userId, request.Credits);

            return Ok(new
            {
                Balance = balance,
                EquivalentInLE = balance * 10L
            });
        }



        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var transactions = await _walletService.GetTransactionsAsync(userId);

            return Ok(transactions);
        }
        [HttpPost("promo-code")]
        public async Task<IActionResult> ApplyPromoCode( ApplyPromoCodeRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var balance = await _walletService
                .ApplyPromoCodeAsync(userId, request.Code);

            if (balance == null)
                return BadRequest(new
                {
                    error = "Invalid, expired or already used promo code",
                    code = "INVALID_PROMO_CODE"
                });

            return Ok(new
            {
                Balance = balance.Value,
                EquivalentInLE = balance.Value * 10L
            });
        }
        }
}
