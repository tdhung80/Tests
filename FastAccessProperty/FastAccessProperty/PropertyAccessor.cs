﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastAccessProperty
{
    /// <summary>
    /// The IPropertyAccessor interface defines a property accessor
    /// </summary>
    public interface IPropertyAccessor
    {
        /// <summary>
        /// Gets the value stored in the property for
        /// the specified target.
        /// </summary>
        /// <param name="target">Object to retrieve
        /// the property from.</param>
        /// <returns>Property value.</returns>
        object Get (object target);
        /// <summary>
        /// Sets the value for the property of
        /// the specified target.
        /// </summary>
        /// <param name="target">Object to set the
        /// property on.</param>
        /// <param name="value">Property value.</param>
        void Set (object target, object value);
    }

	/// <summary>
    /// PropertyAccessorException class.
    /// </summary>
    public class PropertyAccessorException : Exception
    {
        public PropertyAccessorException (string message)
            : base(message)
        {
        }
    }

}
