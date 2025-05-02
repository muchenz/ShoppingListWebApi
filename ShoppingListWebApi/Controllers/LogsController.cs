using AutoMapper;
using EFDataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceMediatR.ListCommandAndQueries;
using ServiceMediatR.SignalREvents;
using ServiceMediatR.UserCommandAndQuerry;
using Shared.DataEndpoints.Models;
using ShoppingListWebApi.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly ILogger<LogsController> _logger;
        private readonly IMapper _mapper;

        public LogsController(ILogger<LogsController> logger, IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet()]
        public async Task<IEnumerable<LogEntity>> GetAllLogs()
        {
            //var logs = await _context.Logs.AsQueryable().ToListAsync();
            //logs.Reverse();

            //return logs;
            return new List<LogEntity>();

        }

        [HttpPost("LogTest")]
        public async Task Log()
        {
            //var logEntity = new LogEntity();

            //logEntity.LogLevel = "aaaaaaaaa";
            //logEntity.StackTrace = "aaaaaaaaa";
            //logEntity.ExceptionMessage = "aaaaaaaaa";
            //logEntity.CreatedDate = DateTime.Now.ToString();
            //logEntity.Source = "Server";

            //_context.Add(logEntity);
            //await _context.SaveChangesAsync();

        }


        [HttpPost("LoggerTest2")]
        public async Task Log2()
        {
            //_logger.LogInformation("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            //_logger.LogInformation(new Exception("111111111"), "aaaa");

            //try
            //{
            //    try
            //    {
            //        throw new Exception("ala");
            //    }
            //    catch (Exception ex)
            //    {

            //        throw new Exception("ela", ex);


            //    }
            //}

            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "aaaa");


            //}

        }
        
        [Authorize]
        [HttpPost("Logger")]
        public async Task Log3([FromBody]Log log)
        {
            //var logEntity = _mapper.Map<LogEntity>(log);


            //_context.Add(logEntity);
            //await _context.SaveChangesAsync();
        }
    }
}
