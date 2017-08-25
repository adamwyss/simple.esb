using Microsoft.Extensions.DependencyInjection;
using simple.esb;
using System;
using System.Threading.Tasks;
using WCommon.Messages;

namespace Worker
{
    #region Workflow

    public class PublishAgentState
    {
        public Guid Identifier { get; set; }
        public bool EntityRecognizerCreated { get; set; }
        public bool IntentClassifierCreated { get; set; }
    }

    public class PublishAgentWorkflow : Saga.UsingData<PublishAgentState>,
                                        IStartedBy<PublishAgent>,
                                        IHandle<CorpusCreated>,
                                        IHandle<EntityRecognizerModelTrained>,
                                        IHandle<IntentClassifierModelTrained>,
                                        IHandle<DeploymentCreated>
    {
        private readonly IServiceBus _bus;

        public PublishAgentWorkflow(IServiceBus bus)
        {
            _bus = bus;
        }

        public override void ConfigureStateDataMapping(StateDataMapper<PublishAgentState> map)
        {
            map.FromMessage<PublishAgent>(m => m.Id).ToData(d => d.Identifier);
            map.FromMessage<CorpusCreated>(m => m.Id).ToData(d => d.Identifier);
            map.FromMessage<EntityRecognizerModelTrained>(m => m.Id).ToData(d => d.Identifier);
            map.FromMessage<IntentClassifierModelTrained>(m => m.Id).ToData(d => d.Identifier);
            map.FromMessage<DeploymentCreated>(m => m.Id).ToData(d => d.Identifier);
        }

        public Task Handle(PublishAgent message)
        {
            Console.WriteLine("PublishAgent");

            _bus.Send(new CreateCorpus() { Id = message.Id });
            
            return Task.CompletedTask;
        }

        public Task Handle(CorpusCreated message)
        {
            Console.WriteLine("CorpusCreated");

            _bus.Send(new TrainEntityRecognizerModel() { Id = message.Id });
            _bus.Send(new TrainIntentClassifierModel() { Id = message.Id });

            return Task.CompletedTask;
        }

        public Task Handle(EntityRecognizerModelTrained message)
        {
            Console.WriteLine("EntityRecognizerModelTrained");

            StateData.EntityRecognizerCreated = true;

            if (StateData.IntentClassifierCreated)
            {
                _bus.Send(new CreateDeployment() { Id = message.Id });
            }

            return Task.CompletedTask;
        }

        public Task Handle(IntentClassifierModelTrained message)
        {
            Console.WriteLine("IntentClassifierModelTrained");

            StateData.IntentClassifierCreated = true;

            if (StateData.EntityRecognizerCreated)
            {
                _bus.Send(new CreateDeployment() { Id = message.Id });
            }

            return Task.CompletedTask;
        }

        public Task Handle(DeploymentCreated message)
        {
            Console.WriteLine("DeploymentCreated");

            _bus.Send(new AgentPublished() { Id = message.Id });

            MarkAsCompleted();
            
            return Task.CompletedTask;
        }
    }

    #endregion

    #region Tasks

    public class CreateCorpusWorkflowTask : IHandle<CreateCorpus>
    {
        private readonly IServiceBus _bus;

        public CreateCorpusWorkflowTask(IServiceBus bus)
        {
            _bus = bus;
        }

        public async Task Handle(CreateCorpus message)
        {
            Console.WriteLine("CreateCorpus");

            await Task.Delay(5000);

            _bus.Send(new CorpusCreated() { Id = message.Id, Success = true });
        }
    }

    public class TrainIntentClassifierWorkflowTask : IHandle<TrainIntentClassifierModel>
    {
        private readonly IServiceBus _bus;

        public TrainIntentClassifierWorkflowTask(IServiceBus bus)
        {
            _bus = bus;
        }

        public async Task Handle(TrainIntentClassifierModel message)
        {
            Console.WriteLine("TrainIntentClassifierModel");

            await Task.Delay(5000);

            _bus.Send(new IntentClassifierModelTrained() { Id = message.Id, Success = true });
        }
    }

    public class TrainEntityRecognizerWorkflowTask : IHandle<TrainEntityRecognizerModel>
    {
        private readonly IServiceBus _bus;

        public TrainEntityRecognizerWorkflowTask(IServiceBus bus)
        {
            _bus = bus;
        }

        public async Task Handle(TrainEntityRecognizerModel message)
        {
            Console.WriteLine("TrainEntityRecognizerModel");

            await Task.Delay(5000);

            _bus.Send(new EntityRecognizerModelTrained() { Id = message.Id, Success = true });
        }
    }
    
    public class DeployWorkflowTask : IHandle<CreateDeployment>
    {
        private readonly IServiceBus _bus;

        public DeployWorkflowTask(IServiceBus bus)
        {
            _bus = bus;
        }

        public async Task Handle(CreateDeployment message)
        {
            Console.WriteLine("CreateDeployment");

            await Task.Delay(5000);

            _bus.Send(new DeploymentCreated() { Id = message.Id, Success = true });
        }
    }

    #endregion
}
