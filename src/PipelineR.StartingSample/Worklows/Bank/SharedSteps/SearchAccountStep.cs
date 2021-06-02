using System;

namespace PipelineR.StartingSample.Worklows.Bank.SharedSteps
{
    public class SearchAccountStep : RequestHandler<BankContext>, ISearchAccountStep
    {
        public SearchAccountStep(BankContext ctx) : base(ctx)
        {
        }

        public override RequestHandlerResult HandleRequest()
        {
            Console.WriteLine("SearchAccountStep");

            return this.Next();
        }
    }

    public interface ISearchAccountStep : IRequestHandler<BankContext>
    {
    }
}