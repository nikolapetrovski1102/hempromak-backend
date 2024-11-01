﻿using Backend_hempromak.Models;
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
    //[Authorize]
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

    }
}
