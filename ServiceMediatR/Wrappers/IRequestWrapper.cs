using MediatR;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceMediatR.Wrappers
{
    public interface IRequestWrapper<T> : IRequest<Result<T>>
    { }
    public interface IHandlerWrapper<TIn, TOut> : IRequestHandler<TIn, Result<TOut>>
        where TIn : IRequestWrapper<TOut>
    { }
}
