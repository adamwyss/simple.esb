using simple.esb;
using System;

namespace WCommon.Messages
{
    public class TrainModels
    {
        public Guid Id { get; set; }
    }
    
    public class ModelsTrained
    {
        public Guid Id { get; set; }
    }

    #region Messages

    public class CheckIntentClassifier
    {
        public Guid Id { get; set; }
    }

    public class CheckEntityRecognizer
    {
        public Guid Id { get; set; }
    }

    public class IntentClassifierTrained
    {
        public Guid Id { get; set; }
    }

    public class EntityRecognizerTrained
    {
        public Guid Id { get; set; }
    }

    #endregion
}
