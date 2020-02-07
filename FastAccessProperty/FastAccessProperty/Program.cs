using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastAccessProperty
{
    class Program
    {
        static void Main (string[] args)
        {
            var type = typeof(TestClass);
			var propInfo = type.GetProperty("StringProp");
            var emitAccessor = new EmitPropertyAccessor(type, "StringProp");
			var lamdaAccessor = new LamdaPropertyAccessor(propInfo);
			var dynamicMethodGetter = DynamicMethodCompiler.CreateGetHandler(propInfo);
			var dynamicMethodSetter = DynamicMethodCompiler.CreateSetHandler(propInfo);

            var testObject = new TestClass() { StringProp = "Hello" };
			var testInterface = testObject as ITestClass;
            
			Console.Write("Start...");
			emitAccessor.Get(testObject);
            dynamicMethodGetter(testObject);
			lamdaAccessor.Get(testObject);
	        Console.WriteLine("Read test");
			var m = 0;
			do {
				var n = 10000000;
				Loop("Direct", () => testObject.StringProp);
				Loop("Interface", () => testInterface.StringProp);
				Loop("Reflection", () => propInfo.GetValue(testObject), n / 50);
				Loop("EmitPropertyAccessor", () => emitAccessor.Get(testObject));
				Loop("LamdaPropertyAccessor", () => lamdaAccessor.Get(testObject));
				Loop("DynamicMethodCompiler", () => dynamicMethodGetter(testObject));

				Console.WriteLine("Wait 5s");
				Thread.Sleep(5000);
			} while (m++ < 2);
			Console.WriteLine("\r\n\r\n");

			Console.Write("Start..."); // compile
			emitAccessor.Set(testObject, "Hello1");
			dynamicMethodSetter(testObject, "Hello1");
			lamdaAccessor.Set(testObject, "Hello1");
			Console.WriteLine("Write test");
			m = 0;
			do {
				var n = 10000000;
				Loop("Direct", () => testObject.StringProp = "Hello1");
				Loop("Interface", () => testInterface.StringProp  = "Hello1");
				Loop("Reflection", () => propInfo.SetValue(testObject, "Hello1"), n / 50);
				Loop("EmitPropertyAccessor", () => emitAccessor.Set(testObject, "Hello1"));
				Loop("LamdaPropertyAccessor", () => lamdaAccessor.Set(testObject, "Hello1"));
				Loop("DynamicMethodCompiler", () => dynamicMethodSetter(testObject, "Hello1"));

				Console.WriteLine("Wait 5s");
				Thread.Sleep(5000);
			} while (m++ < 2);

            Console.WriteLine("--- DONE ---");
            Console.ReadKey();
        }

	    static void Loop (string title, Action action, int n = 10000000) {
			var sw = Stopwatch.StartNew();
			for (int i = 0; i < n; i++) {
				action();
			}
			Console.WriteLine(title + " in " + sw.ElapsedMilliseconds + " ms");
	    }

		static void Loop (string title, Func<object> action, int n = 10000000) {
			var sw = Stopwatch.StartNew();
			for (int i = 0; i < n; i++) {
				action();
			}
			Console.WriteLine(title + " in " + sw.ElapsedMilliseconds + " ms");
		}

    }

	public interface ITestClass
	{
		string StringProp { get; set; }
	}

    public class TestClass : ITestClass
    {
        public string StringProp { get; set; }
    }
}
