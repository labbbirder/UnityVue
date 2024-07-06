using System;
using System.Collections;
using System.Collections.Generic;

namespace BBBirder.UnityVue
{
    public class ScopeCollection : ICollection<WatchScope>
    {
        // 一个Collection通常不会有太多元素，所以使用List实现
        private List<WatchScope> m_List;
        public int Count => m_List.Count;

        public bool IsReadOnly => false;

        public WatchScope this[int index]
        {
            get => m_List[index];
        }
        public ScopeCollection(int Capacity = 4)
        {
            m_List = new(Capacity);
        }

        public void Add(WatchScope scope)
        {
            if (Contains(scope)) return;
            m_List.Add(scope);
        }

        public bool Remove(WatchScope scope)
        {
            return m_List.Remove(scope);
        }

        public bool Contains(WatchScope scope)
        {
            return m_List.Contains(scope);
        }

        public void CopyTo(WatchScope[] array)
        {
            m_List.CopyTo(array);
        }

        public void Clear()
        {
            m_List.Clear();
        }

        public void CopyTo(WatchScope[] array, int arrayIndex)
        {
            m_List.CopyTo(array, arrayIndex);
        }

        public IEnumerator<WatchScope> GetEnumerator()
        {
            return m_List.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_List.GetEnumerator();
        }

    }

}
