using System.Text.Json.Serialization;

namespace PipelineR
{
    /// <summary>
    /// Context class that is shared between the steps of a single or multiple pipelines.
    /// </summary>
    [JsonConverter(typeof(ContextConverter))]
    public abstract class BaseContext
    {
        public BaseContext()
        {
            this.Id = this.ToString();
        }

        public string Id { get; set; }
        public object Request { get; set; }
    
        public RequestHandlerResult Response { get; set; }
        public string CurrentRequestHandlerId { get; set; }
    }
}