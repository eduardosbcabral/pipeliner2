using Polly;
using System;

namespace PipelineR
{
    public abstract class RequestHandler<TContext> : IRequestHandler<TContext> where TContext : BaseContext
    {
        protected RequestHandler(TContext context)
        {
            this.Context = context;
        }

        protected RequestHandler(TContext context, Func<TContext, bool> condition) : this(context)
        {
            this.Condition = condition;
        }

        public TContext Context { get; private set; }
        public Func<TContext, bool> Condition { get; set; }
        public IRequestHandler<TContext> NextRequestHandler { get; set; }

        public Policy Policy { get; set; }
        public Policy<RequestHandlerResult> PolicyRequestHandler { get; set; }

        //private int _rollbackIndex;
        //private Pipeline<TContext> _pipeline;
        //private TRequest _request;

        protected RequestHandlerResult Abort(string errorMessage, int statusCode)
            => this.Context.Response = new RequestHandlerResult(errorMessage, statusCode, false)
            .WithRequestHandlerId(this.RequestHandleId());

        protected RequestHandlerResult Abort(string errorMessage)
            => this.Context.Response = new RequestHandlerResult(errorMessage, 0, false)
              .WithRequestHandlerId(this.RequestHandleId());

        protected RequestHandlerResult Abort(object errorResult, int statusCode)
            => this.Context.Response = new RequestHandlerResult(errorResult, statusCode, false)
              .WithRequestHandlerId(this.RequestHandleId());

        protected RequestHandlerResult Abort(object errorResult)
            => this.Context.Response = new RequestHandlerResult(errorResult, 0, false)
              .WithRequestHandlerId(this.RequestHandleId());

        protected RequestHandlerResult Abort(ErrorResult errorResult, int statusCode)
            => this.Context.Response = new RequestHandlerResult(errorResult, statusCode)
              .WithRequestHandlerId(this.RequestHandleId());

        protected RequestHandlerResult Abort(ErrorResult errorResult)
            => this.Context.Response = new RequestHandlerResult(errorResult, 0)
              .WithRequestHandlerId(this.RequestHandleId());

        protected RequestHandlerResult Finish(object result, int statusCode)
            => this.Context.Response = new RequestHandlerResult(result, statusCode, true)
              .WithRequestHandlerId(this.RequestHandleId());

        protected RequestHandlerResult Finish(object result)
            => this.Context.Response = new RequestHandlerResult(result, 0, true)
              .WithRequestHandlerId(this.RequestHandleId());

        public abstract RequestHandlerResult HandleRequest();

        public RequestHandlerResult Next() => Next(string.Empty);
        public RequestHandlerResult Next(string requestHandlerId)
        {
            if (this.NextRequestHandler != null)
                this.Context.Response = RequestHandlerOrchestrator.ExecuteHandler((RequestHandler<TContext>)this.NextRequestHandler, requestHandlerId);

            return this.Context.Response;
        }
        public string RequestHandleId()
        {
            return this.GetType().Name;
        }
        public void UpdateContext(TContext context)
        {
            context.ConvertTo(this.Context);
        }

        internal RequestHandlerResult Execute()
        {
            //_request = request;
            RequestHandlerResult result = null;

            if (this.Policy != null)
            {
                this.Policy.Execute(() =>
                {
                    result = HandleRequest();
                });
            }
            else if (this.PolicyRequestHandler != null)
            {
                result = this.PolicyRequestHandler.Execute(() =>
                {
                    if (this.Context.Response != null && this.Context.Response.IsSuccess() == false && this.Context.Response.RequestHandlerId != this.RequestHandleId())
                    {
                        throw new PipelinePolicyException(this.Context.Response);
                    }

                    return HandleRequest();
                });

                if (result.IsSuccess() == false)
                {
                    throw new PipelinePolicyException(this.Context.Response);
                }
            }
            else
            {
                result = HandleRequest();
            }

            return result;
        }

        //protected RequestHandlerResult Rollback(RequestHandlerResult result)
        //{
        //    this.Context.Response = result;

        //    this._pipeline.ExecuteRollback(this._rollbackIndex);

        //    return result;
        //}

        //public void AddRollbackIndex(int rollbackIndex) => this._rollbackIndex = rollbackIndex;


        //public void AddPipeline(Pipeline<TContext> pipeline) => this._pipeline = pipeline;
    }
}