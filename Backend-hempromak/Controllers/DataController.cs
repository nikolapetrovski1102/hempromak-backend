using Backend_hempromak.Models;
using Backend_hempromak.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Previewer;
using QuestPDF.Helpers;
using System.Text.Json;
using QuestPDF.Companion;
using System.Net.Mail;
using System.Net.Mime;
using System.ComponentModel;
using System.Xml.Linq;


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

        [HttpGet("")]
        public IActionResult getFromTable([FromQuery] string current_table)
        {
            try
            {
                return Ok(_dataService.getAll(current_table));

            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("postTransaction")]
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

        [HttpGet("getTransactions")]
        public async Task<IActionResult> getTransactions()
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

        [HttpGet("getTransactionDetails/{transaction_id}")]
        public async Task<IActionResult> getTransactionDetails(int transaction_id)
        {
            try
            {
                return Ok( await _dataService.getTransactionDetailsAsync(transaction_id));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getCriticalItems")]
        public async Task<IActionResult> getCriticalItems()
            {
            try
            {
                return Ok(await _dataService.getCriticalItemsAsync());
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getHeadTypes")]
        public async Task<IActionResult> getHeadTypes([FromQuery] string head_type)
        {
            try
            {
                return Ok(await _dataService.getHeadTypesAsync(head_type));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getItemByHeadTypeAndSifra")]  
        public async Task<IActionResult> getItemByHeadTypeAndSifra([FromQuery] string head_type, [FromQuery] string sifra)
        {
            try
            {
                return Ok(await _dataService.getItemByHeadTypeAndSifraAsync(head_type, sifra));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("addItems")]
        public async Task<IActionResult> addItem([FromBody] ItemDTO item)
        {
            try
            {
                return Ok(await _dataService.addItemAsync(item));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("criticalItem")]
        public async Task<IActionResult> getCriticalItem()
        {
            try
            {
                return Ok(await _dataService.getCriticalItemAsync());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
