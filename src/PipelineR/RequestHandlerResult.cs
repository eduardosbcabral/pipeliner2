using System.Collections.Generic;
using System.Linq;
using System.Net;
using WebApi.Models.Response;

namespace PipelineR
{
    public class RequestHandlerResult
    {
        public RequestHandlerResult()
        { }

        public bool Success { get; private set; }

        public object ResultObject { get; private set; }

        public string RequestHandlerId { get; set; }

        public IList<ErrorResult> Errors { get; private set; }

        public int StatusCode { get; set; }

        public bool IsSuccess() => this.Success;

        public object Result() => this.ResultObject;

        internal void SetStatusCode(int statusCode) 
            => this.StatusCode = statusCode;

        internal void SetErrors(params ErrorResult[] errors)
            => this.Errors = errors;

        internal void SetResultErrorItems(params ErrorItemResponse[] errors)
            => this.SetResultObject(new ErrorsResponse 
            { 
                Errors = errors.ToList()
            });

        internal void SetSucess()
            => this.Success = true;

        internal void SetFailure()
            => this.Success = false;

        internal void SetResultObject(object resultObject)
            => this.ResultObject = resultObject;

        internal void SetErrorMessage(string errorMessage)
            => this.SetErrors(new ErrorResult(errorMessage));
    }
}