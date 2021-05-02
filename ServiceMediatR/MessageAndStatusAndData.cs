

using ServiceMediatR.Models;
using Shared;

namespace ServiceMediatR
{
    public static class MessageAndStatusAndData
    {
        public static MessageAndStatusAndData<T> Fail<T>(string message, T data = default) =>
            new MessageAndStatusAndData<T>(data, message, true);

        public static MessageAndStatusAndData<T> Ok<T>(T data, string message) =>
            new MessageAndStatusAndData<T>(data, message, false);
    }

  
}
