using MediatR;
using Shared.DataEndpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceMediatR.Wrappers
{
    public interface IRequestWrapper<T> : IRequest<MessageAndStatusAndData<T>>
    { }
    public interface IHandlerWrapper<TIn, TOut> : IRequestHandler<TIn, MessageAndStatusAndData<TOut>>
        where TIn : IRequestWrapper<TOut>
    { }
}
