//using FineCodeCoverage.Core.Coverage;
//using FineCodeCoverage.Core.Model;
//using Microsoft.AspNetCore.Mvc;
//using System.Threading.Tasks;

//namespace FineCodeCoverage.Controllers
//{
//	[ApiController]
//	[Route("Coverage")]
//	public class CoverageController : ControllerBase
//	{
//		private readonly ICoverageService _coverageService;

//		public CoverageController(ICoverageService coverageService)
//		{
//			_coverageService = coverageService;
//		}

//		[HttpPost("Calculate")]
//		public async Task<CalculateCoverageResponse> CalculateAsync([FromBody] CalculateCoverageRequest request)
//		{
//			return await _coverageService.CalculateCoverageAsync(request);
//		}
//	}
//}
