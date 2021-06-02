using DiagramBuilder.Html;
using System.Collections.Generic;
using System.Linq;

namespace PipelineR
{
    public class DocumentationBuilder
    {
        private readonly IList<HtmlCustomDiagram> _diagrams;

        public DocumentationBuilder()
        {
            _diagrams = new List<HtmlCustomDiagram>();
        }

        public void AddDiagram(HtmlCustomDiagram customDiagram)
        {
            _diagrams.Add(customDiagram);
        }

        public void Compile()
        {
            var path = @"C:\Users\Yuri Pereira\source\doc2";
            new HtmlBuilder().BuildDocumentation(path, _diagrams.ToArray());
        }
    }
}