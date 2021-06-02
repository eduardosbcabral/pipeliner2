namespace PipelineR
{
    public static class RequestHandlerOrchestrator
    {

        public static RequestHandlerResult ExecuteHandler<TContext>(IRequestHandler<TContext> requestHandler) where TContext : BaseContext
            => ExecuteHandler(requestHandler, string.Empty);

        public static RequestHandlerResult ExecuteHandler<TContext>(IRequestHandler<TContext> requestHandler, string requestHandlerId) where TContext : BaseContext
        {
            requestHandler.Context.CurrentRequestHandleId = requestHandler.RequestHandleId();

            if (UseRequestHandlerId(requestHandlerId) &&
                requestHandler.Context.CurrentRequestHandleId.Equals(requestHandlerId, System.StringComparison.InvariantCultureIgnoreCase) == false)
                return ((RequestHandler<TContext>)requestHandler).Next(requestHandlerId);

            if (requestHandler.Condition is null || requestHandler.Condition.IsSatisfied(requestHandler.Context))
                return requestHandler.HandleRequest();
            else
                return ((RequestHandler<TContext>)requestHandler).Next();
        }

        private static bool UseRequestHandlerId(string requestHandlerId) => string.IsNullOrWhiteSpace(requestHandlerId) == false;
    }
}
