using EFDataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace ShoppingListWebApi.Logging
{
    internal class DbLogger : ILogger
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DbLogger(IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor)
        {
            _serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var  logEntity = new LogEntity();
            var rootLogEntity = logEntity;

            bool isInnerException;
            var scope  = _serviceProvider.CreateScope().ServiceProvider;
            var _shopingListDBContext = scope.GetRequiredService<ShopingListDBContext>();

            do
            {
                logEntity.LogLevel = logLevel.ToString();
                logEntity.StackTrace = exception?.StackTrace;
                logEntity.ExceptionMessage = exception?.Message;
                logEntity.CreatedDate = DateTime.Now.ToString();
                logEntity.Source = "Server";
                logEntity.Message = state.ToString();


                 isInnerException = exception?.InnerException != null;

                if (isInnerException)
                {
                    var tempLogEntity = new LogEntity();
                    logEntity.Inner = tempLogEntity;
                    logEntity = tempLogEntity;
                    exception = exception.InnerException;
                }


            } while (isInnerException);

            _shopingListDBContext.Add(rootLogEntity);
            _shopingListDBContext.SaveChanges();
        }
    }
}

//DELETE FROM[Logs] WHERE log_id IN
//(SELECT  log_id
//FROM [Logs] ORDER BY datetime(substr(created_date,7,4)|| "-" || substr(created_date, 4, 2) || "-" || substr(created_date, 1, 2) || " " || substr(created_date, 12, 8)) DESC, log_id DESC Limit -1 offset 5)