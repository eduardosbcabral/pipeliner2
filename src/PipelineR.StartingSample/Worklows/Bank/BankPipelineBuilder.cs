using PipelineR.StartingSample.Models;
using PipelineR.StartingSample.Worklows.Bank.AccountSteps;
using PipelineR.StartingSample.Worklows.Bank.SharedSteps;

namespace PipelineR.StartingSample.Worklows.Bank
{
    public class BankPipelineBuilder : IBankPipelineBuilder
    {
        public IPipelineStarting<BankContext> Pipeline { get; }

        public BankPipelineBuilder(IPipelineStarting<BankContext> pipeline)
        {
            Pipeline = pipeline;
        }

        public RequestHandlerResult CreateAccount(CreateAccountModel model)
        {
            return Pipeline
                        .Start()
                        .AddNext<ISearchAccountStep>()
                            .When(b => b.Id == "bla")
                        .AddNext<ISearchAccountStep>()
                        .AddNext<ICreateAccountStep>()
                        .Execute(model);
        }

        public RequestHandlerResult DepositAccount(DepositAccountModel model)
        {
            return Pipeline
                        .Start()
                        .AddNext<ISearchAccountStep>()
                        .AddNext<ISearchAccountStep>()
                        .AddNext<IDepositAccountStep>()
                        .Execute(model);
        }
    }

    public interface IBankPipelineBuilder : IPipelineBuilder<BankContext>
    {
        RequestHandlerResult CreateAccount(CreateAccountModel model);
        RequestHandlerResult DepositAccount(DepositAccountModel model);
    }
}