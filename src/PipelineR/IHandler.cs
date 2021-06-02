using Polly;
using System;

namespace PipelineR
{
    public interface IHandler<TPipelineContext> where TPipelineContext : BaseContext
    {
        TPipelineContext Context { get; }
        Policy Policy { get; set; }
        Func<TPipelineContext, bool> Condition { get; set; }

        void UpdateContext(TPipelineContext context);
    }
}