using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;

namespace PipelineR
{
    public class Pipeline<TContext> : IPipeline<TContext> where TContext : BaseContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICacheProvider _cacheProvider;

        private IValidator _validator;

        private IRequestHandler<TContext> _requestHandler;
        private IRequestHandler<TContext> _finallyRequestHandler;
        private IRequestHandler<TContext> _lastRequestHandlerAdd;

        private IHandler<TContext> _lastHandlerAdd;

        private readonly Stack<RollbackHandler<TContext>> _rollbacks;
        private readonly string _requestKey;
        private bool _useReuseRequisitionHash;

        public Pipeline(string requestKey = null) : this()
        {
            // AUTO INJECT
            //this._serviceProvider = serviceProvider;
            this._requestKey = requestKey;
            //this._cacheProvider = serviceProvider.GetService<ICacheProvider>();
        }

        public Pipeline()
        {
            _rollbacks = new Stack<RollbackHandler<TContext>>();
        }

        public IPipeline<TContext> UseRecoveryRequestByHash()
        {
            _useReuseRequisitionHash = true;
            return this;
        }

        public IPipeline<TContext> AddNext<TRequestHandler>()
        {
            var requestHandler = (IRequestHandler<TContext>)this._serviceProvider.GetService<TRequestHandler>();
            return this.AddNext(requestHandler);
        }

        public IPipeline<TContext> AddNext(IRequestHandler<TContext> requestHandler)
        {
            if (this._requestHandler == null)
                this._requestHandler = requestHandler;
            else
                GetLastRequestHandler(this._requestHandler).NextRequestHandler = requestHandler;

            _lastRequestHandlerAdd = requestHandler;
            _lastHandlerAdd = requestHandler;
            return this;
        }

        public IPipeline<TContext> AddNext<TRequestHandler>(Func<TContext, bool> condition)
            => this.AddNext<TRequestHandler>(condition, null);

        public IPipeline<TContext> AddNext<TRequestHandler>(Func<TContext, bool> condition, Policy policy)
        {
            var requestHandler = ((RequestHandler<TContext>)(IRequestHandler<TContext>)_serviceProvider.GetService<TRequestHandler>());

            requestHandler.Condition = condition;
            requestHandler.Policy = policy;
            //requestHandler.AddPipeline(this);

            return this.AddNext((RequestHandler<TContext>)requestHandler);
        }

        public IPipeline<TContext> When(Expression<Func<TContext, bool>> condition)
        {
            if (condition != null && this._lastHandlerAdd != null)
                this._lastHandlerAdd.Condition = condition.Compile();

            return this;
        }

        public IPipeline<TContext> When<TCondition>()
        {
            var instance = (ICondition<TContext>)this._serviceProvider.GetService<TCondition>();
            return When(instance.When());
        }

        public IPipeline<TContext> WithPolicy(Policy policy)
        {
            if (policy != null && this._lastHandlerAdd != null)
            {
                this._lastRequestHandlerAdd.Policy = policy;
            }

            return this;
        }

        public IPipeline<TContext> WithPolicy(Policy<RequestHandlerResult> policy)
        {
            if (policy != null && this._lastHandlerAdd != null)
            {
                this._lastRequestHandlerAdd.PolicyRequestHandler = policy;
            }

            return this;
        }

        //public IPipeline<TContext> Rollback(IRollbackHandler<TContext> rollbackHandler)
        //{
        //    var rollbackHandlerAux = (RollbackHandler<TContext, TRequest>)rollbackHandler;

        //    _lastHandlerAdd = rollbackHandler;

        //    _rollbacks.Push(rollbackHandlerAux);

        //    var rollbackIndex = _rollbacks.Count;

        //    rollbackHandlerAux.AddRollbackIndex(rollbackIndex);
        //    rollbackHandlerAux.RequestCondition = this._lastRequestHandlerAdd.Condition;

        //    this._lastRequestHandlerAdd.AddRollbackIndex(rollbackIndex);

        //    return this;
        //}

        //public IPipeline<TContext> Rollback<TRollbackHandler>() where TRollbackHandler : IRollbackHandler<TContext>
        //{
        //    var rollbackHandler = (IRollbackHandler<TContext>)_serviceProvider.GetService<TRollbackHandler>();
        //    this.Rollback(rollbackHandler);
        //    return this;
        //}

        public IPipeline<TContext> AddFinally(IRequestHandler<TContext> requestHandler)
        {
            _finallyRequestHandler = requestHandler;
            _lastRequestHandlerAdd = (RequestHandler<TContext>)requestHandler;
            return this;
        }

        public IPipeline<TContext> AddFinally<TRequestHandler>() => AddFinally<TRequestHandler>(null);

        public IPipeline<TContext> AddFinally<TRequestHandler>(Policy policy)
        {
            var requestHandler = (IRequestHandler<TContext>)_serviceProvider.GetService<TRequestHandler>();
            requestHandler.Policy = policy;
            return this.AddFinally(requestHandler);
        }

        public IPipeline<TContext> AddValidator<TRequest>(IValidator<TRequest> validator) where TRequest : class
        {
            _validator = validator;
            return this;
        }

        public IPipeline<TContext> AddValidator<TRequest>() where TRequest : class
        {
            var validator = (IValidator<TRequest>)_serviceProvider.GetService<TRequest>();
            return this.AddValidator(validator);
        }

        public RequestHandlerResult Execute<TRequest>(TRequest request) where TRequest : class
        {
            return Execute(request, string.Empty);
        }

        public RequestHandlerResult Execute<TRequest>(TRequest request, string idempotencyKey) where TRequest : class
        {
            if (this._validator != null)
            {
                var validateResult = this._validator.Validate(request);

                if (!validateResult.IsValid)
                {
                    var errors = validateResult.Errors
                        .Select(p => new ErrorResult(null, p.ErrorMessage, p.PropertyName))
                        .ToArray();

                    return RequestHandlerResultBuilder.Instance()
                        .WithErrors(errors)
                        .WithHttpStatusCode(HttpStatusCode.BadRequest)
                        .Build();
                }
            }

            if (this._requestHandler == null)
            {
                throw new ArgumentNullException("No started handlers");
            }

            RequestHandlerResult result = null;

            var lastRequestHandlerId = string.Empty;
            var nextRequestHandlerId = string.Empty;
            TContext context = null;

            var hash = idempotencyKey == string.Empty ? request.GenerateHash() : idempotencyKey;

            if (this._useReuseRequisitionHash)
            {
                var snapshot = this._cacheProvider.Get<PipelineSnapshot>(hash).Result;
                if (snapshot != null)
                {
                    if (snapshot.Success)
                    {
                        result = snapshot.Context.Response;
                        result.SetStatusCode(200);
                        return result;
                    }
                    else
                    {
                        context = (TContext)snapshot.Context;
                        context.Request = request;
                        nextRequestHandlerId = snapshot.LastRequestHandlerId;
                        this._requestHandler.UpdateContext(context);
                    }
                }
            }

            lastRequestHandlerId = Execute(request, nextRequestHandlerId, ref result);

            if (this._useReuseRequisitionHash)
            {
                var sucess = result?.IsSuccess() ?? false;
                var snapshot = new PipelineSnapshot(sucess,
                    lastRequestHandlerId,
                    this._requestHandler.Context);

                this._cacheProvider.Add<PipelineSnapshot>(snapshot, hash);
            }
            return result;
        }

        private string Execute<TRequest>(TRequest request, string nextRequestHandlerId, ref RequestHandlerResult result)
        {
            string lastRequestHandlerId;
            try
            {
                this._requestHandler.Context.Request = request;

                result = RequestHandlerOrchestrator
                    .ExecuteHandler((RequestHandler<TContext>)this._requestHandler, nextRequestHandlerId);
            }
            catch (PipelinePolicyException px)
            {
                result = px.Result;
            }
            catch (Exception ex)
            {
                if (Log.Logger != null)
                {
                    using (LogContext.PushProperty("RequestKey", this._requestKey))
                    {
                        Log.Logger.Error(ex, string.Concat("Error - ", this._requestHandler.Context.CurrentRequestHandlerId));
                    }
                }
            }
            finally
            {
                lastRequestHandlerId = this._requestHandler.Context.CurrentRequestHandlerId;
                result = ExecuteFinallyHandler() ?? result;
            }

            return lastRequestHandlerId;
        }

        private RequestHandlerResult ExecuteFinallyHandler()
        {
            RequestHandlerResult result = null;

            if (this._finallyRequestHandler != null)
            {
                result = ((RequestHandler<TContext>)this._finallyRequestHandler).Execute();
            }

            return result;
        }

        private static IRequestHandler<TContext> GetLastRequestHandler(IRequestHandler<TContext> requestHandler)
        {
            if (requestHandler.NextRequestHandler != null)
            {
                return GetLastRequestHandler(requestHandler.NextRequestHandler);
            }

            return requestHandler;
        }

        internal void ExecuteRollback(int rollbackIndex)
        {
            //foreach (var rollbackHandler in this._rollbacks.Where(rollbackHandler => rollbackHandler.Index <= rollbackIndex))
            //{
            //    rollbackHandler.Execute(request);
            //}
        }        
    }
}