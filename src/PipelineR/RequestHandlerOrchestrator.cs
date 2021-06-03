namespace PipelineR
{
    public static class RequestHandlerOrchestrator
    {

        public static RequestHandlerResult ExecuteHandler<TContext>(IRequestHandler<TContext> requestHandler) where TContext : BaseContext
            => ExecuteHandler(requestHandler, string.Empty);

        public static RequestHandlerResult ExecuteHandler<TContext>(IRequestHandler<TContext> requestHandler, string requestHandlerId) where TContext : BaseContext
        {
            requestHandler.Context.CurrentRequestHandlerId = requestHandler.RequestHandlerId();

            if (UseRequestHandlerId(requestHandlerId) &&
                !requestHandler.Context.CurrentRequestHandlerId.Equals(requestHandlerId, System.StringComparison.InvariantCultureIgnoreCase))
            {
                return ((RequestHandler<TContext>)requestHandler).Next(requestHandlerId);
            }

            if (requestHandler.Condition is null || requestHandler.Condition.IsSatisfied(requestHandler.Context))
            {
                return requestHandler.HandleRequest();
            }
            
            return ((RequestHandler<TContext>)requestHandler).Next();
        }

        private static bool UseRequestHandlerId(string requestHandlerId)
            => !string.IsNullOrWhiteSpace(requestHandlerId);
    }
}
