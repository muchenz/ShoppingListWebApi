using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.DataEndpoints.Models
{
    public  class Log
    {
        public long LogId { get; set; }
        public string LogLevel { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }
        public string CreatedDate { get; set; }
        public long? UserId { get; set; }
        public Log Inner { get; set; }

    }
    public class MessageStaus
    {
        public const string OK = "OK";
        public const string Error = "ERROR";
        public const string ValidationError = "VALIDATION_ERROR";
        public const string Conflict = "CONFLICT";
        public const string NotFound = "NOT_FOUND";


    }
    public class MessageAndStatus
    {
        public MessageAndStatus(string message, string status)
        {
            Message= message;
            Status = status;
        }

        public bool IsError => Status != MessageStaus.OK;
        public string Status { get; set; }
        public string Message { get; set; }
        public List<(string,string)> ValidationErrorList { get; set; }

        public static MessageAndStatus Ok() =>
           new MessageAndStatus(string.Empty, MessageStaus.OK);
        public static MessageAndStatus Ok(string msg) =>
           new MessageAndStatus(msg, MessageStaus.OK);

        public static MessageAndStatus Error(string msg) =>
           new MessageAndStatus(msg, MessageStaus.Error);
        public static MessageAndStatus Conflict(string msg) =>
           new MessageAndStatus(msg, MessageStaus.Conflict);
        public static MessageAndStatus NotFound(string msg) =>
           new MessageAndStatus(msg, MessageStaus.NotFound);
    }
      

    public class MessageAndStatusAndData<T> : MessageAndStatus
    {
        private MessageAndStatusAndData(T data, string msg, string status): base(msg, status)
        {
            Data = data;
        }

        public T Data { get; set; }

        public static new MessageAndStatusAndData<T> Ok(T data) =>
            new MessageAndStatusAndData<T>(data, string.Empty, MessageStaus.OK);

        public static new MessageAndStatusAndData<T> Error(string msg) =>
           new MessageAndStatusAndData<T>(default, msg, MessageStaus.Error);
    }

    //public class MessageAndStatusAndData
    //{
    //    public static MessageAndStatusAndData<T> Ok<T>(T data) =>
    //       new MessageAndStatusAndData<T>(data, string.Empty, MessageSatus.OK);

    //    public static MessageAndStatusAndData<T> Fail<T>(string msg) =>
    //       new MessageAndStatusAndData<T>(default, msg, MessageSatus.Error);
    //}

    public class Invitation
    {
        public int InvitationId { get; set; }
        public string EmailAddress { get; set; }
        public int PermissionLevel { get; set; }
        public int ListAggregatorId { get; set; }
        public string ListAggregatorName { get; set; }
        public string SenderName { get; set; }

    }

    public class User
    {

        public User()
        {
            ListAggregators = new HashSet<ListAggregator>();
            Roles = new HashSet<string>();

        }

        public int UserId { get; set; }
        public string EmailAddress { get; set; }
        // public string Password { get; set; }

        public byte LoginType { get; set; } // 1 - local 2 - facebook ()
        public ICollection<ListAggregator> ListAggregators { get; set; }
        public ICollection<string> Roles { get; set; }

        // public virtual TokenItem Token { get; set; }

    }
    public class UserListAggregator
    { 
        public int UserId { get; set; }
        public int ListAggregatorId { get; set; }
        public virtual User User { get; set; }
        public virtual ListAggregator ListAggregator { get; set; }
        public int PermissionLevel { get; set; }
        public int State { get; set; }

    }

    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }

    }


    public class ListAggregator
    {

        public ListAggregator()
        {
            Lists = new HashSet<List>();

        }

        public int ListAggregatorId { get; set; }
        public string ListAggregatorName { get; set; }
        public int PermissionLevel { get; set; }


        public ICollection<List> Lists { get; set; }

    }



    public class List
    {
        public List()
        {
            ListItems = new HashSet<ListItem>();

        }
        public int ListId { get; set; }
        public string ListName { get; set; }
        public int Order { get; set; }

        public ICollection<ListItem> ListItems { get; set; }
        public int ListAggrId { get; set; }


    }


    public class ListItem
    {
        public int ListItemId { get; set; }
        public int Order { get; set; }
        public int State { get; set; }

        public string ListItemName { get; set; }
        public int ListAggrId { get; set; }

    }


    public class ListAggregationWithUsersPermission
    {

        public ListAggregator ListAggregator { get; set; }

        public List<UserPermissionToListAggregation> UsersPermToListAggr { get; set; }
    }

    public class UserPermissionToListAggregation
    {

        public User User { get; set; }
        public int Permission { get; set; }

    }

    //public class TokenItem
    //{
    //    public int TokenId { get; set; }
    //    public string Token { get; set; }
    //    public int UserId { get; set; }
    //    public User User { get; set; }


    //}
    public static class SiganalREventName
    {
        public const string ListItemEdited = nameof(ListItemEdited);
        public const string ListItemAdded = nameof(ListItemAdded);
        public const string ListItemDeleted = nameof(ListItemDeleted);
        public const string InvitationAreChanged = nameof(InvitationAreChanged);
        public const string DataAreChanged = nameof(DataAreChanged);
    }

}
