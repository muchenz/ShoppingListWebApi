using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using AutoMapper;
using EFDataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using SignalRService;


namespace ShoppingListWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvitationController : ControllerBase
    {

        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;
        private readonly SignarRService _signarRService;
        private readonly IInvitationEndpoint _invitationEndpoint;

        public InvitationController(IMapper mapper, SignarRService signarRService
            , IInvitationEndpoint invitationEndpoint)
        {
            _mapper = mapper;
            _signarRService = signarRService;
            _invitationEndpoint = invitationEndpoint;
        }

        [HttpPost("GetInvitationsList")]
        public async Task<ActionResult<MessageAndStatus>> GetInvitationsList(string userName)
        {
            //TODO: 'userName' should be get from context

            var invitationsList = await _invitationEndpoint.GetInvitationsListAsync(userName);

            return new MessageAndStatus { Status = "OK", Message = JsonConvert.SerializeObject(invitationsList) };
        }



        [HttpPost("RejectInvitaion")]
        public async Task<ActionResult<MessageAndStatus>> RejectInvitaion([FromBody] Invitation invitation, [FromHeader] string signalRId)
        {

            await _invitationEndpoint.RejectInvitaionAsync(invitation);   

            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault()?.Value;
            //TODO: for what sending 'new_invitation'?? for refresh list with invitation??
            if (userId != null)
                await _signarRService.SendRefreshMessageToUsersAsync(new List<int> { int.Parse(userId) }, 
                    SiganalREventName.InvitationAreChanged, signalRId: signalRId);

            return await Task.FromResult(new MessageAndStatus { Status = "OK" });
        }

        [HttpPost("AcceptInvitation")]
        public async Task<ActionResult<MessageAndStatus>> AcceptInvitation([FromBody] Invitation invitation, [FromHeader] string signalRId)
        {
            var userId = int.Parse(User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value);

            await _invitationEndpoint.AcceptInvitationAsync(invitation, userId);

           await _signarRService.SendRefreshMessageToUsersAsync(new List<int> { userId }, 
               SiganalREventName.InvitationAreChanged, signalRId: signalRId);

            //TODO: for what sending 'new_invitation'?? for refresh list with invitation??

            return await Task.FromResult(new MessageAndStatus { Status = "OK" });
        }
    }
}