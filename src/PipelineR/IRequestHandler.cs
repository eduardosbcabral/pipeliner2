using Polly;

namespace PipelineR
{
    public interface IRequestHandler<TPipelineContext> : IHandler<TPipelineContext> where TPipelineContext : BaseContext
    {
        IRequestHandler<TPipelineContext> NextRequestHandler { get; set; }
        Policy<RequestHandlerResult> PolicyRequestHandler { set; get; }

        string RequestHandleId();

        RequestHandlerResult HandleRequest();
    }
}