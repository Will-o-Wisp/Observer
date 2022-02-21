using Observation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace Tests.Helpers {

	public class NotifyImplementer : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		private int number = 0;
		public int Number{ 
			get => number;
			
			set {
				number = value;
				PropertyChanged?.Invoke(this,
				new PropertyChangedEventArgs(nameof(Number)));
			}
		}
	}

	//====================================================================================

	public interface IObserverTestHelper: IObserver {

		[Observe]
		int Number { get; set; }

		[Relay(Collection = true)][Observe]
		Observation.ObservableCollection<int> Numbers { get; set; }

		[Observe]
		NotifyImplementer ObservedImplementer{ get; set; }

		[Relay(Property = true)]
		NotifyImplementer RelayingImplementer{ get; set; }

		String GetText();

		void SetText(String newtext);

		int GetSum(params int[] ints);

		void GetAndSet(out int result, ref int x, ref int y, int target = 0);

		int this[string key]{ get; set; }

		static string GetHelloText() {
			return "Hello";
		}

	}

	//====================================================================================

	public class ObserverTestHelper: IObserverTestHelper {

		public int Number { get; set; }

		public Observation.ObservableCollection<int> Numbers { get; set; } =
			new Observation.ObservableCollection<int>();

		public void SetText( string newtext ) {
			Text = newtext;
		}

		public string GetText() {
			return Text;
		}

		public int GetSum(params int[] ints ) {
			int sum = 0;
			foreach(int number in ints) sum += number;
			return sum;
		}

		public void GetAndSet( out int result, ref int x, ref int y, int target = 0 ) {
			result = x + y;
			x = target;
			y = target;
		}


		private Dictionary<String, int> numsdictionary = new Dictionary<string, int>();
		public int this[string key] {
			get => numsdictionary[key];
			set => numsdictionary[key] = value;
		}

		public NotifyImplementer ObservedImplementer{ get; set; } = null;

		public NotifyImplementer RelayingImplementer{ get; set; } = null;

		private string Text = null;

		//--------------------------------------------------------------

		public List<Tuple<EventType, String>> Fired { get; } 
			= new List<Tuple<EventType, string>>();
		public enum EventType{ Observed, Relaying, RelayingCollection }

		public void ObservedChanged( string propertyname ) {
			Fired.Add(new Tuple<EventType, string>(EventType.Observed, propertyname));
		}

		public void RelayingChanged( string propertyname,
			object sender, PropertyChangedEventArgs e ) {
			Fired.Add(new Tuple<EventType, string>(EventType.Relaying, propertyname));
		}

		public void RelayingCollectionChanged( string propertyname,
			object sender, NotifyCollectionChangedEventArgs e ) {
			Fired.Add(new Tuple<EventType, string>(EventType.RelayingCollection, propertyname));
		}
	}

}