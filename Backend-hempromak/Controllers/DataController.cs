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

        public DataController (IConfiguration configuration, DataService dataService)
        {
            _dbContext = new DbContext ();
            _configuration = configuration;
            _dataService = dataService;
        }

        [HttpGet("tables")]
        public IActionResult getALlTables ()
        {
            try
            {
                var createTableQuery = @"
                CREATE TABLE IF NOT EXISTS users (
                    ID INT NOT NULL AUTO_INCREMENT,
                    Username VARCHAR(250) NOT NULL,
                    Email VARCHAR(250) NOT NULL,
                    Password VARCHAR(250) NOT NULL,
                    Role BIT(1) DEFAULT NULL,
                    isActive BIT(1) NOT NULL,
                    date_created DATETIME,
                    PRIMARY KEY (ID),
                    INDEX idx_email (Email)
                );";

                var query = _dbContext.executeSqlQuery(createTableQuery);

                return Ok(query);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("")]
        public IActionResult getFromTable([FromQuery] string current_table)
        {
            try
            {
                return Ok(_dataService.getAll(current_table));

            }catch(Exception ex)
            {
                return BadRequest(ex.ToString());
            }

        }

        [HttpPost("postTransaction")]
        public async Task<IActionResult> postTransaction([FromBody] TransferModel transferModel)
        {
            try
            {
                await _dataService.postTransactionAsync(transferModel, HttpContext);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
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
                return BadRequest(ex.ToString());
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
                return BadRequest(ex.ToString());
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
                return BadRequest(ex.ToString());
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
                return BadRequest(ex.ToString());
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
                return BadRequest(ex.ToString());
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
                return BadRequest(ex.ToString());
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
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet("getTableCount")]
        public async Task<IActionResult> GetTableCount([FromQuery] string tables)
        {
            try
            {
                return Ok(await _dataService.getTableCountAsync(tables));
            }
            catch(Exception ex) 
            {
                return BadRequest(ex.ToString());
            }
        }

    }
}
