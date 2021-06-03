using System.Net;

using WebApi.Models.Response;

namespace PipelineR
{
    internal class RequestHandlerResultBuilder
    {
        private RequestHandlerResult Handler;

        public RequestHandlerResultBuilder()
        {
            this.Reset();
        }

        public void Reset()
            => this.Handler = new RequestHandlerResult();

        public RequestHandlerResultBuilder WithErrors(params ErrorResult[] errors)
        {
            this.Handler.SetErrors(errors);
            return this;
        }

        public RequestHandlerResultBuilder WithResultErrorItems(params ErrorItemResponse[] errors)
        {
            this.Handler.SetResultErrorItems(errors);
            return this;
        }

        public RequestHandlerResultBuilder WithStatusCode(int statusCode)
        {
            this.Handler.StatusCode = statusCode;
            return this;
        }

        public RequestHandlerResultBuilder WithHttpStatusCode(HttpStatusCode statusCode)
        {
            this.Handler.StatusCode = (int)statusCode;
            return this;
        }

        public RequestHandlerResultBuilder WithSuccess()
        {
            this.Handler.SetSucess();
            return this;
        }

        public RequestHandlerResultBuilder WithFailure()
        {
            this.Handler.SetFailure();
            return this;
        }

        public RequestHandlerResultBuilder WithRequestHandlerId(string requestHandlerId)
        {
            this.Handler.RequestHandlerId = requestHandlerId;
            return this;
        }

        public RequestHandlerResultBuilder WithResultObject(object resultObject)
        {
            this.Handler.SetResultObject(resultObject);
            return this;
        }

        public RequestHandlerResultBuilder WithErrorMessage(string errorMessage)
        {
            this.Handler.SetErrorMessage(errorMessage);
            return this;
        }

        public RequestHandlerResult Build()
            => this.Handler;

        public static RequestHandlerResultBuilder Instance()
            => new RequestHandlerResultBuilder();
    }
}
