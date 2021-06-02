namespace PipelineR
{
    public interface IRollbackHandler<TPipelineContext> : IHandler<TPipelineContext> where TPipelineContext : BaseContext
    {
        void HandleRollback();
    }
}