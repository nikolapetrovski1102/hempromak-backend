using Backend_hempromak.Models;
using Backend_hempromak.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Backend_hempromak.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {

        private readonly DbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly DataService _dataService;

        public DataController (IConfiguration configuration)
        {
            _dbContext = new DbContext ();
            _configuration = configuration;
            _dataService = new DataService(_dbContext, configuration);
        }

        [HttpGet("/")]
        public IActionResult getFromTable()
        {
            try
            {
                return Ok(_dataService.getAll());

            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("/postTransaction")]
        public async Task<IActionResult> postTransaction([FromBody] TransferModel transferModel)
        {
            try
            {
                await _dataService.postTransactionAsync(transferModel);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("/getTransaction")]
        public async Task<IActionResult> getTransaction()
        {
            try
            {
                return Ok(_dataService.getAllTransactions().Result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}
