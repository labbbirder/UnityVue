//author: bbbirder
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using com.bbbirder;
public class ReactiveList<T> : IEnumerable<T>,IList<T>,IList,IWatched
{
	public Action<object, string> onSetProperty { get; set; }
	public Action<object, string> onGetProperty { get; set; }	
	private const int DefaultCapacity = 8;
	public T[] innerArray;
	public bool IsReadOnly => false;
	int _Count;
	public int Count {get{
		onGetProperty?.Invoke(this,nameof(Count));
		return _Count;
	} private set{
		// var prev = _Count;
		if(_Count==value) return;
		_Count = value;
		onSetProperty?.Invoke(this,nameof(Count));
	}} 
	public int Capacity = DefaultCapacity;
	public bool IsValueType { get; private set;}

	public bool IsFixedSize => false;

	public bool IsSynchronized => false;
	object _syncRoot;
	public object SyncRoot{
		get{
			if(_syncRoot==null){
				Interlocked.CompareExchange<object>(ref _syncRoot,new(),null);
			}
			return _syncRoot;
		}
	}

	object IList.this[int index] { get => this[index]; set => this[index]=(T)value; }

	public ReactiveList (ReactiveList<T> CopyList)
	{
		innerArray = (T[])CopyList.innerArray.Clone ();
		Count = innerArray.Length;
		Capacity = innerArray.Length;
		CSReactive.DataMaker.OnMakeData(this);
	}

	public ReactiveList (T[] StartArray)
	{
		innerArray = StartArray;
		Count = innerArray.Length;
		Capacity = innerArray.Length;
		CSReactive.DataMaker.OnMakeData(this);
	}

	public ReactiveList (IEnumerable StartArray)
	{
		Count = 0;
		innerArray = new T[Capacity];
		foreach(var ele in StartArray){
			EnsureCapacity(-~Count);
			this[Count] = (T)ele;
			Count++;
		}
		Capacity = innerArray.Length;
		CSReactive.DataMaker.OnMakeData(this);
	}

	public ReactiveList (int StartCapacity)
	{
		Capacity = StartCapacity;
		innerArray = new T[Capacity];

		Initialize ();
		CSReactive.DataMaker.OnMakeData(this);
	}
	public ReactiveList ()
	{
		innerArray = new T[Capacity];
		Initialize ();
		CSReactive.DataMaker.OnMakeData(this);
	}

    public void __InitWithRawData(object raw) {
		// Count = 0;
		// innerArray = (T[])raw;
		// foreach(var ele in StartArray){
		// 	EnsureCapacity(-~Count);
		// 	this[Count] = (T)ele;
		// 	Count++;
		// }
		// Capacity = innerArray.Length;
    }

	private void Initialize ()
	{
		
		Count = 0;
		this.IsValueType = typeof(T).IsValueType;
	}

	public void Add (T item)
	{
		EnsureCapacity (Count + 1);
		this [Count++] = item;

	}

	public void AddRange (ReactiveList<T> items)
	{
		int arrayLength = items.Count;
		EnsureCapacity (Count + arrayLength + 1);
		for (int i = 0; i < arrayLength; i++)
		{
			this[Count++] = items[i];
		}
	}

	public void AddRange (T[] items)
	{
		int arrayLength = items.Length;
		EnsureCapacity (Count + arrayLength + 1);
		for (int i = 0; i < arrayLength; i++)
		{
			this[Count++] = items[i];
		}
	}
	public void AddRange (T[] items, int startIndex, int count)
	{
		EnsureCapacity (Count + count + 1);
		for (int i = 0; i < count; i++)
		{
			this[Count++] = items[i + startIndex];
		}
	}

	public void Insert (int index, T item)
	{
		Count+=1;
		EnsureCapacity(Count);
		for(int i=~-Count; i>-~index; i--){
			this[i] = this[~-i];
		}
		this[index] = item;
	}

	public bool Remove (T item)
	{
		
		int index = Array.IndexOf (innerArray, item, 0, Count);
		if (index >= 0) {
			RemoveAt (index);
			return true;
		}
		return false;
	}

	public void RemoveAt (int index)
	{
		if(index<0||index>=Count) return;
		Count--;
		this [index] = default(T);
		for(int i = index;i<Count;i++){
			this[i] = this[-~i];
		}
	}

	public T[] ToArray ()
	{
		T[] retArray = new T[Count];
		Array.Copy (innerArray,0,retArray,0,Count);
		return retArray;
	}
	public void CopyTo(T[] array, int index){
		Array.Copy(innerArray,0,array,index,Count);
	}
	public bool Contains (T item)
	{
		return Array.IndexOf (innerArray,item,0,Count) != -1;
	}
	public int IndexOf (T item)
	{
		return Array.IndexOf (innerArray,item,0,Count);
	}

	public void Reverse ()
	{
		//Array.Reverse (innerArray,0,Count);
		int highCount = Count / 2;
		int reverseCount = Count - 1;
		for (int i = 0; i < highCount; i++)
		{
			T swapItem = this[i];
			this[i] = this[reverseCount];
			this[reverseCount] = swapItem;

			reverseCount--;
		}
	}

	public void EnsureCapacity (int min)
	{
		if (Capacity < min)
		{
			Capacity *= 2;
			if (Capacity < min) {
				Capacity = min;
			}
			Array.Resize (ref innerArray, Capacity);
		}
	}

	public T this [int index] {
		get {
			onGetProperty?.Invoke(this,index.ToString());
			return innerArray [index];
		}
		set {
			var prev = innerArray.ElementAtOrDefault(index);
			if(EqualityComparer<T>.Default.Equals(prev,value)) return;
			innerArray [index] = value;
			onSetProperty?.Invoke(this,index.ToString());
		}
	}

	public void Clear ()
	{
		if (this.IsValueType) {
			FastClear();
		} else {
			for (int i = 0; i < Capacity; i++) {
				innerArray[i] = default(T);
			}
		}
		Count = 0;
	}
	
	/// <summary>
	/// Marks elements for overwriting. Note: this list will still keep references to objects.
	/// </summary>
	public void FastClear ()
	{
		Count = 0;
	}

	public void CopyTo (ReactiveList<T> target)
	{
		Array.Copy (innerArray,0,target.innerArray,0,Count);
		target.Count = Count;
		target.Capacity = Capacity;
	}

	// public T[] TrimmedArray {
	// 	get {
	// 		T[] ret = new T[Count];
	// 		Array.Copy (innerArray, ret, Count);
	// 		return ret;
	// 	}
	// }
	
	public override string ToString ()
	{
		if (Count <= 0)
			return base.ToString ();
		string output = string.Empty;
		for (int i = 0; i < Count - 1; i++)
			output += innerArray [i] + ", ";
		
		return base.ToString () + ": " + output + innerArray [Count - 1];
	}

	public IEnumerator<T> GetEnumerator ()
	{
		for (int i = 0; i < this.Count; i++) {
			yield return this.innerArray[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator ()
	{
		for (int i = 0; i < this.Count; i++) {
			yield return this.innerArray[i];
		}
	}
    public int Add(object value)
    {
        Add((T)value);
		return Count;
    }

    public bool Contains(object value)
    {
        return Contains((T)value);
    }

    public int IndexOf(object value)
    {
        return IndexOf((T)value);
    }

    public void Insert(int index, object value)
    {
        Insert(index,(T)value);
    }

    public void Remove(object value)
    {
        Remove((T)value);
    }

    public void CopyTo(Array array, int index)
    {
        CopyTo((T[])array,index);
    }

    // public static implicit operator T[](ReactiveList<T> list){
	// 	return list.ToArray();
	// }
}