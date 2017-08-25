using simple.esb;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WCommon.Messages
{
    public class PublishAgent
    {
        public Guid Id { get; set; }
    }

    public class AgentPublished
    {
        public Guid Id { get; set; }
    }

    #region Messages

    public class CreateCorpus
    {
        public Guid Id { get; set; }
    }

    public class CorpusCreated
    {
        public Guid Id { get; set; }
        public bool Success { get; set; }
    }

    public class TrainIntentClassifierModel
    {
        public Guid Id { get; set; }
    }

    public class IntentClassifierModelTrained
    {
        public Guid Id { get; set; }
        public bool Success { get; set; }
    }

    public class TrainEntityRecognizerModel
    {
        public Guid Id { get; set; }
    }

    public class EntityRecognizerModelTrained
    {
        public Guid Id { get; set; }
        public bool Success { get; set; }
    }

    public class CreateDeployment
    {
        public Guid Id { get; set; }
    }

    public class DeploymentCreated
    {
        public Guid Id { get; set; }
        public bool Success { get; set; }
    }

    #endregion

}
