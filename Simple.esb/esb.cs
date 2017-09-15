using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace simple.esb
{
    public interface IServiceBus
    {
        void Send<T>(T message);
        void Retry<T>(T message, TimeSpan delay);
    }

    public interface IHandle<T>
    {
        Task Handle(T message);
    }

    public interface IHandleAnyException
    {
        void OnHandlerException(object message, Exception exception);
    }

    public interface IHaveState<T>
    {
        T StateData { get; }

        void ConfigureStateDataMapping(StateDataMapper<T> mapper);
    }

    public interface IStartedBy<T> : IHandle<T>
    {

    }

    public abstract class Saga
    {
        public bool Completed { get; private set; }

        public void MarkAsCompleted()
        {
            Completed = true;
        }

        public abstract class UsingData<T> : Saga, IHaveState<T>
        {
            public T StateData { get; internal set; }

            public abstract void ConfigureStateDataMapping(StateDataMapper<T> mapper);
        }
    }
    
    public class StateDataMapper<TData>
    {
        private readonly Dictionary<Type, DataLookupInfo> _lookup;

        public StateDataMapper(Dictionary<Type, DataLookupInfo> lookup)
        {
            _lookup = lookup;
        }

        public Mapping<TData, TMessage> FromMessage<TMessage>(Expression<Func<TMessage, object>> fromExpression)
        {
            return new Mapping<TData, TMessage>(this, fromExpression, _lookup);
        }
    }

    public class Mapping<TData, TMessage>
    {
        private readonly StateDataMapper<TData> _mapper;
        private readonly Expression<Func<TMessage, object>> _fromExpression;
        private readonly Dictionary<Type, DataLookupInfo> _lookup;

        internal Mapping(StateDataMapper<TData> mapper, Expression<Func<TMessage, object>> fromExpression, Dictionary<Type, DataLookupInfo> lookup)
        {
            _mapper = mapper;
            _fromExpression = fromExpression;
            _lookup = lookup;
        }

        public void ToData(Expression<Func<TData, object>> toExpression)
        {
            string messageProperty = GetMemberInfo(_fromExpression).Member.Name;
            string dataProperty = GetMemberInfo(toExpression).Member.Name;

            _lookup.Add(typeof(TMessage), new DataLookupInfo { MessageProperty = messageProperty, DataProperty = dataProperty });
        }

        private MemberExpression GetMemberInfo(Expression method)
        {
            LambdaExpression lambda = method as LambdaExpression;
            if (lambda == null)
            {
                throw new ArgumentNullException("method");
            }

            MemberExpression memberExpr = null;

            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                memberExpr = ((UnaryExpression)lambda.Body).Operand as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpr = lambda.Body as MemberExpression;
            }

            if (memberExpr == null)
            {
                throw new ArgumentException("method");
            }

            return memberExpr;
        }
    }













    public interface IDataStore
    {
        object Get(string propertyName, object propertyValue);
        void Save(string propertyName, object propertyValue, object dataInstanceValue);
        void Delete(string propertyName, object propertyValue);

        Task<IDisposable> LockResource(string propertyName, object propertyValue);

    }




    public interface IPreviewMessage
    {
        void Peek(object message);
    }


    public interface IHandlerProvider
    {
        IEnumerable<object> GetHandlers(Type handlerType, object message);
    }

}
