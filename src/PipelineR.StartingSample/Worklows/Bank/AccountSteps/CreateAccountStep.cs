using System;

namespace PipelineR.StartingSample.Worklows.Bank.AccountSteps
{
    public class CreateAccountStep : RequestHandler<BankContext>, ICreateAccountStep
    {
        public CreateAccountStep(BankContext ctx) : base(ctx)
        {
        }

        public override RequestHandlerResult HandleRequest()
        {
            Console.WriteLine("CreateAccountStep");
            
            return this.Next();
        }
    }

    public interface ICreateAccountStep : IRequestHandler<BankContext>
    {
    }
}