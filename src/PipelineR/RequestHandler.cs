using Polly;
using System;
using System.Net;

namespace PipelineR
{
    public abstract class RequestHandler<TContext> : IRequestHandler<TContext> where TContext : BaseContext
    {
        public TContext Context { get; private set; }
        public Func<TContext, bool> Condition { get; set; }
        public IRequestHandler<TContext> NextRequestHandler { get; set; }

        public Policy Policy { get; set; }
        public Policy<RequestHandlerResult> PolicyRequestHandler { get; set; }

        private const int DEFAULT_FAILURE_STATUS_CODE = 400;
        private const int DEFAULT_SUCCESS_STATUS_CODE = 200;

        //private int _rollbackIndex;
        //private Pipeline<TContext> _pipeline;
        //private TRequest _request;

        protected RequestHandler(TContext context)
        {
            this.Context = context;
        }

        protected RequestHandler(TContext context, Func<TContext, bool> condition) : this(context)
        {
            this.Condition = condition;
        }

        public string RequestHandlerId()
            => this.GetType().Name;

        public void UpdateContext(TContext context)
            => context.ConvertTo(this.Context);

        public abstract RequestHandlerResult HandleRequest();

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
                    if (this.Context.Response != null && !this.Context.Response.IsSuccess() && this.Context.Response.RequestHandlerId != this.RequestHandlerId())
                    {
                        throw new PipelinePolicyException(this.Context.Response);
                    }

                    return HandleRequest();
                });

                if (!result.IsSuccess())
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

        public RequestHandlerResult Next()
            => Next(string.Empty);

        public RequestHandlerResult Next(string requestHandlerId)
        {
            if (this.NextRequestHandler is null)
            {
                this.Context.Response = RequestHandlerOrchestrator.ExecuteHandler(this.NextRequestHandler, requestHandlerId);
            }

            return this.Context.Response;
        }

        protected RequestHandlerResult Abort(string errorMessage, int statusCode)
            => this.Context.Response = this.BaseAbort(errorMessage: errorMessage, statusCode: statusCode);

        protected RequestHandlerResult Abort(string errorMessage, HttpStatusCode httpStatusCode)
            => this.Context.Response = this.BaseAbort(errorMessage: errorMessage, statusCode: (int)httpStatusCode);

        protected RequestHandlerResult Abort(string errorMessage)
            => this.BaseAbort(errorMessage: errorMessage);

        protected RequestHandlerResult Abort(object errorResult, int statusCode)
            => this.Context.Response = this.BaseAbort(errorResultObject: errorResult, statusCode: statusCode);

        protected RequestHandlerResult Abort(object errorResult, HttpStatusCode httpStatusCode)
            => this.Context.Response = this.BaseAbort(errorResultObject: errorResult, statusCode: (int)httpStatusCode);

        protected RequestHandlerResult Abort(object errorResult)
            => this.Context.Response = this.BaseAbort(errorResultObject: errorResult);

        protected RequestHandlerResult Abort(ErrorResult errorResult, int statusCode)
            => this.Context.Response = this.BaseAbort(errorResult: errorResult, statusCode: statusCode);

        protected RequestHandlerResult Abort(ErrorResult errorResult, HttpStatusCode httpStatusCode)
            => this.Context.Response = this.BaseAbort(errorResult: errorResult, statusCode: (int)httpStatusCode);

        protected RequestHandlerResult Abort(ErrorResult errorResult)
            => this.Context.Response = this.BaseAbort(errorResult: errorResult);

        private RequestHandlerResult BaseAbort(
            string errorMessage = "",
            int statusCode = DEFAULT_FAILURE_STATUS_CODE,
            object errorResultObject = null,
            ErrorResult errorResult = null)
            => this.Context.Response = RequestHandlerResultBuilder.Instance()
                .WithErrorMessage(errorMessage)
                .WithStatusCode(statusCode)
                .WithResultObject(errorResultObject)
                .WithErrors(errorResult)
                .WithFailure()
                .WithRequestHandlerId(this.RequestHandlerId())
                .Build();

        protected RequestHandlerResult Finish(object result, int statusCode)
            => this.BaseFinish(result: result, statusCode: statusCode);

        protected RequestHandlerResult Finish(object result, HttpStatusCode httpStatusCode)
            => this.BaseFinish(result: result, statusCode: (int)httpStatusCode);

        protected RequestHandlerResult Finish(object result)
            => this.BaseFinish(result);

        private RequestHandlerResult BaseFinish(
            object result = null,
            int statusCode = DEFAULT_SUCCESS_STATUS_CODE)
            => this.Context.Response = RequestHandlerResultBuilder.Instance()
                .WithResultObject(result)
                .WithStatusCode(statusCode)
                .WithSuccess()
                .WithRequestHandlerId(this.RequestHandlerId())
                .Build();
    }
}