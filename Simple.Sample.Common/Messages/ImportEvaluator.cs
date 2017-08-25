using simple.esb;
using System;

namespace WCommon.Messages
{
    public class ImportEvaluatorData
    {
        public Guid ImportId { get; set; }
    }
    
    public class EvaluatorDataImported
    {
        public Guid ImportId { get; set; }
    }

    #region Messages

    public class StageTable1
    {
        public Guid ImportId { get; set; }
    }

    public class Table1Staged
    {
        public Guid ImportId { get; set; }
        public bool Success { get; set; }
    }

    public class StageTable2
    {
        public Guid ImportId { get; set; }
    }

    public class Table2Staged
    {
        public Guid ImportId { get; set; }
        public bool Success { get; set; }
    }

    public class StageTable3
    {
        public Guid ImportId { get; set; }
    }

    public class Table3Staged
    {
        public Guid ImportId { get; set; }
        public bool Success { get; set; }
    }

    public class StageTable4
    {
        public Guid ImportId { get; set; }
    }

    public class Table4Staged
    {
        public Guid ImportId { get; set; }
        public bool Success { get; set; }
    }

    public class StageTable5
    {
        public Guid ImportId { get; set; }
    }

    public class Table5Staged
    {
        public Guid ImportId { get; set; }
        public bool Success { get; set; }
    }

    public class MergeStagedData
    {
        public Guid ImportId { get; set; }
    }

    public class StagedDataMerged
    {
        public Guid ImportId { get; set; }
        public bool Success { get; set; }
    }

    #endregion

}
