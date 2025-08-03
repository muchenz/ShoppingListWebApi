using BlazorClient.Models;
using BlazorClient.Models.Requests;
using BlazorClient.Models.Response;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClient.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILocalStorageService _localStorage;
        private readonly StateService _userInfoService;
        private readonly TokenHttpClient _tokenHttpClient;

        public UserService(HttpClient httpClient, IConfiguration configuration, ILocalStorageService localStorage
            , StateService userInfoService, TokenHttpClient tokenHttpClient)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _localStorage = localStorage;
            _userInfoService = userInfoService;
            _tokenHttpClient = tokenHttpClient;
            _httpClient.BaseAddress = new Uri(_configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"]);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BlazorServer");

        }

        string token;
        private async Task SetRequestBearerAuthorizationHeader(HttpRequestMessage httpRequestMessage)
        {

            token = await _localStorage.GetItemAsync<string>("accessToken");
            var gid = await _localStorage.GetItemAsync<string>("gid");

            if (token != null)
            {

                httpRequestMessage.Headers.Authorization
                    = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
                // _httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer  {token}");
            }


            var signalRId = _userInfoService.StateInfo.ClientSignalRID;

            httpRequestMessage.Headers.Add("SignalRId", signalRId);
        }


        public async Task<MessageAndStatusAndData<UserNameAndTokensResponse>> RegisterAsync(RegistrationModel model)
        {

            var loginRequest = new RegistrationRequest
            {
                UserName = model.UserName,
                Password = model.Password
            };

            var json = JsonConvert.SerializeObject(loginRequest);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/Register")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };


            var response = await _httpClient.SendAsync(requestMessage);


            switch (response)
            {
                case { StatusCode: System.Net.HttpStatusCode.OK }:
                    var tokens = await response.Content.ReadFromJsonAsync<UserNameAndTokensResponse>();
                    return MessageAndStatusAndData<UserNameAndTokensResponse>.Ok(tokens);

                case { StatusCode: System.Net.HttpStatusCode.Conflict }:
                    return MessageAndStatusAndData<UserNameAndTokensResponse>.Fail("User exists.");

                default:
                    return MessageAndStatusAndData<UserNameAndTokensResponse>.Fail("Server error.");
            }

            //if (response.IsSuccessStatusCode)
            //{
            //    var token = await response.Content.ReadAsStringAsync();

            //    return MessageAndStatusAndData<string>.Ok(token);
            //}

            //return response switch
            //{
            //    { StatusCode: System.Net.HttpStatusCode.Conflict } =>
            //         MessageAndStatusAndData<string>.Fail("User exists."),
            //    _ =>
            //        MessageAndStatusAndData<string>.Fail("Server error."),
            //};

        }


        public async Task<MessageAndStatusAndData<UserNameAndTokensResponse>> LoginAsync(string userName, string password, string deviceId)
        {
            var loginRequest = new LoginRequest
            {
                UserName = userName,
                Password = password,
                DeviceId = deviceId
            };

            var json = JsonConvert.SerializeObject(loginRequest);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/Login")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };


            var response = await _httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                return MessageAndStatusAndData<UserNameAndTokensResponse>.Fail("Invalid username or password.");
            }

            var content = await response.Content.ReadAsStringAsync();

            var tokenAndUsername = JsonConvert.DeserializeObject<UserNameAndTokensResponse>(content);


            return MessageAndStatusAndData<UserNameAndTokensResponse>.Ok(tokenAndUsername);

        }


        public async Task<List<ListAggregationWithUsersPermission>> GetListAggrWithUsersPermAsync()
        {


            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "User/ListAggregationWithUsersPermission");


            //await SetRequestBearerAuthorizationHeader(requestMessage);


            //var response = await _httpClient.SendAsync(requestMessage);
            var response = await _tokenHttpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var lists = JsonConvert.DeserializeObject<List<ListAggregationWithUsersPermission>>(data);

            return lists;
        }
        public async Task<List<ListAggregationWithUsersPermission>> GetListAggrWithUsersPerm_EmptyAsync()
        {

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "Permissions/ListAggregationWithUsersPermission_Empty");


            // await SetRequestBearerAuthorizationHeader(requestMessage);


            //var response = await _httpClient.SendAsync(requestMessage);
            var response = await _tokenHttpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var list = JsonConvert.DeserializeObject<List<ListAggregationWithUsersPermission>>(data);


            return list;
        }

        public async Task<List<UserPermissionToListAggregation>> GetUsersPermissionByListAggrId
            (int listAggregationId)
        {

            var querry = new QueryBuilder();

            querry.Add("listAggregationId", listAggregationId.ToString());

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "Permissions/ListUsersPermissionByListAggrId" + querry.ToString());


            //await SetRequestBearerAuthorizationHeader(requestMessage);
            //SetRequestAuthorizationLevelHeader(requestMessage, listAggregationId);

            //var response = await _httpClient.SendAsync(requestMessage);
            var response = await _tokenHttpClient.SendAsync(requestMessage, listAggregationId);

            var data = await response.Content.ReadAsStringAsync();

            var list = JsonConvert.DeserializeObject<List<UserPermissionToListAggregation>>(data);

            return list;
        }




        public async Task<User> GetUserDataTreeAsync()
        {

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "User/UserDataTree");


            //await SetRequestBearerAuthorizationHeader(requestMessage);

            var response = await _tokenHttpClient.SendAsync(requestMessage);

            //var response = await _httpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var user = JsonConvert.DeserializeObject<User>(data);


            return await Task.FromResult(user);
        }
        public async Task LogOutAsync()
        {

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "User/LogOut");

            //await SetRequestBearerAuthorizationHeader(requestMessage);

            //var response = await _httpClient.SendAsync(requestMessage);
            var response = await _tokenHttpClient.SendAsync(requestMessage);

        }

        public async Task<MessageAndStatus> AddUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "AddUserPermission");
        }

        public async Task<MessageAndStatus> ChangeUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {

            return await UniversalUserPermission(userPermissionToList, listAggregationId, "ChangeUserPermission");

        }


        public async Task<MessageAndStatus> DeleteUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "DeleteUserPermission");
        }

        public async Task<MessageAndStatus> InviteUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "InviteUserPermission");
        }

        private async Task<MessageAndStatus> UniversalUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId,
            string actionName)
        {
            var querry = new QueryBuilder();

            querry.Add("listAggregationId", listAggregationId.ToString());

            //var httpMethod = actionName == "DeleteUserPermission"? HttpMethod.Delete : HttpMethod.Post;

            string serializedUser = JsonConvert.SerializeObject(userPermissionToList);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Permissions/" + actionName + querry.ToString());


            requestMessage.Content = new StringContent(serializedUser);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            //await SetRequestBearerAuthorizationHeader(requestMessage);
            //SetRequestAuthorizationLevelHeader(requestMessage, listAggregationId);

            //var response = await _httpClient.SendAsync(requestMessage);
            var response = await _tokenHttpClient.SendAsync(requestMessage, listAggregationId);

            var responseStatusCode = response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();

                var problem = JsonConvert.DeserializeObject<ProblemDetails>(responseBody);

                return MessageAndStatus.Fail(problem.Title);
            }

            var message = actionName switch
            {
                "AddUserPermission" => "User was added.",
                "ChangeUserPermission" => "Permission has changed.",
                "InviteUserPermission" => "Ivitation was added.",
                "DeleteUserPermission" => "User permission was deleted.",
                _ => throw new ArgumentException("Bad action name.")
            };

            return MessageAndStatus.Ok(message);

        }


        public async Task<List<Invitation>> GetInvitationsListAsync()
        {

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "Invitation/InvitationsList");


            //await SetRequestBearerAuthorizationHeader(requestMessage);


            //var response = await _httpClient.SendAsync(requestMessage);
            var response = await _tokenHttpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();


            var invitations = JsonConvert.DeserializeObject<List<Invitation>>(data);


            return invitations;
        }



        public async Task<MessageAndStatus> AcceptInvitationAsync(Invitation invitation)
        {
            return await UniversalInvitationAction(invitation, "AcceptInvitation");

        }
        public async Task<MessageAndStatus> RejectInvitaionAsync(Invitation invitation)
        {

            return await UniversalInvitationAction(invitation, "RejectInvitaion");

        }

        async Task<MessageAndStatus> UniversalInvitationAction(Invitation invitation, string actionName)
        {
            string serialized = JsonConvert.SerializeObject(invitation);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Invitation/" + actionName);


            requestMessage.Content = new StringContent(serialized);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            //await SetRequestBearerAuthorizationHeader(requestMessage);

            //var response = await _httpClient.SendAsync(requestMessage);
            var response = await _tokenHttpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                return MessageAndStatus.Ok();
            }
            return MessageAndStatus.Fail();
        }

        public async Task<bool> VerifyAcceessRefreshTokens(string accessToken, string refreshToken)
        {
            var veryfiRequest = new VerifyAccessRefreshTokenRequest
            {
                RefreshToken = refreshToken
            };

            var json = JsonConvert.SerializeObject(veryfiRequest);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/VerifyAcceessRefreshTokens")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            requestMessage.Headers.Authorization
                    = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", accessToken);
            HttpResponseMessage response = null;
            try
            {
                 response = await _httpClient.SendAsync(requestMessage);
            }
            catch (Exception ex)
            {
                return false;
            }

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        void SetRequestAuthorizationLevelHeader(HttpRequestMessage httpRequestMessage, int listAggregationId)
        {

            if (token != null)
            {
                httpRequestMessage.Headers.Add("listAggregationId", listAggregationId.ToString());

                using SHA256 mySHA256 = SHA256.Create();

                var bytes = Encoding.ASCII.GetBytes(token + listAggregationId.ToString());

                var hashBytes = mySHA256.ComputeHash(bytes);

                var hashString = Convert.ToBase64String(hashBytes);

                httpRequestMessage.Headers.Add("Hash", hashString);

            }
        }
    }
}
