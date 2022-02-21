using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace Observation {
	public class ObservableHashSet<T> : ISet<T>, IEnumerable<T>, INotifyCollectionChanged {

		private HashSet<T> hash;

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public delegate void AddRemoveHandler(object sender, IEnumerable<T> items);
		public delegate void ResetHandler(object sender);

		public event AddRemoveHandler Added;
		public event AddRemoveHandler Removed;
		public event ResetHandler Reset;

		#region Non Objervable Links

		private class nonObservableLinks<Z> : INonObservableLinks {
			public ObservableHashSet<T> Observable { get; }
			public HashSet<Z> NonObservable { get; }
			public AddRemoveHandler Added { get; }
			public AddRemoveHandler Removed { get; }
			public ResetHandler Reset { get; }
			public Func<T, Z> Convert { get; }

			public Func<Z, T> ConvertBack { get; }

			private bool IsLinked = false;

			public nonObservableLinks(ObservableHashSet<T> observable, HashSet<Z> nonObservable,
				Func<T, Z> convert, Func<Z, T> convertBack, ReflectInitialization initialization){
				Observable = observable;
				NonObservable = nonObservable;
				Convert = convert;
				ConvertBack = convertBack;
				Initialize(initialization);

				Added = (object sender, IEnumerable<T> items) => 
					nonObservable.UnionWith(items.Select(x => Convert(x)));
				Observable.Added += Added;
				Removed = (object sender, IEnumerable<T> items) => 
					nonObservable.ExceptWith(items.Select(x => Convert(x)));
				Observable.Removed += Removed;
				Reset = (object sender) => nonObservable.Clear();
				Observable.Reset += Reset;

				IsLinked = true;
			}

			private void Initialize(ReflectInitialization mode) {
				switch(mode) {
					case ReflectInitialization.Clear:
						NonObservable.Clear();
						Observable.Clear();
					break;
					case ReflectInitialization.Observable:
						NonObservable.Clear();
						NonObservable.UnionWith(Observable.Select(x => Convert(x)));
					break;
					case ReflectInitialization.NonObservable:
						Observable.Clear();
						Observable.UnionWith(NonObservable.Select(x => ConvertBack(x)));
					break;
				}
			}

			~nonObservableLinks() {
				Unlink();
			}

			public void Unlink() {
				if(!IsLinked) return;
				Observable.Removed -= Removed;
				Observable.Reset -= Reset;
				Observable.Added -= Added;
				IsLinked = false;
			}
		}

		private Dictionary<object, INonObservableLinks> Links = 
			new Dictionary<object, INonObservableLinks>(new ObjectKeyComparer());

		public void ReflectOn(HashSet<T> nonObservable, ReflectInitialization
			initialization = ReflectInitialization.Observable) {

			lock(this){
				Debug.Assert(nonObservable != null);
				if(Links.ContainsKey(nonObservable)) return;
				Links[nonObservable] = new nonObservableLinks<T>(this, nonObservable, 
					(T obj) => obj, (T obj) => obj, initialization);
			}
		}

		public void ReflectOn<Z>(HashSet<Z> nonObservable, IValueConverter<T,Z> converter,
			ReflectInitialization initialization = ReflectInitialization.Observable) {

			ReflectOn<Z>(nonObservable, converter.Convert,
				converter.ConvertBack, initialization);
		}

		public void ReflectOn<Z>(HashSet<Z> nonObservable, Func<T, Z> convert, Func<Z, T> convertBack,
				ReflectInitialization initialization = ReflectInitialization.Observable) {
			lock(this){
				Debug.Assert(nonObservable != null);
				Debug.Assert(convert!=null);
				Debug.Assert(convertBack!=null);
				if(Links.ContainsKey(nonObservable)) return;
				Links[nonObservable] = new nonObservableLinks<Z>(this, nonObservable,
					convert, convertBack, initialization);
			}
		}

		public void UnlinkReflection<Z>(HashSet<Z> nonObservable) {
			lock(this){
				if(Links == null || !Links.ContainsKey(nonObservable)) return;
				Links[nonObservable].Unlink();
				Links.Remove(nonObservable);
			}
		}

		#endregion

		#region Constructors

		public ObservableHashSet() {
			hash = new HashSet<T>();
		}

		public ObservableHashSet(IEnumerable<T> items) {
			hash = new HashSet<T>(items);
		}

		#endregion

		#region Non Altering
		public int Count => hash.Count;

		public bool IsReadOnly => false;


		public IEnumerator<T> GetEnumerator() {
			return hash.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return hash.GetEnumerator();
		}

		public bool IsSubsetOf( IEnumerable<T> other ) {
			lock (this) {
				return hash.IsSubsetOf(other);
			}
		}

		public bool IsSupersetOf( IEnumerable<T> other ) {
			lock (this) {
				return hash.IsSupersetOf(other);
			}
		}

		public bool IsProperSupersetOf( IEnumerable<T> other ) {
			lock (this) {
				return hash.IsProperSupersetOf(other);
			}
		}

		public bool IsProperSubsetOf( IEnumerable<T> other ) {
			lock (this) {
				return hash.IsProperSubsetOf(other);
			}
		}

		public bool Overlaps( IEnumerable<T> other ) {
			lock (this) {
				return hash.Overlaps(other);
			}
		}

		public bool SetEquals( IEnumerable<T> other ) {
			lock (this) {
				return hash.SetEquals(other);
			}			
		}

		public bool Contains( T item ) {
			lock (this) {
				return hash.Contains(item);
			}
		}

		public void CopyTo( T[] array, int arrayIndex ) {
			lock (this) {
				hash.CopyTo(array, arrayIndex);
			}
		}

		#endregion

		#region Altering

		public bool Add( T item ) {
			lock(this){
				if(!hash.Add(item)) return false;

				var addeditemlist = new List<T>{ item };
				CollectionChanged?.Invoke(this, 
					new NotifyCollectionChangedEventArgs
					(NotifyCollectionChangedAction.Add, addeditemlist));
				Added?.Invoke(this, addeditemlist);

				return true;
			}
		}

		public void UnionWith( IEnumerable<T> other ) {
			lock(this){
				if(other == null) throw new ArgumentNullException();
				List<T> added = new List<T>();

				foreach(T item in other) if (!hash.Contains(item)) {
					bool result = hash.Add(item);
					Debug.Assert(result == true);
					added.Add(item);
				}

				if(added.Count > 0) {
					CollectionChanged?.Invoke(this, 
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, added));
					Added?.Invoke(this, added);
				}
			}
		}

		public void IntersectWith( IEnumerable<T> other ) {
			lock (this) {
				if(other == null) throw new ArgumentNullException();

				List<T> removed = new List<T>();

				foreach(T item in other) {
					if (!hash.Contains(item)) {
						removed.Add(item);
						bool result = hash.Remove(item);
						Debug.Assert(result == true);
					}
				}

				if(removed.Count > 0){
					CollectionChanged?.Invoke(this, 
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed));
					Removed?.Invoke(this, removed);
				}
			}
		}

		public void ExceptWith( IEnumerable<T> other ) {
			lock (this) {
				if(other == null) throw new ArgumentNullException();

				List<T> removed = new List<T>();

				foreach(T item in other) 
					if(hash.Remove(item)) removed.Add(item);
				
				if(removed.Count > 0) {
					CollectionChanged?.Invoke(this, 
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed));
					Removed?.Invoke(this, removed);
				}
			}
		}

		public void SymmetricExceptWith( IEnumerable<T> other ) {
			lock (this) {
				if(other == null) throw new ArgumentNullException();

				HashSet<T> added = new HashSet<T>(other);
				added.ExceptWith(hash);

				HashSet<T> removed = new HashSet<T>(hash);
				removed.IntersectWith(other);
				
				hash.ExceptWith(removed);
				hash.UnionWith(added);
				
				if(removed.Count > 0){
					CollectionChanged?.Invoke(this, 
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed));
					Removed?.Invoke(this, removed);
				}

				if(added.Count > 0){
					CollectionChanged?.Invoke(this, 
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, added));
					Added?.Invoke(this, added);
				}
			}
		}

		void ICollection<T>.Add( T item ) {
			this.Add(item);
		}

		public void Clear() {
			lock (this) {
				hash.Clear();
				CollectionChanged?.Invoke(this, 
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				Reset?.Invoke(this);
			}
		}

		public bool Remove( T item ) {
			lock (this) {
				if(!hash.Remove(item)) return false;

				var removed = new List<T>{ item };
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs
					(NotifyCollectionChangedAction.Remove, removed));
				Removed?.Invoke(this, removed);

				return true;
			}
		}

		#endregion
	}
}