using System;

namespace PipelineR
{
    public static class ConditionalExtension
    {
        public static bool IsSatisfied<TContext>(this Func<TContext, bool> condition, TContext context)
        {
            return condition.Invoke(context);
        }

        public static IRequestHandler<TContext> When<TContext>(
            this IRequestHandler<TContext> requestHandler, Func<TContext, bool> condition) where TContext : BaseContext
        {
            ((RequestHandler<TContext>)requestHandler).Condition = condition;
            return requestHandler;
        }
    }
}