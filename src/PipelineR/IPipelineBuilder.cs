namespace PipelineR
{
    public interface IPipelineBuilder<TContext> where TContext : BaseContext
    {
        IPipelineStarting<TContext> Pipeline { get; }
    }
}