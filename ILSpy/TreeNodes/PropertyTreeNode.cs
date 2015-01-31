﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Windows.Media;
using ICSharpCode.Decompiler;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Represents a property in the TreeView.
	/// </summary>
	public sealed class PropertyTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		readonly PropertyDef property;
		readonly bool isIndexer;

		public PropertyTreeNode(PropertyDef property)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			this.property = property;
			using (LoadedAssembly.DisableAssemblyLoad()) {
				this.isIndexer = property.IsIndexer();
			}

			if (property.GetMethod != null)
				this.Children.Add(new MethodTreeNode(property.GetMethod));
			if (property.SetMethod != null)
				this.Children.Add(new MethodTreeNode(property.SetMethod));
			if (property.HasOtherMethods) {
				foreach (var m in property.OtherMethods)
					this.Children.Add(new MethodTreeNode(m));
			}
			
		}

		public PropertyDef PropertyDefinition {
			get { return property; }
		}

		public override object Text
		{
			get { return GetText(property, Language, isIndexer) + property.MDToken.ToSuffixString(); }
		}

		public static object GetText(PropertyDef property, Language language, bool? isIndexer = null)
		{
			return HighlightSearchMatch(language.FormatPropertyName(property, isIndexer), " : " + language.TypeToString(property.PropertySig.GetRetType().ToTypeDefOrRef(), false, property));
		}
		
		public override object Icon
		{
			get { return GetIcon(property); }
		}

		public static ImageSource GetIcon(PropertyDef property, bool isIndexer = false)
		{
			MemberIcon icon = isIndexer ? MemberIcon.Indexer : MemberIcon.Property;
			MethodAttributes attributesOfMostAccessibleMethod = GetAttributesOfMostAccessibleMethod(property);
			bool isStatic = (attributesOfMostAccessibleMethod & MethodAttributes.Static) != 0;
			return Images.GetIcon(icon, GetOverlayIcon(attributesOfMostAccessibleMethod), isStatic);
		}

		private static AccessOverlayIcon GetOverlayIcon(MethodAttributes methodAttributes)
		{
			switch (methodAttributes & MethodAttributes.MemberAccessMask) {
				case MethodAttributes.Public:
					return AccessOverlayIcon.Public;
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return AccessOverlayIcon.Internal;
				case MethodAttributes.Family:
					return AccessOverlayIcon.Protected;
				case MethodAttributes.FamORAssem:
					return AccessOverlayIcon.ProtectedInternal;
				case MethodAttributes.Private:
					return AccessOverlayIcon.Private;
				case MethodAttributes.CompilerControlled:
					return AccessOverlayIcon.CompilerControlled;
				default:
					throw new NotSupportedException();
			}
		}

		private static MethodAttributes GetAttributesOfMostAccessibleMethod(PropertyDef property)
		{
			// There should always be at least one method from which to
			// obtain the result, but the compiler doesn't know this so
			// initialize the result with a default value
			MethodAttributes result = (MethodAttributes)0;

			// Method access is defined from inaccessible (lowest) to public (highest)
			// in numeric order, so we can do an integer comparison of the masked attribute
			int accessLevel = 0;

			if (property.GetMethod != null) {
				int methodAccessLevel = (int)(property.GetMethod.Attributes & MethodAttributes.MemberAccessMask);
				if (accessLevel < methodAccessLevel) {
					accessLevel = methodAccessLevel;
					result = property.GetMethod.Attributes;
				}
			}

			if (property.SetMethod != null) {
				int methodAccessLevel = (int)(property.SetMethod.Attributes & MethodAttributes.MemberAccessMask);
				if (accessLevel < methodAccessLevel) {
					accessLevel = methodAccessLevel;
					result = property.SetMethod.Attributes;
				}
			}

			if (property.HasOtherMethods) {
				foreach (var m in property.OtherMethods) {
					int methodAccessLevel = (int)(m.Attributes & MethodAttributes.MemberAccessMask);
					if (accessLevel < methodAccessLevel) {
						accessLevel = methodAccessLevel;
						result = m.Attributes;
					}
				}
			}

			return result;
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			if (settings.SearchTermMatches(property.Name) && settings.Language.ShowMember(property))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileProperty(property, output, options);
		}
		
		public override bool IsPublicAPI {
			get {
				switch (GetAttributesOfMostAccessibleMethod(property) & MethodAttributes.MemberAccessMask) {
					case MethodAttributes.Public:
					case MethodAttributes.Family:
					case MethodAttributes.FamORAssem:
						return true;
					default:
						return false;
				}
			}
		}

		IMemberRef IMemberTreeNode.Member
		{
			get { return property; }
		}
	}
}
