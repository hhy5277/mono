//
// System.Drawing.FontConverter.cs
//
// Authors:
//	Dennis Hayes (dennish@Raytek.com)
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Ravindra (rkumar@novell.com)
//
// Copyright (C) 2002,2003 Ximian, Inc.  http://www.ximian.com
//
// Copyright (C) 2004-2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Drawing.Text;
using System.ComponentModel.Design.Serialization;
using System.Reflection;

namespace System.Drawing
{
	public class FontConverter : TypeConverter
	{
		public FontConverter ()
		{
		}
		~FontConverter ()
		{
			// required to match API definition
		}	
		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof (string))
				return true;

			return base.CanConvertFrom (context, sourceType);
		}

		public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof (String))
				return true;

			if (destinationType == typeof (InstanceDescriptor))
				return true;

			return base.CanConvertTo (context, destinationType);
		}

		public override object ConvertTo (ITypeDescriptorContext context,
			CultureInfo culture,
			object value,
			Type destinationType)
		{
			if ((destinationType == typeof (string)) && (value is Font)) {
				Font font = (Font) value;
				StringBuilder sb = new StringBuilder ();
				sb.Append (font.Name).Append (culture.TextInfo.ListSeparator[0] + " ");
				sb.Append (font.Size);

				switch (font.Unit) {
					// MS throws ArgumentException, if unit is set 
					// to GraphicsUnit.Display
					// Don't know what to append for GraphicsUnit.Display
				case GraphicsUnit.Display:
					sb.Append ("display"); break;

				case GraphicsUnit.Document:
					sb.Append ("doc"); break;

				case GraphicsUnit.Point:
					sb.Append ("pt"); break;

				case GraphicsUnit.Inch:
					sb.Append ("in"); break;

				case GraphicsUnit.Millimeter:
					sb.Append ("mm"); break;

				case GraphicsUnit.Pixel:
					sb.Append ("px"); break;

				case GraphicsUnit.World:
					sb.Append ("world"); break;
				}

				if (font.Style != FontStyle.Regular)
					sb.Append (culture.TextInfo.ListSeparator[0] + " style=").Append (font.Style);

				return sb.ToString ();
			}

			if ((destinationType == typeof (InstanceDescriptor)) && (value is Font)) {
				Font font = (Font) value;
				ConstructorInfo met = typeof(Font).GetTypeInfo ().GetConstructor (new Type[] {typeof(string), typeof(float), typeof(FontStyle), typeof(GraphicsUnit)});
				object[] args = new object[4];
				args [0] = font.Name;
				args [1] = font.Size;
				args [2] = font.Style;
				args [3] = font.Unit;
				return new InstanceDescriptor (met, args);
			}
			
			return base.ConvertTo (context, culture, value, destinationType);
		}

		public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			FontStyle	f_style;
			float		f_size;
			GraphicsUnit	f_unit;
			string		font;
			string		units;
			string[]	fields;

			if (! (value is string)) {
				return base.ConvertFrom (context, culture, value);
			}

			font = (string)value;
			font = font.Trim ();

			if (font.Length == 0) {
				return null;
			}

			if (culture == null) {
				culture = CultureInfo.CurrentCulture;
			}

			// Format is FontFamily, size[<units>[, style=1,2,3]]
			// This is a bit tricky since the comma can be used for styles and fields
			fields = font.Split(new char[] {culture.TextInfo.ListSeparator[0]});
			if (fields.Length < 1) {
				throw new ArgumentException("Failed to parse font format");
			}

			font = fields[0];
			f_size = 8f;
			units = "px";
			f_unit = GraphicsUnit.Pixel;
			if (fields.Length > 1) {	// We have a size
				for (int i = 0; i < fields[1].Length; i++) {
					if (Char.IsLetter(fields[1][i])) {
						f_size = (float)TypeDescriptor.GetConverter(typeof(float)).ConvertFromString(context, culture, fields[1].Substring(0, i));
						units = fields[1].Substring(i);
						break;
					}
				}
				if (units == "display") {
					f_unit = GraphicsUnit.Display;
				} else if (units == "doc") {
					f_unit = GraphicsUnit.Document;
				} else if (units == "pt") {
					f_unit = GraphicsUnit.Point;
				} else if (units == "in") {
					f_unit = GraphicsUnit.Inch;
				} else if (units == "mm") {
					f_unit = GraphicsUnit.Millimeter;
				} else if (units == "px") {
					f_unit = GraphicsUnit.Pixel;
				} else if (units == "world") {
					f_unit = GraphicsUnit.World;
				}
			}

			f_style = FontStyle.Regular;
			if (fields.Length > 2) {	// We have style
				string		compare;

				for (int i = 2; i < fields.Length; i++) {
					compare = fields[i];

					if (compare.IndexOf("Regular") != -1) {
						f_style |= FontStyle.Regular;
					}
					if (compare.IndexOf("Bold") != -1) {
						f_style |= FontStyle.Bold;
					}
					if (compare.IndexOf("Italic") != -1) {
						f_style |= FontStyle.Italic;
					}
					if (compare.IndexOf("Strikeout") != -1) {
						f_style |= FontStyle.Strikeout;
					}
					if (compare.IndexOf("Underline") != -1) {
						f_style |= FontStyle.Underline;
					}
				}
			}

			return new Font (font, f_size, f_style, f_unit);
		}

		public override object CreateInstance (ITypeDescriptorContext context, IDictionary propertyValues)
		{
			Object value;
			byte charSet = 1;
			float size = 8;
			String name = null;
			bool vertical = false;
			FontStyle style = FontStyle.Regular;
			FontFamily fontFamily = null;
			GraphicsUnit unit = GraphicsUnit.Point;

			if ((value = propertyValues ["GdiCharSet"]) != null)
				charSet = (byte) value;

			if ((value = propertyValues ["Size"]) != null)
				size = (float) value;

			if ((value = propertyValues ["Unit"]) != null)
				unit = (GraphicsUnit) value;

			if ((value = propertyValues ["Name"]) != null)
				name = (String) value;

			if ((value = propertyValues ["GdiVerticalFont"]) != null)
				vertical = (bool) value;

			if ((value = propertyValues ["Bold"]) != null) {
				bool bold = (bool) value;
				if (bold == true)
					style |= FontStyle.Bold;
			}

			if ((value = propertyValues ["Italic"]) != null) {
				bool italic = (bool) value;
				if (italic == true)
					style |= FontStyle.Italic;
			}

			if ((value = propertyValues ["Strikeout"]) != null) {
				bool strike = (bool) value;
				if (strike == true)
					style |= FontStyle.Strikeout;
			}

			if ((value = propertyValues ["Underline"]) != null) {
				bool underline = (bool) value;
				if (underline == true)
					style |= FontStyle.Underline;
			}

			/* ?? Should default font be culture dependent ?? */
			if (name == null)
				fontFamily = new FontFamily ("Tahoma");
			else {
				name = name.ToLower ();
				FontCollection collection = new InstalledFontCollection ();
				FontFamily [] installedFontList = collection.Families;
				foreach (FontFamily font in installedFontList) {
					if (name == font.Name.ToLower ()) {
						fontFamily = font;
						break;
					}
				}

				// font family not found in installed fonts
				if (fontFamily == null) {
					collection = new PrivateFontCollection ();
					FontFamily [] privateFontList = collection.Families;
					foreach (FontFamily font in privateFontList) {
						if (name == font.Name.ToLower ()) {
							fontFamily = font;
							break;
						}
					}
				}

				// font family not found in private fonts also
				if (fontFamily == null)
					fontFamily = FontFamily.GenericSansSerif;
			}

			return new Font (fontFamily, size, style, unit, charSet, vertical);
		}

		public override bool GetCreateInstanceSupported (ITypeDescriptorContext context)
		{
			return true;
		}

		public override PropertyDescriptorCollection GetProperties
			(ITypeDescriptorContext context,
			object value, Attribute [] attributes)
		{
			if (value is Font)
				return TypeDescriptor.GetProperties (value, attributes);

			return base.GetProperties (context, value, attributes);
		}

		public override bool GetPropertiesSupported (ITypeDescriptorContext context)
		{
			return true;
		}

		public sealed class FontNameConverter : TypeConverter
		, IDisposable		
		{
			FontFamily [] fonts;
			
			public FontNameConverter ()
			{
				fonts = FontFamily.Families;
			}	
			void IDisposable.Dispose ()
			{
			}
			public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
			{
				if (sourceType == typeof (string))
					return true;

				return base.CanConvertFrom (context, sourceType);
			}

			public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if (value is string)
					return value;
				return base.ConvertFrom (context, culture, value);
			}

			public override StandardValuesCollection GetStandardValues (ITypeDescriptorContext context)
			{
				string [] values = new string [fonts.Length];
				for (int i = fonts.Length; i > 0;){
					i--;
					values [i] = fonts [i].Name;
				}
				
				return new TypeConverter.StandardValuesCollection (values);
			}

			public override bool GetStandardValuesExclusive (ITypeDescriptorContext context)
			{
				// We allow other values other than those in the font list.
				return false;
			}

			public override bool GetStandardValuesSupported (ITypeDescriptorContext context)
			{
				// Yes, we support picking an element from the list. 
				return true;
			}
		}

		public class FontUnitConverter : EnumConverter
		{
			public FontUnitConverter () : base (typeof (GraphicsUnit)) {}
			
			public override StandardValuesCollection GetStandardValues (ITypeDescriptorContext context)
			{
				return base.GetStandardValues (context);
			}
				
		}
	}
}
