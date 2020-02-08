using System;
using System.Reflection;
using System.Reflection.Emit;

namespace FastAccessProperty
{
    public delegate object DynamicMethodDelegate (object target, object[] args);

    public static class DelegateFactory
    {
        /// <summary>
        /// Generates a DynamicMethodDelegate delegate from a MethodInfo object.
        /// </summary>
        public static DynamicMethodDelegate Create (MethodInfo methodInfo) {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object), typeof(object[]) }, methodInfo.DeclaringType.Module);
            var il = dynamicMethod.GetILGenerator();

            #region Look parameters

            var ps = methodInfo.GetParameters();
            var paramTypes = new Type[ps.Length];
            for (int i = 0; i < paramTypes.Length; i++) {
                if (ps[i].ParameterType.IsByRef)
                    paramTypes[i] = ps[i].ParameterType.GetElementType(); // TODO: DEBUG
                else
                    paramTypes[i] = ps[i].ParameterType;
            }

            #endregion

            #region Create local variables for each arguments
            var locals = new LocalBuilder[paramTypes.Length];
            for (int i = 0; i < paramTypes.Length; i++) {
                locals[i] = il.DeclareLocal(paramTypes[i], true);

                il.Emit(OpCodes.Ldarg_1);
                EmitHelper.EmitFastInt(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                EmitHelper.EmitCastToReference(il, paramTypes[i]);
                il.Emit(OpCodes.Stloc, locals[i]);
            }
            #endregion

            #region Instance push

            // If method isn't static push target instance on top of stack.
            if (!methodInfo.IsStatic) {
                // Argument 0 of dynamic method is target instance.
                il.Emit(OpCodes.Ldarg_0);
            }

            #endregion

            #region Parameters => locals
            for (int i = 0; i < paramTypes.Length; i++) {
                if (ps[i].ParameterType.IsByRef)
                    il.Emit(OpCodes.Ldloca_S, locals[i]);
                else
                    il.Emit(OpCodes.Ldloc, locals[i]);
            }
            #endregion

            #region Method call

            // Perform actual call.
            // If method is not final a callvirt is required
            // otherwise a normal call will be emitted.
            if (methodInfo.IsStatic) // method.IsFinal
                il.EmitCall(OpCodes.Call, methodInfo, null);
            else
                il.EmitCall(OpCodes.Callvirt, methodInfo, null);

            if (methodInfo.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else
                EmitHelper.EmitBoxIfNeeded(il, methodInfo.ReturnType);

            #endregion

            #region Process ref/out parameters
            for (int i = 0; i < paramTypes.Length; i++) {
                if (ps[i].ParameterType.IsByRef) {
                    il.Emit(OpCodes.Ldarg_1);
                    EmitHelper.EmitFastInt(il, i);
                    il.Emit(OpCodes.Ldloc, locals[i]);
                    if (locals[i].LocalType.IsValueType)
                        il.Emit(OpCodes.Box, locals[i].LocalType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }
            #endregion

            // Emit return opcode.
            il.Emit(OpCodes.Ret);

            return (DynamicMethodDelegate)dynamicMethod.CreateDelegate(typeof(DynamicMethodDelegate));
        }
    }

    public static class EmitHelper
    {
        public static readonly Module DynamicModule = typeof(EmitHelper).Module;
        public static readonly Type[] SingleObject = { typeof(object) };
        public static readonly Type[] TwoObjects = { typeof(object), typeof(object) };
        public static readonly Type[] ManyObjects = { typeof(object), typeof(object[]) };

        public static void EmitBoxIfNeeded (this ILGenerator il, Type type) {
            if (type.IsValueType) {
                il.Emit(OpCodes.Box, type);
            }
        }

        public static void EmitUnboxIfNeeded (this ILGenerator il, Type type) {
            if (type.IsValueType) {
                il.Emit(OpCodes.Unbox_Any, type);
            }
        }

        public static void EmitFastInt (this ILGenerator il, int value) {
            switch (value) {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128) {
                il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
            } else {
                il.Emit(OpCodes.Ldc_I4, value);
            }
        }

        public static void EmitCastToReference (this ILGenerator il, Type type) {
            if (type.IsValueType) {
                il.Emit(OpCodes.Unbox_Any, type);
            } else {
                il.Emit(OpCodes.Castclass, type);
            }
        }

        public static void PushInstance (this ILGenerator il, Type type) {
            il.Emit(OpCodes.Ldarg_0);
            if (type.IsValueType) {
                il.Emit(OpCodes.Unbox, type);
            }
        }
    }

    public static class MyPropertyAccessor
    {
        private static Func<object, object> CreatePropertyGetterHandler (PropertyInfo propertyInfo) {
            var dynam = new DynamicMethod(string.Empty, typeof(object), EmitHelper.SingleObject, EmitHelper.DynamicModule, true);
            var il = dynam.GetILGenerator();
            var methodInfo = propertyInfo.GetGetMethod();

            if (!methodInfo.IsStatic) {
                il.PushInstance(propertyInfo.DeclaringType);
            }

            if (methodInfo.IsFinal || !methodInfo.IsVirtual) {
                il.Emit(OpCodes.Call, methodInfo);
            } else {
                il.Emit(OpCodes.Callvirt, methodInfo);
            }

            il.EmitBoxIfNeeded(propertyInfo.PropertyType);
            il.Emit(OpCodes.Ret);

            return (Func<object, object>)dynam.CreateDelegate(typeof(Func<object, object>));
        }

        private static Action<object, object> CreatePropertySetterHandler (PropertyInfo propertyInfo) {
            var dynam = new DynamicMethod(string.Empty, typeof(void), EmitHelper.TwoObjects, EmitHelper.DynamicModule, true);
            var il = dynam.GetILGenerator();
            var methodInfo = propertyInfo.GetSetMethod();

            if (!methodInfo.IsStatic) {
                il.PushInstance(propertyInfo.DeclaringType);
            }

            il.Emit(OpCodes.Ldarg_1);
            il.EmitUnboxIfNeeded(propertyInfo.PropertyType);

            if (methodInfo.IsFinal || !methodInfo.IsVirtual) {
                il.Emit(OpCodes.Call, methodInfo);
            } else {
                il.Emit(OpCodes.Callvirt, methodInfo);
            }
            il.Emit(OpCodes.Ret);

            return (Action<object, object>)dynam.CreateDelegate(typeof(Action<object, object>));
        }

    }
}
