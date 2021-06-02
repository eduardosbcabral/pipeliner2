using FluentValidation;
using System;
using System.Linq.Expressions;

namespace PipelineR
{
    public interface IPipeline<TContext> where TContext : BaseContext
    {
        RequestHandlerResult Execute<TRequest>(TRequest request) where TRequest : class;
        RequestHandlerResult Execute<TRequest>(TRequest request, string idempotencyKey) where TRequest : class;

        IPipeline<TContext> AddNext<IRequestHandler>();
        IPipeline<TContext> AddNext(IRequestHandler<TContext> requestHandler);
        IPipeline<TContext> AddFinally(IRequestHandler<TContext> requestHandler);
        IPipeline<TContext> AddFinally<TStepHandler>();
        IPipeline<TContext> AddValidator<TRequest>(IValidator<TRequest> validator) where TRequest : class;
        IPipeline<TContext> AddValidator<TRequest>() where TRequest : class;
        IPipeline<TContext> When(Expression<Func<TContext, bool>> func);
        IPipeline<TContext> When<TCondition>();
        //IPipeline<TContext> SetValue<TPropertie>(Expression<Func<TContext, TPropertie>> action, TPropertie value);
    }
}