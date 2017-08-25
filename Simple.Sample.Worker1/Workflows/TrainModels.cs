using simple.esb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WCommon.Messages;

namespace Worker.Workflows
{
    #region Workflow

    public class TrainModelsData
    {
        public Guid AgentId { get; set; }
        public bool IntentClassifierCompleted { get; set; }
        public bool EntityRecognizerCompleted { get; set; }
    }

    public class TrainModelsWorkflow : Saga.UsingData<TrainModelsData>,
                                       IStartedBy<TrainModels>,
                                       IHandle<CheckIntentClassifier>,
                                       IHandle<CheckEntityRecognizer>,
                                       IHandle<IntentClassifierTrained>,
                                       IHandle<EntityRecognizerTrained>
    {
        private IServiceBus _bus;

        public TrainModelsWorkflow(IServiceBus bus)
        {
            _bus = bus;
        }

        public override void ConfigureStateDataMapping(StateDataMapper<TrainModelsData> mapper)
        {
            mapper.FromMessage<TrainModels>(m => m.Id).ToData(d => d.AgentId);
            mapper.FromMessage<CheckIntentClassifier>(m => m.Id).ToData(d => d.AgentId);
            mapper.FromMessage<CheckEntityRecognizer>(m => m.Id).ToData(d => d.AgentId);
            mapper.FromMessage<IntentClassifierTrained>(m => m.Id).ToData(d => d.AgentId);
            mapper.FromMessage<EntityRecognizerTrained>(m => m.Id).ToData(d => d.AgentId);
        }

        public Task Handle(TrainModels message)
        {
            Console.WriteLine(" 1) get model information from agent");
            Console.WriteLine(" 2) call to train intent classifier");
            Console.WriteLine(" 3) call to train entity recognizer");

            _bus.Send(new CheckIntentClassifier() { Id = message.Id });
            _bus.Send(new CheckEntityRecognizer() { Id = message.Id });

            return Task.CompletedTask;
        }

        public Task Handle(CheckIntentClassifier message)
        {
            Console.WriteLine(" 4a) check classifier training status");
            var r = new Random();
            bool complete = r.Next() % 3 != 0;

            if (!complete)
            {
                Console.WriteLine(" 4a) classifier training pending");
                _bus.Retry(message, TimeSpan.FromSeconds(2));
            }
            else
            {
                Console.WriteLine(" 4a) classifier training completed");
                _bus.Send(new IntentClassifierTrained() { Id = message.Id });
            }

            return Task.CompletedTask;
        }

        public Task Handle(CheckEntityRecognizer message)
        {
            Console.WriteLine(" 4b) check entity recognizer training status");
            var r = new Random();
            bool complete = r.Next() % 3 != 0;

            if (!complete)
            {
                Console.WriteLine(" 4b) entity recognizer training pending");
                _bus.Retry(message, TimeSpan.FromSeconds(2));
            }
            else
            {
                Console.WriteLine(" 4b) entity recognizer training completed");
                _bus.Send(new EntityRecognizerTrained() { Id = message.Id });
            }

            return Task.CompletedTask;
        }

        public Task Handle(IntentClassifierTrained message)
        {
            Console.WriteLine(" 5a) checking for entity recognizer completion");

            StateData.IntentClassifierCompleted = true;
            CheckForCompletion(message.Id);
            return Task.CompletedTask;
        }

        public Task Handle(EntityRecognizerTrained message)
        {
            Console.WriteLine(" 5b) checking for classifier completion");

            StateData.EntityRecognizerCompleted = true;
            CheckForCompletion(message.Id);
            return Task.CompletedTask;
        }

        private void CheckForCompletion(Guid id)
        {
            if (StateData.IntentClassifierCompleted &&
                StateData.EntityRecognizerCompleted)
            {
                Console.WriteLine(" 6) Training completed");

                _bus.Send(new ModelsTrained() { Id = id });
                MarkAsCompleted();
            }
        }
    }

    #endregion
}
