﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms.VisualStyles;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Binders
{
	public class NotifyValueSupport
	{
		public static void Register()
		{
			var defaultSetBinding = ConventionManager.SetBinding;

			//ConventionManager.ApplyItemTemplate - Будет пытаться установить
			//шаблон для NotifyValue<List<string>> Producers
			//тк будет думать что NotifyValue биндинг будет производиться
			//к Producers а на самом деле биндинг будет к Producers.Value
			//это сделает ConventionManager.Set
			ConventionManager.AddElementConvention<Selector>(Selector.ItemsSourceProperty,
				"SelectedItem",
				"SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
					Patch(ref path, ref property);
					var ignore = ConventionManager.SetBindingWithoutBindingOrValueOverwrite(viewModelType,
						path,
						property,
						element,
						convention,
						ItemsControl.ItemsSourceProperty);

					if (!ignore)
						return false;

					ConventionManager.ConfigureSelectedItem(element, Selector.SelectedItemProperty, viewModelType, path);
					if (IsArrayOfPrimitive(property.PropertyType))
						return true;

					if (!(element is PopupSelector))
						ConventionManager.ApplyItemTemplate((ItemsControl)element, property);

					return true;
				};

			ConventionManager.ConfigureSelectedItem =
				(selector, selectedItemProperty, viewModelType, path) => {
					if (ConventionManager.HasBinding(selector, selectedItemProperty)) {
						return;
					}

					var baseName = path;
					if (path.EndsWith(".Value")) {
						var index = path.LastIndexOf('.');
						baseName = path.Substring(0, index);
					}

					foreach (var potentialName in ConventionManager.DerivePotentialSelectionNames(baseName)) {
						var propertyInfo = viewModelType.GetPropertyCaseInsensitive(potentialName);
						if (propertyInfo != null) {
							var selectionPath = potentialName;
							if (IsNotifyValue(propertyInfo))
								selectionPath += ".Value";

							var binding = new Binding(selectionPath) { Mode = BindingMode.TwoWay };
							var shouldApplyBinding = ConventionManager.ConfigureSelectedItemBinding(selector,
								selectedItemProperty,
								viewModelType,
								selectionPath,
								binding);

							if (shouldApplyBinding) {
								BindingOperations.SetBinding(selector, selectedItemProperty, binding);
								return;
							}
						}
					}
				};

			ConventionManager.SetBinding =
				(viewModelType, path, property, element, convention, bindableProperty) => {
					Patch(ref path, ref property);
					defaultSetBinding(viewModelType, path, property, element, convention, bindableProperty);
				};

			var basePrepareContext = ActionMessage.PrepareContext;
			ActionMessage.PrepareContext = context => {
				ActionMessage.SetMethodBinding(context);
				if (context.Target == null || context.Method == null)
					return;

				var guardName = "Can" + context.Method.Name;
				var targetType = context.Target.GetType();

				var guard = targetType.GetProperty(guardName);
				if (guard == null || !IsNotifyValue(guard)) {
					basePrepareContext(context);
					return;
				}

				var inpc = guard.GetValue(context.Target, null) as INotifyPropertyChanged;
				if (inpc == null)
					return;

				PropertyChangedEventHandler handler = null;
				handler = (s, e) => {
					if (context.Message == null) {
						inpc.PropertyChanged -= handler;
						return;
					}
					context.Message.UpdateAvailability();
				};

				inpc.PropertyChanged += handler;
				context.Disposing += delegate { inpc.PropertyChanged -= handler; };
				context.Message.Detaching += delegate { inpc.PropertyChanged -= handler; };

				context.CanExecute = () => (NotifyValue<bool>)guard.GetValue(context.Target, null);
			};
		}

		public static bool IsArrayOfPrimitive(Type posibleArrayOrList)
		{
			if (posibleArrayOrList.IsArray) {
				var elementType = posibleArrayOrList.GetElementType();
				return elementType == typeof(string) || elementType.IsPrimitive;
			}

			return false;
		}

		public static bool IsNotifyValue(PropertyInfo property)
		{
			return property.PropertyType.IsGenericType
				&& property.PropertyType.GetGenericTypeDefinition() == typeof(NotifyValue<>);
		}

		public static void Patch(ref string path, ref PropertyInfo property)
		{
			if (IsNotifyValue(property)) {
				path += ".Value";
				property = property.PropertyType.GetProperty("Value");
			}
		}
	}
}