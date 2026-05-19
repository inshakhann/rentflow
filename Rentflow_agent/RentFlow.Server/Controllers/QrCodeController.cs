using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using RentFlow.Server.Data;

namespace RentFlow.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class QrCodeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QrCodeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("tenant")]
        public async Task<IActionResult> GetTenantQrCode()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // Generate a payment link URL (use app base URL + tenant payment path)
            var paymentUrl = $"https://rentflow.app/tenant/payments?uid={userId}";

            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(paymentUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var pngBytes = qrCode.GetGraphic(8);
            var base64 = Convert.ToBase64String(pngBytes);

            return Ok(new { qrBase64 = base64, paymentUrl = paymentUrl });
        }
    }
}
