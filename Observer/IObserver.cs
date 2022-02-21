/**********************************************************************************
 * MIT License																	  *
 * 																				  *
 * Copyright (c) 2020 Manos Chatzianestis										  *
 * 																				  *
 * Permission is hereby granted, free of charge, to any person obtaining a copy	  *
 * of this software and associated documentation files (the "Software"), to deal  *
 * in the Software without restriction, including without limitation the rights	  *
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell	  *
 * copies of the Software, and to permit persons to whom the Software is		  *
 * furnished to do so, subject to the following conditions:						  *
 * 																				  *
 * The above copyright notice and this permission notice shall be included in all *
 * copies or substantial portions of the Software.								  *
 * 																				  *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR	  *
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,		  *
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE	  *
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER		  *
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  *
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE  *
 * SOFTWARE.																	  *
 * ********************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace Observation {

	/// <summary>
	/// <para>
	/// If Collection property is set to true to an <c>INotifyCollectionChanged</c>
	/// property of an object, that object receives RelayingCollectionChanged events
	/// when the collection is altered.
	/// </para>
	/// <para>
	/// If Property property is set to true to an <c>INotifyPropertyChanged</c>
	/// property of an object, that object receives RelayingChanged events
	/// when the property fires a NotifyChanged event.
	/// </para>
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Property)]
	public class Relay : System.Attribute {
		public bool Collection { get; set; } = false;
		public bool Property { get; set; } = false;
	}
	
	/// <summary>
	/// Use to decorate a property that raises <c>ObservedChanged</c>
	/// when the property is changed via its set method
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Property)]
	public class Observe : System.Attribute { }
	
	//-----------------------------------------------------------------------------

	/// <summary>
	/// <c>IObserver</c> should be implemented by any class that
	/// wishes to generate a proxy using <c>Observer</c> class
	/// </summary>
	public interface IObserver {

		/// <summary>
		/// Each time the value of a property marked by the "Observe" Attribute is
		/// altered by the use of its set method, ObservedChanged is triggered.
		/// </summary>
		/// <param name="propertyname">The name of the property that was changed</param>
		void ObservedChanged(string propertyname);

		/// <summary>
		/// Each time the value of an <c>INotifyPropertyChanged</c> property marked
		/// by the "Relay" Attribute is altered by the use of its set method,
		/// RelayingChanged is triggered.
		/// </summary>
		/// <param name="propertyname">The name of the property that was changed</param>
		/// <param name="sender">The INotifyPropertyChanged object that triggered the event</param>
		/// <param name="e">The arguments of the INotifyPropertyChanged event</param>
		void RelayingChanged(string propertyname,
			object sender, PropertyChangedEventArgs e );

		/// <summary>
		/// Each time the value of an <c>INotifyCollectionChanged</c> property marked
		/// by the "Relay" Attribute is altered by the use of its set method,
		/// RelayingCollectionChanged is triggered.
		/// </summary>
		/// <param name="propertyname">The name of the property that was changed</param>
		/// <param name="sender">The INotifyCollectionChanged object that triggered the event</param>
		/// <param name="e">The arguments of the INotifyCollectionChanged event</param>
		void RelayingCollectionChanged(string propertyname,
			object sender, NotifyCollectionChangedEventArgs e );
	}
}