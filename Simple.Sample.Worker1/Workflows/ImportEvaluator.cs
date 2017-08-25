using Microsoft.Extensions.DependencyInjection;
using simple.esb;
using System;
using System.Threading.Tasks;
using WCommon.Messages;

namespace Worker
{
    #region Workflow

    public class ImportData
    {
        public Guid ImportId { get; set; }
        public bool Staged1 { get; set; }
        public bool Staged2 { get; set; }
        public bool Staged3 { get; set; }
        public bool Staged4 { get; set; }
        public bool Staged5 { get; set; }
        public bool DataMerge { get; set; }
    }

    public class ImportEvaluatorWorkflow : Saga.UsingData<ImportData>,
                                           IStartedBy<ImportEvaluatorData>,
                                           IHandle<Table1Staged>,
                                           IHandle<Table2Staged>,
                                           IHandle<Table3Staged>,
                                           IHandle<Table4Staged>,
                                           IHandle<Table5Staged>,
                                           IHandle<StagedDataMerged>
    {
        private readonly IServiceBus _bus;

        public ImportEvaluatorWorkflow(IServiceBus bus)
        {
            _bus = bus;
        }

        public override void ConfigureStateDataMapping(StateDataMapper<ImportData> map)
        {
            map.FromMessage<ImportEvaluatorData>(m => m.ImportId).ToData(d => d.ImportId);
            map.FromMessage<Table1Staged>(m => m.ImportId).ToData(d => d.ImportId);
            map.FromMessage<Table2Staged>(m => m.ImportId).ToData(d => d.ImportId);
            map.FromMessage<Table3Staged>(m => m.ImportId).ToData(d => d.ImportId);
            map.FromMessage<Table4Staged>(m => m.ImportId).ToData(d => d.ImportId);
            map.FromMessage<Table5Staged>(m => m.ImportId).ToData(d => d.ImportId);
            map.FromMessage<StagedDataMerged>(m => m.ImportId).ToData(d => d.ImportId);
        }

        public Task Handle(ImportEvaluatorData message)
        {
            StateData.ImportId = message.ImportId;

            _bus.Send(new StageTable1() { ImportId = message.ImportId });
            _bus.Send(new StageTable2() { ImportId = message.ImportId });
            _bus.Send(new StageTable3() { ImportId = message.ImportId });
            _bus.Send(new StageTable4() { ImportId = message.ImportId });
            _bus.Send(new StageTable5() { ImportId = message.ImportId });

            return Task.CompletedTask;
        }

        public async Task Handle(Table1Staged message)
        {
            Console.WriteLine("Table1Staged");

            await Task.Delay(TimeSpan.FromSeconds(10));

            StateData.Staged1 = true;
            HandleTableStagedEvent(message.ImportId, message.Success);
            //return Task.CompletedTask;
        }

        public Task Handle(Table2Staged message)
        {
            Console.WriteLine("Table2Staged");

            StateData.Staged2 = true;
            HandleTableStagedEvent(message.ImportId, message.Success);
            return Task.CompletedTask;
        }

        public Task Handle(Table3Staged message)
        {
            Console.WriteLine("Table3Staged");

            StateData.Staged3 = true;
            HandleTableStagedEvent(message.ImportId, message.Success);
            return Task.CompletedTask;
        }

        public Task Handle(Table4Staged message)
        {
            Console.WriteLine("Table4Staged");

            StateData.Staged4 = true;
            HandleTableStagedEvent(message.ImportId, message.Success);
            return Task.CompletedTask;
        }

        public Task Handle(Table5Staged message)
        {
            Console.WriteLine("Table5Staged");

            StateData.Staged5 = true;
            HandleTableStagedEvent(message.ImportId, message.Success);
            return Task.CompletedTask;
        }

        public Task Handle(StagedDataMerged message)
        {
            Console.WriteLine("StagedDataMerged");

            MarkAsCompleted();
            _bus.Send(new EvaluatorDataImported() { ImportId = message.ImportId });
            return Task.CompletedTask;
        }

        private void HandleTableStagedEvent(Guid importId, bool taskSucceeded)
        {
            if (!taskSucceeded)
            {
                // we are done, all other messages will be discarded.
                MarkAsCompleted();
                return;
            }

            if (StateData.Staged1 &&
                StateData.Staged2 &&
                StateData.Staged3 &&
                StateData.Staged4 &&
                StateData.Staged5)
            {
                _bus.Send(new MergeStagedData() { ImportId = importId });
            }
        }
    }

    #endregion

    #region Tasks

    public class StageTable1WorkflowTask : IHandle<StageTable1>
    {
        private readonly IServiceBus _bus;

        public StageTable1WorkflowTask(IServiceBus bus)
        {
            _bus = bus;
        }

        public async Task Handle(StageTable1 message)
        {
            Console.WriteLine("StageTable1");

            await Task.Delay(15000);
            _bus.Send(new Table1Staged() { ImportId = message.ImportId, Success = true });
        }
    }

    public class StageTable2WorkflowTask : IHandle<StageTable2>
    {
        private readonly IServiceBus _bus;

        public StageTable2WorkflowTask(IServiceBus bus)
        {
            _bus = bus;
        }

        public async Task Handle(StageTable2 message)
        {
            Console.WriteLine("StageTable2");

            await Task.Delay(15000);
            _bus.Send(new Table2Staged() { ImportId = message.ImportId, Success = true });
        }
    }

    public class StageTable3WorkflowTask : IHandle<StageTable3>
    {
        private readonly IServiceBus _bus;

        public StageTable3WorkflowTask(IServiceBus bus)
        {
            _bus = bus;
        }

        public async Task Handle(StageTable3 message)
        {
            Console.WriteLine("StageTable3");

            await Task.Delay(15000);
            _bus.Send(new Table3Staged() { ImportId = message.ImportId, Success = true });
        }
    }

    public class StageTable4WorkflowTask : IHandle<StageTable4>
    {
        private readonly IServiceBus _bus;

        public StageTable4WorkflowTask(IServiceBus bus)
        {
            _bus = bus;
        }

        public async Task Handle(StageTable4 message)
        {
            Console.WriteLine("StageTable4");

            await Task.Delay(15000);
            _bus.Send(new Table4Staged() { ImportId = message.ImportId, Success = true });
        }
    }

    public class StageTable5WorkflowTask : IHandle<StageTable5>
    {
        private readonly IServiceBus _bus;

        public StageTable5WorkflowTask(IServiceBus bus)
        {
            _bus = bus;
        }

        public async Task Handle(StageTable5 message)
        {
            Console.WriteLine("StageTable5");

            await Task.Delay(15000);
            _bus.Send(new Table5Staged() { ImportId = message.ImportId, Success = true });
        }
    }

    public class MergeStagedDataWorkflowTask : IHandle<MergeStagedData>
    {
        private readonly IServiceBus _bus;

        public MergeStagedDataWorkflowTask(IServiceBus bus)
        {
            _bus = bus;
        }

        public async Task Handle(MergeStagedData message)
        {
            Console.WriteLine("MergeStagedData");

            await Task.Delay(5000);
            _bus.Send(new StagedDataMerged() { ImportId = message.ImportId, Success = true });
        }
    }

    #endregion
}
