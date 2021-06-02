using Polly;
using System;

namespace PipelineR
{
    public abstract class RollbackHandler<TContext> : IRollbackHandler<TContext> where TContext : BaseContext
    {
        public Func<TContext, bool> Condition { get; set; }
        public Policy Policy { get; set; }

        public int Index { get; private set; }

        internal Func<TContext, bool> RequestCondition { get; set; }

        protected RollbackHandler(TContext context)
        {
            this.Context = context;
        }

        public TContext Context { get; private set; }

        public abstract void HandleRollback();

        internal void Execute()
        {
            //if (this.RequestCondition != null && this.RequestCondition.IsSatisfied(this.Context, request) == false)
            //    return;

            //if (this.Condition != null && this.Condition.IsSatisfied(this.Context, request) == false)
            //    return;

            //if (this.Policy != null)
            //{
            //    this.Policy.Execute(() =>
            //    {
            //         HandleRollback(request);
            //    });
            //}
            //else
            //{
            //     HandleRollback(request);
            //}
        }

        internal void AddRollbackIndex(int rollbackIndex) => this.Index = rollbackIndex;

        public void UpdateContext(TContext context)
        {
            context.ConvertTo(this.Context);
        }
    }
}