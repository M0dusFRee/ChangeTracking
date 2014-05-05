﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeTracking
{
    public interface IChangeTrackableCollection<T>
    {
        IEnumerable<T> UnchangedItems { get; }        
        IEnumerable<T> AddedItems { get; }
        IEnumerable<T> ChangedItems { get; }
        IEnumerable<T> DeletedItems { get; }
    }
}