using System;

namespace PipelineR.StartingSample.Worklows.Bank.AccountSteps
{
    public class DepositAccountStep : RequestHandler<BankContext>, IDepositAccountStep
    {
        public DepositAccountStep(BankContext ctx) : base(ctx)
        {
        }

        public override RequestHandlerResult HandleRequest()
        {
            Console.WriteLine("DepositAccountStep");
            
            return this.Next();
        }
    }

    public interface IDepositAccountStep : IRequestHandler<BankContext>
    {
    }
}