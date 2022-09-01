using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Async;
using Core.LowLevel;
using System.Reflection;

namespace Core
{
    /// <summary>
    /// Содержит, управляет всеми сервисами
    /// </summary>
    public class ServiceClient : UnitySingleton<ServiceClient>, ISupportAsyncCall
    {
        [SerializeField] private bool syncLoadServices = false;

        private List<IService> services = new List<IService>();

        private List<IServiceUpdate> reqUpdate = new List<IServiceUpdate>();
        private List<IServiceAsyncUpdate> reqAsyncUpdate = new List<IServiceAsyncUpdate>();

        private UnitySyncTimer delayTimer = new UnitySyncTimer(0.05f) { IsLooped = true };

        bool ISupportAsyncCall.enabledUpdate => true;

        #region not supported
        void ISupportAsyncCall.AsyncLateUpdate(TimeSpan updateDelay)
        {
            throw new NotSupportedException();
        }
        bool ISupportAsyncCall.enabledLateUpdate => false;
        #endregion

        public void RegisterNew(IService service)
        {
            if (service == null)
                return;

            if (SameTypeContains(service.GetType()))
                throw new ArgumentException($"Type {service.GetType().FullName} already contains");
            services.Add(service);
            if (service is IServiceUpdate serviceUpdate)
                reqUpdate.Add(serviceUpdate);
            if(service is IServiceAsyncUpdate serviceAsyncUpdate)
                reqAsyncUpdate.Add(serviceAsyncUpdate);
            service.Initialize();
        }

        public static T GetService<T>() where T : class, IService
        {
            foreach(var sr in Self.services)
            {
                if (sr is T typed)
                    return typed;
            }
            return null;
        }

        public static Task<T> GetServiceAsync<T>() where T : class, IService
        {
            return Task.Run(() => GetService<T>());
        }

        private bool SameTypeContains(Type type)
        {
            foreach(var sr in services)
            {
                if (sr.GetType().IsEquivalentTo(type))
                    return true;
            }

            return false;
        }

        void ISupportAsyncCall.AsyncUpdate(TimeSpan updateDelay)
        {
            var deltaTime = updateDelay.TotalSeconds;
            foreach (var serv in reqAsyncUpdate)
                serv.AsyncUpdate(deltaTime);
        }

        void Update()
        {
            if(delayTimer.Sync(Time.deltaTime))
            {
                double deltaTime = delayTimer.StartTime;
                foreach(var serv in reqUpdate)
                {
                    serv.Update(deltaTime);
                }
            }
        }

        protected override async void SingletonAwake()
        {
            if (syncLoadServices)
            {
                foreach (var serv in LoadedMarkedServices().Result)
                    RegisterNew(serv.Item2);
            }
            else
            {
                foreach (var serv in await LoadedMarkedServices())
                    RegisterNew(serv.Item2);
            }
            UnityEngine.Debug.Log($"Loaded {services.Count} services");
            this.RegisterSelf(ThreadDefinition.servicesTread);
        }
        async Task<List<(int, IService)>> LoadedMarkedServices()
        {
            List<(int,IService)> instances = new List<(int, IService)>();
            var currentAssembly = Assembly.GetExecutingAssembly();
            var attrType = typeof(ServiceAutoLoadAttribute);

            return await Task.Run(() => 
            {
                foreach (var tp in currentAssembly.GetTypes())
                {
                    var attr = tp.GetCustomAttribute(attrType, false) as ServiceAutoLoadAttribute;
                    if (attr != null)
                    {
                        (int, IService) tuple = (attr.Priority, Activator.CreateInstance(tp) as IService);
                        instances.Add(tuple);
                    }
                }
                instances.Sort((x, y) => 
                {
                    if (x.Item1 > y.Item1)
                        return -1;
                    else if (x.Item1 < y.Item1)
                        return 1;
                    else return 0;
                });
                return instances;
            });
            
        }

    }
}
