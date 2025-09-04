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

    public record Error
    {
        public static readonly Error None = new(string.Empty, string.Empty, ErrorTypes.None);
        //public static readonly Error NullValue = new("Error.NullValue", "Null value was provided", ErrorType.Failure);


        private Error(string code, string description, string errorType)
        {
            (Code, Description, ErrorType) = (code, description, errorType);
        }

        public string Code { get; }
        public string Description { get; }
        public string ErrorType { get; }

        //public static implicit operator Result(Error error) => Result.Failure(error);

        public static Error Ok(string code, string description) => new(code, description, ErrorTypes.None);
        public static Error Unexpected(string code, string description) => new(code, description, ErrorTypes.Unexpected);
        public static Error ValidationError(string code, string description) => new(code, description, ErrorTypes.ValidationError);
        public static Error Conflict(string code, string description) => new(code, description, ErrorTypes.Conflict);
        public static Error NotFound(string code, string description) => new(code, description, ErrorTypes.NotFound);
        public static Error Forbidden(string code, string description) => new(code, description, ErrorTypes.Forbidden);


        public static Error Ok(string description) => new(string.Empty, description, ErrorTypes.None);
        public static Error Unexpected(string description) => new(string.Empty, description, ErrorTypes.Unexpected);
        public static Error ValidationError( string description) => new(string.Empty, description, ErrorTypes.ValidationError);
        public static Error Conflict( string description) => new(string.Empty, description, ErrorTypes.Conflict);
        public static Error NotFound(string description) => new(string.Empty, description, ErrorTypes.NotFound);
        public static Error Forbidden(string description) => new(string.Empty, description, ErrorTypes.Forbidden);
        public static Error Unauthorized(string description) => new(string.Empty, description, ErrorTypes.Unauthorized);
    }

    public class ErrorTypes
    {
        public const string None = "NONE";
        public const string Unexpected = "UNEXCEPTED";
        public const string ValidationError = "VALIDATION_ERROR";
        public const string Conflict = "CONFLICT";
        public const string NotFound = "NOT_FOUND";
        public const string Forbidden = "FORBIDDEN";
        public const string Unauthorized = "UNAUTHORIZED";


    }


    public class Result
    {

        public Error GetError() => _errors.First();
        public Error[] GetErrors() => _errors;

        Error[] _errors;
        protected Result(bool isErros, Error error)
        {
            _isError = isErros;
            _errors = [error];

        }
        protected Result(bool isErros, Error[] errors)
        {
            if (errors == null || errors.Length == 0)
            {
                throw new ArgumentException("'Eerrors' must be not empty or null.");
            }
            _isError = isErros;
            _errors = errors;

        }

        private bool _isError = false;
        public bool IsError => _isError;
        public bool IsSuccess => !_isError;


        public static Result Ok() =>
           new Result(false, Error.None);
        
        public static Result Failure(Error error) =>
           new Result(true, error);
        public static Result Failure(Error[] errors) =>
            new Result(true, errors);
    }
      

    public class Result<T> : Result
    {
        private Result(T data, bool isError, Error error): base(isError, error)
        {
            Data = data;
        }
        private Result(T data, bool isError, Error[] errors) : base(isError, errors)
        {
            Data = data;
        }
        public T Data { get; set; }

        public static new Result<T> Ok(T data) =>
            new Result<T>(data, false, Error.None);

        public static new Result<T> Failure(Error error) =>
           new Result<T>(default, true, error);
        public static new Result<T> Failure(Error[] errors) =>
          new Result<T>(default, true, errors);

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
