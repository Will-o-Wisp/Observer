using System;
using System.Collections.Generic;
using System.Text;

using ChangedArgs = System.Collections.Specialized.NotifyCollectionChangedEventArgs;
using ChangedAction = System.Collections.Specialized.NotifyCollectionChangedAction;
using System.Diagnostics;

namespace Observation {

	public enum ReflectInitialization { Observable, NonObservable, Clear, AsIs }

	internal interface INonObservableLinks {
		void Unlink();
	}

	public class IValueConverter<T, Z> {
		public Func<T, Z> Convert { get; set; }
		public Func<Z, T> ConvertBack { get; set; }
	}

	public class IdentityConverter<T> : IValueConverter<T, T> {
		public IdentityConverter() {
			Convert = (T x) => x;
			ConvertBack = (T x) => x;
		}
	}

	public class ObservableCollection<T> :
		System.Collections.ObjectModel.ObservableCollection<T>{

		public ObservableCollection(){
			CollectionChanged += ObservableCollection_CollectionChanged;
		}

		public delegate void ReplaceHandler(object sender, int index, T olditem, T newitem);
		public delegate void MoveHandler(object sender, int oldindex, int newindex, T item);
		public delegate void AddRemoveHandler(object sender, int index, T item);
		public delegate void ResetHandler(object sender);

		public event AddRemoveHandler Added;
		public event AddRemoveHandler Removed;
		public event ResetHandler Reset;
		public event ReplaceHandler Replaced;
		public event MoveHandler Moved;

		#region Reflect

		private class NonObservableLinks<Z> : INonObservableLinks  {
			public ObservableCollection<T> Observable { get; }
			public List<Z> NonObservable { get; }
			public AddRemoveHandler Added { get; }
			public AddRemoveHandler Removed { get; }
			public ResetHandler Reset { get; }
			public ReplaceHandler Replaced { get; }
			public MoveHandler Moved { get; }
			Func<T, Z> Convert { get; }
			Func<Z, T> ConvertBack{ get; }

			private bool IsLinked = false;

			public NonObservableLinks(ObservableCollection<T> observable,
				List<Z> nonObservable, Func<T, Z> convert, Func<Z, T> convertBack,
				ReflectInitialization initializationMode){

				Observable = observable;
				NonObservable = nonObservable;
				Convert = convert;
				ConvertBack = convertBack;
				Initialize(initializationMode);

				Added = (object sender, int index, T item) => NonObservable.Insert(index, Convert(item));
				Observable.Added += Added;
				Removed = (object sender, int index, T item) => NonObservable.RemoveAt(index);
				Observable.Removed += Removed;
				Reset = (object sender) => NonObservable.Clear();
				Observable.Reset += Reset;
				Replaced = (object sender, int index, T olditem, T newitem) =>
					NonObservable[index] = Convert(newitem);
				Observable.Replaced += Replaced;
				Moved = (object sender, int oldindex, int newindex, T item) => {
					NonObservable.RemoveAt(oldindex);
					NonObservable[newindex] = Convert(item);
				};
				Observable.Moved += Moved;
				IsLinked = true;
			}

			private void Initialize(ReflectInitialization mode) {
				switch(mode) {
					case ReflectInitialization.Clear:
						Observable.Clear();
						NonObservable.Clear();
					break;
					case ReflectInitialization.Observable:
						NonObservable.Clear();
						foreach(var item in Observable) NonObservable.Add(Convert(item));
					break;
					case ReflectInitialization.NonObservable:
						Observable.Clear();
						foreach(var item in NonObservable) Observable.Add(ConvertBack(item));
					break;
				}
			}

			~NonObservableLinks() {
				Unlink();
			}

			public void Unlink() {
				if(!IsLinked) return;
				Observable.Removed -= Removed;
				Observable.Reset -= Reset;
				Observable.Added -= Added;
				Observable.Moved -= Moved;
				Observable.Replaced -= Replaced;
				IsLinked = false;
			}
		}

		private Dictionary<object, INonObservableLinks> ReflectionLinks = 
			new Dictionary<object, INonObservableLinks>(new ObjectKeyComparer());

		public void ReflectOn(List<T> nonObservable, ReflectInitialization
			initialization = ReflectInitialization.Observable) {

			lock(this){
				Debug.Assert(nonObservable != null);
				if(ReflectionLinks.ContainsKey(nonObservable)) return;

				ReflectionLinks[nonObservable] = new NonObservableLinks<T>(this,
					nonObservable, (T obj) => obj, (T obj) => obj, initialization);
			}
		}

		public void ReflectOn<Z>(List<Z> nonObservable, IValueConverter<T,Z> converter,
			ReflectInitialization initialization = ReflectInitialization.Observable) {

			ReflectOn<Z>(nonObservable, converter.Convert,
				converter.ConvertBack, initialization);
		}

		public void ReflectOn<Z>(List<Z> nonObservable, Func<T, Z> convert, Func<Z, T> convertBack,
			ReflectInitialization initialization = ReflectInitialization.Observable) {
			
			lock(this){
				Debug.Assert(nonObservable != null);
				Debug.Assert(convert!=null);
				Debug.Assert(convertBack!=null);
				if(ReflectionLinks.ContainsKey(nonObservable)) return;
				ReflectionLinks[nonObservable] = new NonObservableLinks<Z>(this, 
					nonObservable, convert, convertBack, initialization);
			}
		}

		public void UnlinkReflection<Z>(List<Z> nonObservable) {
			lock(this){
				if(ReflectionLinks == null || !ReflectionLinks.ContainsKey(nonObservable)) return;
				ReflectionLinks[nonObservable].Unlink();
				ReflectionLinks.Remove(nonObservable);
			}
		}

		#endregion

		private void ObservableCollection_CollectionChanged( object sender, ChangedArgs e ) {
			switch (e.Action) {
				case ChangedAction.Add:
					Added?.Invoke(sender, e.NewStartingIndex, (T) e.NewItems[0]);
				break;
				case ChangedAction.Remove:
					Removed?.Invoke(sender, e.OldStartingIndex, (T) e.OldItems[0]);
				break;
				case ChangedAction.Reset:
					Reset?.Invoke(sender);
				break;
				case ChangedAction.Replace:
					Replaced?.Invoke(sender, e.OldStartingIndex, (T) e.OldItems[0], (T) e.NewItems[0]);
				break;
				case ChangedAction.Move:
					Moved?.Invoke(sender, e.OldStartingIndex, e.NewStartingIndex, (T) e.OldItems[0]);
				break;
			}
		}

	}
}