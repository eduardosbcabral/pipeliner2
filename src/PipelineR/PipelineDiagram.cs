using ClassStructureJson;
using DiagramBuilder.Flowcharts;
using DiagramBuilder.Html;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace PipelineR
{
    public class PipelineDiagram<TContext> : IPipeline<TContext> where TContext : BaseContext
    {
        private readonly DocumentationBuilder _builder;
        private readonly Flowchart _flowchart;
        private readonly IList<NodeR> _nodes;
        //private Node _lastNode;
        //private Node _penultNode;

        public PipelineDiagram()
        {
            _builder = AutoInject.ServiceProvider.GetService<DocumentationBuilder>();
            _flowchart = new Flowchart("Teste");
            _nodes = new List<NodeR>();
        }

        public IPipeline<TContext> AddFinally(IRequestHandler<TContext> requestHandler)
        {
            throw new NotImplementedException();
        }

        public IPipeline<TContext> AddFinally<TStepHandler>()
        {
            throw new NotImplementedException();
        }

        public IPipeline<TContext> AddNext<TRequestHandler>()
        {
            var requestHandler = (IRequestHandler<TContext>)AutoInject.ServiceProvider.GetService<TRequestHandler>();
            return AddNext(requestHandler);
        }

        public IPipeline<TContext> AddNext(IRequestHandler<TContext> requestHandler)
        {
            var node = new Node(requestHandler.GetType().Name);
            AddNodeR(node, NodeType.Next);
            return this;
        }

        public IPipeline<TContext> AddValidator<TRequest>(FluentValidation.IValidator<TRequest> validator) where TRequest : class
        {
            throw new NotImplementedException();
        }

        public IPipeline<TContext> AddValidator<TRequest>() where TRequest : class
        {
            throw new NotImplementedException();
        }

        public RequestHandlerResult Execute<TRequest>(TRequest request) where TRequest : class
        {
            return Execute(request, string.Empty);
        }

        public RequestHandlerResult Execute<TRequest>(TRequest request, string idempotencyKey) where TRequest : class
        {
            ProcessNodes();

            var customDiagram = new HtmlCustomDiagram(_flowchart);
            customDiagram.AddPreClassDiagram(new HtmlClassDiagram("Request", request));
            _builder.AddDiagram(customDiagram);

            return null;
        }

        public IPipeline<TContext> When(Expression<Func<TContext, bool>> func)
        {
            var expressionBody = ExpressionBody(func);
            AddNodeR(new Node(expressionBody, NodeShapes.Rhombus), NodeType.When);
            return this;
        }

        public IPipeline<TContext> When<TCondition>()
        {
            throw new NotImplementedException();
        }

        private string ExpressionBody<T>(Expression<Func<T, bool>> exp)
        {
            string expBody = (exp).Body.ToString();

            var paramName = exp.Parameters[0].Name;
            var paramTypeName = "[Context]"; //exp.Parameters[0].Type.Name;

            expBody = expBody.Replace(paramName + ".", paramTypeName + ".")
                         .Replace("\"", "#quot;")
                         .Replace("AndAlso", "&&").Replace("OrElse", "||");

            return $"\"{expBody}\"";
        }

        private void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        private void ProcessNodes()
        {
            foreach (var nodeR in _nodes.Where(n => n.Type != NodeType.When))
            {
                if (HasNextNodeR(nodeR) == false)
                    continue;

                var nextNodeR = NextNodeR(nodeR);

                var node = nodeR.Node;
                var nextNode = nextNodeR.Node;

                if (nextNodeR.Type == NodeType.When)
                {
                    _flowchart.Connect(nextNode)
                               .With(node, "Sim", NodeLinkType.DottedLineArrow);

                    if (HasNextNodeR(nextNodeR))
                    {
                        var next = NextNodeR(nextNodeR);
                        _flowchart.With(next.Node, "Não", NodeLinkType.DottedLineArrow);
                        _flowchart.Connect(node)
                                    .With(next.Node);
                    }
                }
                else
                {
                    _flowchart.Connect(node)
                                .With(nextNode);
                }
            }
        }

        private bool HasNextNodeR(NodeR currentNode) => NextNodeR(currentNode) != null;
        private NodeR NextNodeR(NodeR currentNode) 
        {
            var index = _nodes.IndexOf(currentNode);
            var nextNodeIndex = index + 1;

            if (nextNodeIndex < _nodes.Count())
                return _nodes[nextNodeIndex];

            return null;
        }
        private void AddNodeR(Node node, NodeType type) => _nodes.Add(new NodeR(node, type));
    }

    internal class NodeR
    {
        public NodeR(Node node, NodeType type)
        {
            Node = node;
            Type = type;
        }

        public Node Node { get; set; }
        public NodeType Type { get; set; }
    }

    internal enum NodeType
    {
        Next,
        When,
        Finnaly
    }
}