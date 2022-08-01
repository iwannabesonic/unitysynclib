using Core;
using Core.LowLevel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Core
{
    [ServiceAutoLoad(ServiceAutoLoadAttribute.systemPriority)]
    public sealed class VaultItemObservableService : IService, IServiceUpdate
    {
        private Dictionary<VaultItem, VaultItemObservable> observableList = new Dictionary<VaultItem, VaultItemObservable>(64);
        private ServiceAccess serviceAccess; //reflection

        void IService.Initialize()
        {
            serviceAccess = new ServiceAccess(this);
        }

        public void RegisterNewItem(IServiceItem item)
        {
            if(item is VaultItemObservable vio)
            {
                if (observableList.ContainsKey(vio.Source))
                    throw new ArgumentException("Observer for this item already contains");
                else
                {
                    observableList.Add(vio.Source, vio);
                }
            }
        }

        void IServiceUpdate.Update(double deltaTime)
        {
            foreach(var item in observableList)
                (item.Value as IServiceItem).UpdateItem(deltaTime);
        }

        public sealed class ServiceAccess
        {
            private VaultItemObservableService service;
            public VaultItemObservable GetObservableFromItem(VaultItem item)
            {
                if (service.observableList.TryGetValue(item, out var observer))
                    return observer;
                else return null;
            }

            public ServiceAccess(VaultItemObservableService service)
            {
                this.service = service;
            }
        }
    }

    public sealed class VaultItemObservable : IVaultItem, ITagged, IServiceItem
    {
        private double cashedValue;
        private readonly VaultItem source;
        public VaultItem Source => source;

        public event Action<VaultItem> OnValueChangesHandler;

        #region interfaces
        public string key => ((IVaultItem)source).key;

        public double value => ((IVaultItem)source).value;

        public double baseValue => ((IVaultItem)source).baseValue;

        public double addValue { get => ((IVaultItem)source).addValue; set => ((IVaultItem)source).addValue = value; }
        public double multValue { get => ((IVaultItem)source).multValue; set => ((IVaultItem)source).multValue = value; }
        public double rawValue { get => ((IVaultItem)source).rawValue; set => ((IVaultItem)source).rawValue = value; }

        public int Count => ((ITagged)source).Count;

        public void Add(string tag)
        {
            ((ITagged)source).Add(tag);
        }

        public bool Remove(string tag)
        {
            return ((ITagged)source).Remove(tag);
        }

        public bool TagExist(string tag)
        {
            return source.TagExist(tag);
        }

        public IReadOnlyCollection<string> ToList()
        {
            return ((ITagged)source).ToList();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)source).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)source).GetEnumerator();
        }
        #endregion

        void IServiceItem.UpdateItem(double deltaTime)
        {
            if (cashedValue != source.Value)
            {
                cashedValue = source.Value;
                OnValueChangesHandler?.Invoke(source);
            }
        }

        public VaultItemObservable(VaultItem source)
        {
            this.source = source;
            cashedValue = source.Value;

            ServiceClient.GetService<VaultItemObservableService>().RegisterNewItem(this);
            Debug.Log("Created new observalbe");
        }
    }

    public static class VaultItemObservableExtentions
    {
        private static VaultItemObservableService.ServiceAccess serviceAccess;
        public static VaultItemObservable AsObservable(this VaultItem item)
        {
            if(serviceAccess == null)
            {
                serviceAccess = typeof(VaultItemObservableService).GetField("serviceAccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(ServiceClient.GetService<VaultItemObservableService>()) as VaultItemObservableService.ServiceAccess;
            }

            var observer = serviceAccess.GetObservableFromItem(item);
            if(observer == null)
                observer = new VaultItemObservable(item);
            return observer;
        }
    }
}
