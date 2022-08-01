using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public interface IService
    {
        void Initialize();
    }

    public interface IServiceUpdate
    {
        void Update(double deltaTime);
    }

    public interface IServiceAsyncUpdate
    {
        void AsyncUpdate(double deltaTime);
    }

    public interface IServiceItem
    {
        void UpdateItem(double deltaTime);
    }

    public static class IServiceExtentions
    {
        public static T GetService<T>(this IService serv) where T : class, IService => serv as T;
    }

    public abstract class BaseService : IService
    {
        protected abstract void Initialize();
        void IService.Initialize() => Initialize();

        protected BaseService(bool autoRegist)
        {
            ServiceClient.Self.RegisterNew(this);
        }

        public BaseService() { }
    }

    /// <summary>
    /// Помечает что сервис должен автоматически загрузится в клиент
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ServiceAutoLoadAttribute : System.Attribute
    {
        public const int systemPriority = 1000;
        public const int importantPriority = 100;
        public const int defaultPriority = 0;
        public const int lowPriority = -100;
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236

        // This is a positional argument
        public ServiceAutoLoadAttribute()
        {
            Priority = defaultPriority;
        }

        public ServiceAutoLoadAttribute(int priority)
        {
            Priority = priority;
        }

        public int Priority { get; private set; }
    }
}
