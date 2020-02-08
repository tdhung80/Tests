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
			var lamdaAccessor = new LamdaPropertyAccessor(propInfo);
			var dynamicMethodGetter = DynamicMethodCompiler.CreateGetHandler(propInfo); // DelegateFactory.CreatePropertyGetter(propInfo);
            var dynamicMethodSetter = DynamicMethodCompiler.CreateSetHandler(propInfo); // DelegateFactory.CreatePropertySetter(propInfo);
            var delegateMethod = DelegateFactory.Create(methodInfo);
            var dynamicMethod = FastReflection.DelegateForCall(methodInfo);

            var testObject = new TestClass() { StringProp = "Hello" };
			var testInterface = testObject as ITestClass;
            
			Console.WriteLine("Getter tests");
			emitAccessor.Get(testObject);
            //dynamicMethodGetter(testObject);
			lamdaAccessor.Get(testObject);
			var m = 0;
			do {
                Console.WriteLine("  Round #" + ++m);

				Loop("Direct", () => testObject.StringProp);
				Loop("Interface", () => testInterface.StringProp);
                Loop("DynamicMethodCompiler", () => dynamicMethodGetter(testObject));
                Loop("EmitPropertyAccessor", () => emitAccessor.Get(testObject));                
                Loop("LamdaPropertyAccessor", () => lamdaAccessor.Get(testObject));
                Loop("Reflection", () => propInfo.GetValue(testObject), N, 50);

                //Console.WriteLine("  Wait 5s");
				//Thread.Sleep(5000);
			} while (m < 2);
			Console.WriteLine("\r\n");

			Console.WriteLine("Setter tests"); // compile
			emitAccessor.Set(testObject, "Hello1");
			//dynamicMethodSetter(testObject, "Hello1");
			lamdaAccessor.Set(testObject, "Hello1");
			m = 0;
			do {
                Console.WriteLine("  Round #" + ++m);

                Loop("Direct", () => testObject.StringProp = "Hello1");
				Loop("Interface", () => testInterface.StringProp  = "Hello1");
                Loop("DynamicMethodCompiler", () => dynamicMethodSetter(testObject, "Hello1"));
                Loop("EmitPropertyAccessor", () => emitAccessor.Set(testObject, "Hello1"));                
                Loop("LamdaPropertyAccessor", () => lamdaAccessor.Set(testObject, "Hello1"));
                Loop("Reflection", () => propInfo.SetValue(testObject, "Hello1"), N, 50);

                //Console.WriteLine("  Wait 5s");
				//Thread.Sleep(5000);
			} while (m < 2);
            Console.WriteLine("\r\n");

            Console.WriteLine("Invoke tests");
            //emitAccessor.Set(testObject, "Hello1");
            //dynamicMethodSetter(testObject, "Hello1");
            //lamdaAccessor.Set(testObject, "Hello1");
            m = 0;
            do {
                Console.WriteLine("  Round #" + ++m);

                Loop("Direct", () => testObject.SimpleMethod());
                Loop("Interface", () => testInterface.SimpleMethod());                
                Loop("DelegateFactory", () => delegateMethod.Invoke(testObject, null));
                Loop("DynamicMethod", () => dynamicMethod.Invoke(testObject, null));
                Loop("Reflection", () => methodInfo.Invoke(testObject, null), N, 50);

                //Loop("EmitPropertyAccessor", () => emitAccessor.Set(testObject, "Hello1"));
                //Loop("LamdaPropertyAccessor", () => lamdaAccessor.Set(testObject, "Hello1"));
                //Loop("DynamicMethodCompiler", () => dynamicMethodSetter(testObject, "Hello1"));

                //Console.WriteLine("  Wait 5s");
                //Thread.Sleep(5000);
            } while (m < 2);
            Console.WriteLine("\r\n");

            Console.WriteLine("--- DONE ---");
            Console.ReadKey();
        }

	    static void Loop (string title, Action action, int n = 10000000, int k = 1) {
			var sw = Stopwatch.StartNew();
			for (int i = 0, m = n / k; i < m; i++) {
				action();
			}
			Console.WriteLine("    " + title + " in " + sw.ElapsedMilliseconds * k + " ms");
	    }

		static void Loop (string title, Func<object> action, int n = 10000000, int k = 1) {
			var sw = Stopwatch.StartNew();
			for (int i = 0, m = n /k; i < m; i++) {
				action();
			}
			Console.WriteLine("    " + title + " in " + sw.ElapsedMilliseconds * k + " ms");
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
