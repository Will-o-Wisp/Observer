using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace Observation {

	public class ObservableDictionary<TKey, TValue> :
		IDictionary<TKey, TValue>,
		IEnumerable<KeyValuePair<TKey, TValue>>,
		IReadOnlyDictionary<TKey,TValue>,
		INotifyCollectionChanged{

		private Dictionary<TKey, TValue> inner;

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public delegate void ReplacedHandler(object sender, TKey key, TValue oldvalue, TValue newvalue);
		public delegate void AddRemoveHandler(object sender, TKey key, TValue value);
		public delegate void ResetHandler(object sender);

		public event ReplacedHandler Replaced;
		public event AddRemoveHandler Added;
		public event AddRemoveHandler Removed;
		public event ResetHandler Reset;

		#region Non Objervable Links

		private class KeyValueConverters<ZKey, ZValue> {
			public Func<ZKey, TKey> tKey;
			public Func<TKey, ZKey> zKey;
			public Func<ZValue, TValue> tValue;
			public Func<TValue, ZValue> zValue;
		}

		private class NonObservableLinks<ZKey, ZValue> : INonObservableLinks {
			public ObservableDictionary<TKey, TValue> Observable { get; }
			public Dictionary<ZKey, ZValue> NonObservable { get; }
			public ReplacedHandler Replaced { get; }
			public AddRemoveHandler Added { get; }
			public AddRemoveHandler Removed { get; }
			public ResetHandler Reset { get; }
			KeyValueConverters<ZKey, ZValue> Converters { get; }

			private bool IsLinked = false;

			public NonObservableLinks(
				ObservableDictionary<TKey, TValue> observable, Dictionary<ZKey, ZValue> nonObservable, 
					 KeyValueConverters<ZKey, ZValue> converters, ReflectInitialization initialization) {
				NonObservable = nonObservable;
				Observable = observable;
				Converters = converters;
				Initialize(initialization);

				Replaced = (object sender, TKey key, TValue oldvalue, TValue newvalue) =>
					NonObservable[Converters.zKey(key)] = Converters.zValue(newvalue);
				Observable.Replaced += Replaced;
				Added = (object sender, TKey key, TValue value) => 
					NonObservable[Converters.zKey(key)] = Converters.zValue(value);
				Observable.Added += Added;
				Removed = (object sender, TKey key, TValue value) =>
					NonObservable.Remove(Converters.zKey(key));
				Observable.Removed += Removed;
				Reset = (object sender) => NonObservable.Clear();
				Observable.Reset += Reset;

				IsLinked = true;
			}

			private void Initialize(ReflectInitialization mode) {
				switch(mode) {
					case ReflectInitialization.Observable:
						NonObservable.Clear();
						foreach(var keyvalue in Observable)
							NonObservable[Converters.zKey(keyvalue.Key)] = Converters.zValue(keyvalue.Value);
					break;
					case ReflectInitialization.NonObservable:
						Observable.Clear();
						foreach(var keyvalue in NonObservable)
							Observable[Converters.tKey(keyvalue.Key)] = Converters.tValue(keyvalue.Value);
					break;
					case ReflectInitialization.Clear:
						NonObservable.Clear();
						Observable.Clear();
					break;
				}
			}

			~NonObservableLinks() {
				Unlink();
			}

			public void Unlink() {
				if(!IsLinked) return;
				Observable.Replaced -= Replaced;
				Observable.Removed -= Removed;
				Observable.Reset -= Reset;
				Observable.Added -= Added;
				IsLinked = false;
			}
		}

		private Dictionary<object, INonObservableLinks> Links = 
			new Dictionary<object, INonObservableLinks>(new ObjectKeyComparer());

		private static KeyValueConverters<TKey, TValue> DefaultConverters
			= new KeyValueConverters<TKey, TValue>() {
			zKey = (TKey x) => x, tKey = (TKey x) => x,
			tValue = (TValue x) => x, zValue = (TValue x) => x
		};

		public void ReflectOn(Dictionary<TKey, TValue> nonObservable,
			ReflectInitialization initialization = ReflectInitialization.Observable) {

			lock(this){
				Debug.Assert(nonObservable != null);
				if(Links.ContainsKey(nonObservable)) return;
				Links[nonObservable] = new NonObservableLinks<TKey, TValue>
					(this, nonObservable, DefaultConverters, initialization);
			}
		}

		public void ReflectOn<ZKey, ZValue>(Dictionary<ZKey, ZValue> nonObservable,
			IValueConverter<TKey, ZKey> keyConverters,
			IValueConverter<TValue, ZValue> valueConverters,
			ReflectInitialization initialization = ReflectInitialization.Observable) {
			
			Debug.Assert(CheckForNull());

			var converters = new KeyValueConverters<ZKey, ZValue>() {
				zKey = keyConverters.Convert, tKey = keyConverters.ConvertBack,
				zValue = valueConverters.Convert, tValue = valueConverters.ConvertBack
			};

			lock(this){
				if(Links.ContainsKey(nonObservable)) return;
				Links[nonObservable] = new NonObservableLinks<ZKey, ZValue>
					(this, nonObservable, converters, initialization);
			}

			bool CheckForNull() {
				if(keyConverters == null || valueConverters == null || nonObservable == null) return false;
				if(keyConverters.Convert == null || keyConverters.ConvertBack == null) return false;
				if(valueConverters.Convert == null || valueConverters.ConvertBack == null) return false;
				return true;
			}
		}

		public void UnlinkReflection<ZKey, ZValue>(Dictionary<ZKey, ZValue> nonObservable) {
			lock(this){
				if(Links == null || !Links.ContainsKey(nonObservable)) return;
				Links[nonObservable].Unlink();
				Links.Remove(nonObservable);
			}
		}

		#endregion

		#region Constructors

		public ObservableDictionary()
			=> inner = new Dictionary<TKey, TValue>();

		public ObservableDictionary( Dictionary<TKey, TValue> initialization )
			=> inner = new Dictionary<TKey, TValue>(initialization);

		public ObservableDictionary( IDictionary<TKey, TValue> dict )
			=> inner = new Dictionary<TKey, TValue>(dict);

		public ObservableDictionary( IDictionary<TKey, TValue> dict, IEqualityComparer<TKey> comparer)
			=> inner = new Dictionary<TKey, TValue>(dict, comparer);

		public ObservableDictionary( IEqualityComparer<TKey> comparer )
			=> inner = new Dictionary<TKey, TValue>(comparer);

		public ObservableDictionary( int capacity )
			=> inner = new Dictionary<TKey, TValue>(capacity);

		public ObservableDictionary( int capacity, IEqualityComparer<TKey> comparer ) 
			=> inner = new Dictionary<TKey, TValue>(capacity, comparer);

		#endregion

		#region Modifying Methods

		public TValue this[TKey key] { 
			get => inner[key];
			
			set {
				lock(this){
					if (inner.ContainsKey(key)) {
						TValue oldvalue = inner[key];
						inner[key] = value;
						CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
							NotifyCollectionChangedAction.Replace,
							new KeyValuePair<TKey, TValue>(key, oldvalue),
							new KeyValuePair<TKey, TValue>(key, value)));
						Replaced?.Invoke(this, key, oldvalue, value);
					} else {
						inner[key] = value;
						CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
							NotifyCollectionChangedAction.Add,
							new KeyValuePair<TKey, TValue>(key, value)));
						Added?.Invoke(this, key, value);
					}
				}
			}
		}

		public void Add( TKey key, TValue value )
			=> this[key] = value;
		
		public void Add( KeyValuePair<TKey, TValue> item )
			=> this[item.Key] = item.Value;
		
		public void Clear() {
			lock (this) {
				if(!inner.Any()) return;
				inner.Clear();
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs
					(NotifyCollectionChangedAction.Reset));
				Reset?.Invoke(this);
			}
		}

		public bool Remove( TKey key ) {
			lock (this) {
				if (!inner.ContainsKey(key)) return false;

				KeyValuePair<TKey, TValue> removed = new KeyValuePair<TKey, TValue>(key, inner[key]);
				bool retval = inner.Remove(key);
				Debug.Assert(retval == true);
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs
					(NotifyCollectionChangedAction.Remove, removed));
				Removed?.Invoke(this, key, removed.Value);
				return true;
			}
		}

		public bool Remove( KeyValuePair<TKey, TValue> item ) {
			lock(this){
				if(!inner.ContainsKey(item.Key)) return false;
				if(!inner[item.Key].Equals(item.Value)) return false;

				bool retval = inner.Remove(item.Key);
				Debug.Assert(retval == true);

				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs
					(NotifyCollectionChangedAction.Remove, item));
				Removed?.Invoke(this, item.Key, item.Value);

				return true;
			}
		}

		#endregion

		#region Non-Modifying Methods

		public void CopyTo( KeyValuePair<TKey, TValue>[] array, int arrayIndex ) 
			=> (inner as ICollection<KeyValuePair<TKey,TValue>>).CopyTo(array, arrayIndex);
		
		public bool TryGetValue( TKey key, out TValue value ) =>
			inner.TryGetValue(key, out value);

		IEnumerator IEnumerable.GetEnumerator() => inner.GetEnumerator();

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
			=> inner.GetEnumerator();

		public bool Contains( KeyValuePair<TKey, TValue> item ) =>
			inner.Contains(item);

		public bool ContainsKey( TKey key ) => inner.ContainsKey(key);

		public int Count => inner.Count;

		public bool IsReadOnly => false;

		public ICollection<TKey> Keys => inner.Keys;

		public ICollection<TValue> Values => inner.Values;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => inner.Keys;

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => inner.Values;

		#endregion
	}
}