using System;
using System.Diagnostics;
using System.Threading;

namespace FastAccessProperty
{
    class Program
    {
        static void Main (string[] args)
        {
            // Proxy, Code Generation, PostSharp
            const int N = 10000000;

            var type = typeof(TestClass);
			var propInfo = type.GetProperty("StringProp");
            var methodInfo = type.GetMethod("SimpleMethod");
            var emitAccessor = new EmitPropertyAccessor(type, "StringProp");
			var lambdaAccessor = new LambdaPropertyAccessor(propInfo);
            var lambdaPropertyAccessor = LambdaInvoker.ForProperty(type, "StringProp");
            var lambdaInvoker = LambdaInvoker.ForMethod(type, "SimpleMethod");
            var dynamicMethodGetter = DynamicMethodCompiler.CreateGetHandler(propInfo); // DelegateFactory.CreatePropertyGetter(propInfo);
            var dynamicMethodSetter = DynamicMethodCompiler.CreateSetHandler(propInfo); // DelegateFactory.CreatePropertySetter(propInfo);
            var dynamicMethod = FastReflection.DelegateForCall(methodInfo);
            var delegateMethod = DelegateFactory.Create(methodInfo);

            var testObject = new TestClass() { StringProp = "Hello" };
			var testInterface = testObject as ITestClass;
            
			Console.WriteLine("PropertyGetter tests");
			emitAccessor.Get(testObject);
			lambdaAccessor.Get(testObject);
			var m = 0;
			do {
                Console.WriteLine("  Round #" + ++m);

				var baseline = Loop("DirectAccess", () => testObject.StringProp, N);
				Loop("Interface", () => testInterface.StringProp, N);
                Loop("DynamicMethod", () => dynamicMethodGetter(testObject), N);
                Loop("DynamicModule", () => emitAccessor.Get(testObject), N);
                Loop("LambdaExpression", () => lambdaAccessor.Get(testObject), N);
                Loop("Reflection", () => propInfo.GetValue(testObject), N, 50);

                //Console.WriteLine("  Wait 5s");
				//Thread.Sleep(5000);
			} while (m < 2);
			Console.WriteLine("\r\n");

			Console.WriteLine("PropertySetter tests"); // compile
			emitAccessor.Set(testObject, "Hello1");
			//dynamicMethodSetter(testObject, "Hello1");
			lambdaAccessor.Set(testObject, "Hello1");
			m = 0;
			do {
                Console.WriteLine("  Round #" + ++m);

                var baseline = Loop("DirectAccess", () => testObject.StringProp = "Hello1", N);
				Loop("Interface", () => testInterface.StringProp  = "Hello1", N);
                Loop("DynamicMethod", () => dynamicMethodSetter(testObject, "Hello1"), N);
                Loop("DynamicModule", () => emitAccessor.Set(testObject, "Hello1"), N);
                Loop("LambdaExpression", () => lambdaAccessor.Set(testObject, "Hello1"), N);
                Loop("Reflection", () => propInfo.SetValue(testObject, "Hello1"), N, 50);

                //Console.WriteLine("  Wait 5s");
				//Thread.Sleep(5000);
			} while (m < 2);
            Console.WriteLine("\r\n");

            Console.WriteLine("MethodInvoker tests");
            //emitAccessor.Set(testObject, "Hello1");
            //dynamicMethodSetter(testObject, "Hello1");
            //lamdaAccessor.Set(testObject, "Hello1");
            m = 0;
            do {
                Console.WriteLine("  Round #" + ++m);

                var baseline = Loop("DirectAccess", () => testObject.SimpleMethod(), N);
                Loop("Interface", () => testInterface.SimpleMethod(), N);                
                Loop("DynamicModule", () => delegateMethod.Invoke(testObject, null), N);
                Loop("DynamicMethod", () => dynamicMethod.Invoke(testObject, null), N);
                Loop("LambdaExpression", () => lambdaInvoker.Invoke(testObject, null), N);                
                Loop("Reflection", () => methodInfo.Invoke(testObject, null), N, 50);

                //Console.WriteLine("  Wait 5s");
                //Thread.Sleep(5000);
            } while (m < 2);
            Console.WriteLine("\r\n");

            Console.WriteLine("--- DONE ---");
            Console.ReadKey();
        }

	    static long Loop (string title, Action action, int n, int k = 1) {
			var sw = Stopwatch.StartNew();
			for (int i = 0, m = n / k; i < m; i++) {
				action();
			}
            var result = sw.ElapsedMilliseconds * k;
            Console.WriteLine("    " + title + " in " + result + " ms");
            return result;
	    }

		static long Loop (string title, Func<object> action, int n, int k = 1) {
			var sw = Stopwatch.StartNew();
			for (int i = 0, m = n /k; i < m; i++) {
				action();
			}
            var result = sw.ElapsedMilliseconds * k;
            Console.WriteLine("    " + title + " in " + result + " ms");
            return result;
		}

    }

	public interface ITestClass
	{
		string StringProp { get; set; }

        void SimpleMethod ();
    }

    public class TestClass : ITestClass
    {
        public string StringProp { get; set; }

        public void SimpleMethod () {
        }
    }
}
