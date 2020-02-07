namespace FastAccessProperty
{
	using System;
	using System.Diagnostics;
	using System.Linq.Expressions;
	using System.Reflection;


	/// <summary>
	/// About 30-40 times faster than using Reflection
	/// </summary>
	public class LamdaPropertyAccessor : IPropertyAccessor
	{
		private Func<object, object> _getter;
		private Action<object, object> _setter;
		private PropertyInfo _propInfo;


		public LamdaPropertyAccessor (PropertyInfo propInfo)
		{
			_propInfo = propInfo; // failsafe

			try {
				if (propInfo.CanRead) {
					_getter = BuildGetAccessor(propInfo.GetGetMethod());
				}
				if (propInfo.CanWrite) {
					_setter = BuildSetAccessor(propInfo.GetSetMethod());
				}
			}
			catch (Exception e) {
				Debug.WriteLine(e);
			}

		}

		private static Func<object, object> BuildGetAccessor (MethodInfo method)
		{
			var obj = Expression.Parameter(typeof(object), "o");

			Expression<Func<object, object>> expr =
				Expression.Lambda<Func<object, object>>(
					Expression.Convert(
						Expression.Call(
							Expression.Convert(obj, method.DeclaringType),
							method),
						typeof(object)),
					obj);

			return expr.Compile();
		}


		private static Action<object, object> BuildSetAccessor (MethodInfo method)
		{
			var obj = Expression.Parameter(typeof(object), "o");
			var value = Expression.Parameter(typeof(object));

			Expression<Action<object, object>> expr =
				Expression.Lambda<Action<object, object>>(
					Expression.Call(
						Expression.Convert(obj, method.DeclaringType),
						method,
						Expression.Convert(value, method.GetParameters()[0].ParameterType)),
					obj,
					value);

			return expr.Compile();
		}


		#region IPropertyAccessor Members

		public object Get (object target)
		{
			if (_getter != null) {
				return _getter(target);
			}
			return _propInfo.GetValue(target);
		}


		public void Set (object target, object value)
		{
			if (_setter != null) {
				_setter(target, value);
				return;
			}
			_propInfo.SetValue(target, value);
		}

		#endregion
	}

}
