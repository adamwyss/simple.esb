using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

namespace simple.esb
{
    public class MessageRouter
    {
        private static readonly Dictionary<Type, Dictionary<Type, DataLookupInfo>> _stateMappingInfo = new Dictionary<Type, Dictionary<Type, DataLookupInfo>>();

        private readonly IServiceProvider _services;
        private readonly IHandlerProvider _handlers;
        private readonly IDataStore _dataStore;

        public MessageRouter(IServiceProvider services, IHandlerProvider handlers, IDataStore store)
        {
            _services = services;
            _handlers = handlers;
            _dataStore = store;
        }

        public async Task RouteToHandlers(object messageObject)
        {
            var messageType = messageObject.GetType();
            Type[] typeArgs = { messageType };
            var messageHandler = typeof(IHandle<>).MakeGenericType(typeArgs);

            MethodInfo method = messageHandler.GetMethods().SingleOrDefault(m => ForHandleMethodForType(m, messageType));
            if (method != null)
            {
                var list = _handlers.GetHandlers(messageHandler, messageObject);      
                foreach (var handler in list)
                {
                    await InvokeHandle(handler, method, messageObject);
                }
            }
        }

        public async Task InvokeHandle(object handler, MethodInfo method, object message)
        {
            var so = new StatefulObject(handler, _dataStore, message, _stateMappingInfo);

            // creates the data object if it doesn't exist.
            so.Initialize(false);

            await so.LockData();
            try
            {
                // looks up the data object and assigns it to the saga if possible
                so.Initialize(true);

                try
                {
                    await (Task)method.Invoke(handler, new object[] { message });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("HANDLER CRASHED WITH EXCEPTION: {0}", ex);

                    var exceptionHandler = handler as IHandleAnyException;
                    if (exceptionHandler != null)
                    {
                        exceptionHandler.OnHandlerException(message, ex);
                    }
                    else
                    {
                        // TODO: call unhandled exception handler.
                    }
                }

                so.SaveToStore();
            }
            finally
            {
                so.UnlockData();
            }
            
            // the saga has been completed, no longer need state data.
            Saga x = handler as Saga;
            if (x != null && x.Completed)
            {
                await so.LockData();
                try
                {        
                    so.RemoveFromStore();
                }
                finally
                {
                    so.UnlockData();
                }
            }
        }

        public static bool IsGenericHandler(Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IHandle<>);
        }

        private static bool ForHandleMethodForType(MethodInfo method, Type argumentType)
        {
            if (method.Name != "Handle")
            {
                return false;
            }

            var arguments = method.GetParameters().ToArray();
            if (arguments.Length != 1)
            {
                return false;
            }

            var argument = arguments.Single();
            if (argument.ParameterType != argumentType)
            {
                return false;
            }

            return true;
        }
    }

    public class StatefulObject
    {
        private readonly IDataStore _dataStore;

        private readonly object _theObject;
        private readonly Type _theObjectType;

        private PropertyInfo _theDataProperty;
        private Type _theDataType;

        private object _message;

        private readonly Dictionary<Type, Dictionary<Type, DataLookupInfo>> _mapping;

        private IDisposable _lockObject;

        public StatefulObject(object theObject, IDataStore store, object message, Dictionary<Type, Dictionary<Type, DataLookupInfo>> mapping)
        {
            _theObject = theObject;
            _theObjectType = theObject.GetType();
            _message = message;
            _dataStore = store;

            _mapping = mapping;
        }

        public async Task LockData()
        {
            if (_lockObject != null)
            {
                throw new InvalidOperationException("lock already exists.");
            }

            DataLookupInfo info;
            object value;
            object dataInstanceValue;

            if (TryGetMappings(out info, out value, out dataInstanceValue))
            {
                _lockObject = await _dataStore.LockResource(info.DataProperty, value);
            }
            else
            {
                // no persistant state mappings on this object apparently...  we will let it go.
            }
        }

        public void UnlockData()
        {
            if (_lockObject != null)
            {
                _lockObject.Dispose();
                _lockObject = null;
            }
        }

        public void Initialize(bool assignToSaga)
        {
            PropertyInfo dataProperty = _theObjectType.GetProperties().SingleOrDefault(p => p.Name == "StateData");
            if (dataProperty == null)
            {
                return;
            }

            Type dataType = dataProperty.PropertyType;
            Type haveStateInterface = _theObjectType.GetGenericInterfaces(typeof(IHaveState<>))
                                                    .WithSingleArgument(dataType);
            if (haveStateInterface == null)
            {
                return;
            }
            
            Dictionary<Type, DataLookupInfo> lookup;
            bool success = _mapping.TryGetValue(_theObjectType, out lookup);
            if (!success)
            {
                lookup = new Dictionary<Type, DataLookupInfo>();

                MethodInfo method = _theObjectType.GetMethods().SingleOrDefault(m => ForConfigureMappingMethodForType(m, dataType));
                if (method != null)
                {
                    var mappingType = typeof(StateDataMapper<>).MakeGenericType(new[] { dataType });
                    object mappingInstance = Activator.CreateInstance(mappingType, new[] { lookup });

                    method.Invoke(_theObject, new object[] { mappingInstance });

                    _mapping.Add(_theObjectType, lookup);
                }
            }

            object dataInstance = null;

            Type messageType = _message.GetType();
            object value = null;

            DataLookupInfo info;
            bool success2 = lookup.TryGetValue(messageType, out info);
            if (success2)
            {
                value = messageType.GetProperty(info.MessageProperty).GetValue(_message, null);

                dataInstance = _dataStore.Get(info.DataProperty, value);
            }
            
            if (info == null)
            {
                throw new Exception("SAGA " + _theObjectType.Name + " has no mapping data for message " + messageType.Name);
            }

            if (dataInstance == null)
            {
                var startedByDeclaration = typeof(IStartedBy<>).MakeGenericType(messageType);
                bool startedBy = startedByDeclaration.IsAssignableFrom(_theObjectType);
                if (!startedBy)
                {
                    // TODO: is this the right use for IStartedBy?  should a mapping always be required?
                    throw new InvalidOperationException($"No mapping exists for {messageType} in {_theObjectType} and this message cannot start a saga.");
                }

                // no statedata existed, lets create a new one.
                dataInstance = Activator.CreateInstance(dataType);

                dataType.GetProperty(info.DataProperty).SetValue(dataInstance, value);

                // save it back to the store.
                _dataStore.Save(info.DataProperty, value, dataInstance);
            }

            if (assignToSaga)
            {
                dataProperty.SetValue(_theObject, dataInstance);
            }
        }

        public void RemoveFromStore()
        {
            DataLookupInfo info;
            object value;
            object dataInstanceValue;

            if (TryGetMappings(out info, out value, out dataInstanceValue))
            {
                _dataStore.Delete(info.DataProperty, value);
            }
        }

        public void SaveToStore()
        {
            DataLookupInfo info;
            object value;
            object dataInstanceValue;

            if (TryGetMappings(out info, out value, out dataInstanceValue))
            {
                _dataStore.Save(info.DataProperty, value, dataInstanceValue);
            }
        }


        private bool TryGetMappings(out DataLookupInfo info, out object value, out object dataInstanceValue)
        {
            info = null;
            value = null;
            dataInstanceValue = null;

            PropertyInfo dataProperty = _theObjectType.GetProperties().SingleOrDefault(p => p.Name == "StateData");
            if (dataProperty == null)
            {
                return false;
            }

            Type dataType = dataProperty.PropertyType;
            Type haveStateInterface = _theObjectType.GetGenericInterfaces(typeof(IHaveState<>))
                                                    .WithSingleArgument(dataType);
            if (haveStateInterface == null)
            {
                return false;
            }

            dataInstanceValue = dataProperty.GetValue(_theObject, null);

            Dictionary<Type, DataLookupInfo> lookup;
            bool success = _mapping.TryGetValue(_theObjectType, out lookup);
            if (!success)
            {
                throw new InvalidOperationException("the mapping table should be filed out by now.");
            }

            Type messageType = _message.GetType();
            
            bool success2 = lookup.TryGetValue(messageType, out info);
            if (success2)
            {
                value = messageType.GetProperty(info.MessageProperty).GetValue(_message, null);
            }

            return true;
        }


        private static bool ForConfigureMappingMethodForType(MethodInfo method, Type argumentType)
        {
            if (method.Name != "ConfigureStateDataMapping")
            {
                return false;
            }

            var arguments = method.GetParameters().ToArray();
            if (arguments.Length != 1)
            {
                return false;
            }

            var argument = arguments.Single();
            var parameter = argument.ParameterType;
            if (!ReflectionHelper.ImplementsGenericInterface(parameter, typeof(StateDataMapper<>)))
            {
                return false;
            }

            var genericParameterTypes = parameter.GenericTypeArguments;
            if (genericParameterTypes.Length != 1)
            {
                return false;
            }

            var genericParameterArg = genericParameterTypes.Single();
            if (genericParameterArg != argumentType)
            {
                return false;
            }

            return true;
        }
    }

    [DebuggerDisplay("MapInfo: Message.{MessageProperty} = Data.{DataProperty}")]
    public class DataLookupInfo
    {
        public string MessageProperty { get; set; }
        public string DataProperty { get; set; }
    }

    public static class ReflectionHelper
    {
        public static IEnumerable<Type> GetGenericInterfaces(this Type type, Type genericType)
        {
            return type.GetInterfaces().Where(i => i.ImplementsGenericInterface(genericType)).ToList();
        }

        public static bool ImplementsGenericInterface(this Type type, Type genericType)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == genericType;
        }

        public static Type WithSingleArgument(this IEnumerable<Type> interfaces, Type argument)
        {
            foreach (Type i in interfaces)
            {
                Type[] args = i.GetGenericArguments();
                if (args.Length == 1 && args[0] == argument)
                {
                    return i;
                }
            }

            return null;
        }
    }
}
