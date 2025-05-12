using EFDataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Shared.DataEndpoints.Abstaractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Auth.Api
{


    public class SecurityLevelAttribute : AuthorizeAttribute
    {
        public SecurityLevelAttribute(int level)
        {
            Policy = $"Level.{level}";
        }
    }



    public class CustomRequirePermissionLevel : IAuthorizationRequirement
    {
        public int Level { get; }
        public CustomRequirePermissionLevel(int level)
        {
            Level = level;
        }
    }

    public class CustomRequirePermissionLevelHandler : AuthorizationHandler<CustomRequirePermissionLevel>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserEndpoint _userEndpoint;

        public CustomRequirePermissionLevelHandler(IHttpContextAccessor httpContextAccessor, IUserEndpoint userEndpoint)
        {
            _httpContextAccessor = httpContextAccessor;
            _userEndpoint = userEndpoint;
        }

        protected override async Task HandleRequirementAsync(
          AuthorizationHandlerContext context,
          CustomRequirePermissionLevel requirement)
        {


            // var listAggregationId = _httpContextAccessor.HttpContext.Request.Query["listAggregationId"];
            var lvlRequiredFromCode = requirement.Level.ToString();

            StringValues value1;

            var hash = _httpContextAccessor.HttpContext.Request.Headers["Hash"].ToString();
            _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("listAggregationId", out value1);
            var token = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Split(" ")[1];

            string listAggregationId = value1.ToString();



            if (string.IsNullOrEmpty(listAggregationId) || string.IsNullOrEmpty(hash)
               || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(lvlRequiredFromCode))
                return;



            ComputeHash(token, listAggregationId, out string hashString);

            //bad veryfication - 
            if (hashString != hash)
                return;


            var intAgrId = int.Parse(listAggregationId);

            var intLvlReq = int.Parse(lvlRequiredFromCode);

            //var user = await _userEndpoint.GetUserByNameAsync(context.User.Identity.Name);
            //if (user == null) return;
            // var userListAgg = await _userEndpoint.GetUserListAggrByUserId(user.UserId);

            var userId = context.User.Claims.Where(a=>a.Type == ClaimTypes.NameIdentifier).FirstOrDefault();

            if (userId == null) 
                return;

            var userListAgg = await _userEndpoint.GetUserListAggrByUserId( int.Parse(userId.Value));
                                   

            if (userListAgg?.Where(a=>a.ListAggregatorId==intAgrId).FirstOrDefault()?.PermissionLevel <= intLvlReq)
            {
                context.Succeed(requirement);
            }
        }


        
        //protected override Task HandleRequirementAsync(
        //    AuthorizationHandlerContext context,
        //    CustomRequirePermissionLevel requirement)
        //{

        //    // var listAggregationId = _httpContextAccessor.HttpContext.Request.Query["listAggregationId"];
        //    var lvlRequiredFromCode = requirement.Level.ToString();

        //    StringValues value1;

        //    var hash = _httpContextAccessor.HttpContext.Request.Headers["Hash"].ToString();
        //    _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("listAggregationId", out value1);
        //    var token = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Split(" ")[1];

        //    string listAggregationId = value1.ToString();



        //    if (string.IsNullOrEmpty(listAggregationId) || string.IsNullOrEmpty(hash)
        //       || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(lvlRequiredFromCode))
        //        return Task.CompletedTask;
                                 
          

        //    ComputeHash(token, listAggregationId, out string hashString);

        //    //bad veryfication - 
        //    if (hashString != hash)
        //        return Task.CompletedTask;
           

        //    var intAgrId = int.Parse(listAggregationId);

        //    var intLvlReq = int.Parse(lvlRequiredFromCode);


        //    var aggreagtonLevelList = context.User.Claims.Where(a => a.Type == "ListAggregator").Select(a =>
        //    {
        //        var split = a.Value.Split(".");

        //        return new { aggrId = split[0], lvl = split[1] };
        //    }).ToList();


        //    if (aggreagtonLevelList.Any(a => a.aggrId == listAggregationId))
        //        if (int.Parse(aggreagtonLevelList.Single(a => a.aggrId == listAggregationId).lvl) <= intLvlReq)
        //            context.Succeed(requirement);


        //    return Task.CompletedTask;
        //}

        private static void ComputeHash(string token, string listAggregationId, out string hashString)
        {
            var mySHA256 = SHA256.Create();
            var bytes = Encoding.ASCII.GetBytes(token + listAggregationId);

            var hashBytes = mySHA256.ComputeHash(bytes);

            hashString = Convert.ToBase64String(hashBytes);
        }
    }

    public class CustomAuthorizationPolicyProvider
       : DefaultAuthorizationPolicyProvider
    {
        public CustomAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public override Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {

            if (policyName.StartsWith("Level"))
            {
                var parts = policyName.Split('.');
                var type = parts.First();
                var value = parts.Last();

                var policy = new AuthorizationPolicyBuilder()
                          .AddRequirements(new CustomRequirePermissionLevel(Convert.ToInt32(value)))
                          .Build();


                return Task.FromResult(policy);
            }
           

            return base.GetPolicyAsync(policyName);
        }
    }
}
