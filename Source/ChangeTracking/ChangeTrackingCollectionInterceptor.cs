﻿using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    internal sealed class ChangeTrackingCollectionInterceptor<T> : IInterceptor, IChangeTrackableCollection<T> where T : class
    {
        private ChangeTrackingBindingList<T> _WrappedTarget;
        private IList<T> _DeletedItems;
        private static HashSet<string> _ImplementedMethods;
        private static HashSet<string> _BindingListImplementedMethods;
        private static HashSet<string> _IBindingListImplementedMethods;

        static ChangeTrackingCollectionInterceptor()
        {
            _ImplementedMethods = new HashSet<string>(typeof(ChangeTrackingCollectionInterceptor<T>).GetMethods(BindingFlags.Instance | BindingFlags.Public).Select(m => m.Name));
            _BindingListImplementedMethods = new HashSet<string>(typeof(ChangeTrackingBindingList<T>).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).Select(m => m.Name));
            _IBindingListImplementedMethods = new HashSet<string>(typeof(ChangeTrackingBindingList<T>).GetInterfaceMap(typeof(System.ComponentModel.IBindingList)).TargetMethods.Where(mi => mi.IsPrivate).Select(mi => mi.Name.Substring(mi.Name.LastIndexOf('.') + 1)));
        }

        public ChangeTrackingCollectionInterceptor(IList<T> target)
        {
            for (int i = 0; i < target.Count; i++)
            {
                target[i] = target[i].AsTrackable(notifyParentItemCanceled: ItemCanceled);
            }
            _WrappedTarget = new ChangeTrackingBindingList<T>(target, DeleteItem, ItemCanceled);
            _DeletedItems = new List<T>();
        }

        public void Intercept(IInvocation invocation)
        {
            if (_ImplementedMethods.Contains(invocation.Method.Name))
            {
                invocation.ReturnValue = invocation.Method.Invoke(this, invocation.Arguments);
                return;
            }
            if (_BindingListImplementedMethods.Contains(invocation.Method.Name))
            {
                invocation.ReturnValue = invocation.Method.Invoke(_WrappedTarget, invocation.Arguments);
                return;
            }
            if (_IBindingListImplementedMethods.Contains(invocation.Method.Name))
            {
            	invocation.ReturnValue = invocation.Method.Invoke(_WrappedTarget, invocation.Arguments);
                return;
            }
            invocation.Proceed();
        }

        private void DeleteItem(T item)
        {
            var currentStatus = item.CastToIChangeTrackable().ChangeTrackingStatus;
            var manager = (IChangeTrackingManager<T>)item;
            manager.Delete();
            if (currentStatus != ChangeStatus.Added)
            {
                _DeletedItems.Add(item);
            }
        }
      
        private void ItemCanceled(T item)
        {
            _WrappedTarget.CancelNew(_WrappedTarget.IndexOf(item));
        }

        public IEnumerable<T> UnchangedItems
        {
            get { return _WrappedTarget.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Unchanged).Cast<T>(); }
        }

        public IEnumerable<T> AddedItems
        {
            get { return _WrappedTarget.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Added).Cast<T>(); }
        }

        public IEnumerable<T> ChangedItems
        {
            get { return _WrappedTarget.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Changed).Cast<T>(); }
        }

        public IEnumerable<T> DeletedItems
        {
            get { return _DeletedItems.Select(i => i); }
        }
    }
}