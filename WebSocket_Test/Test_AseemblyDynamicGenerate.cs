using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WebSocket_Test
{
    class Test_AseemblyDynamicGenerate
    {

        //測試動態產生Assembly的執行檔(存在於Bin/Release下)
        //ref:https://msdn.microsoft.com/zh-tw/library/system.reflection.assemblyname.codebase(v=vs.110).aspx
        //範例會發出動態組件並將組件儲存至目前的目錄中。 建立組件時，會使用 CodeBase 屬性來指定儲存組件的目錄
        //組件載入的最佳作法ref:https://msdn.microsoft.com/zh-tw/library/dd153782(v=vs.110).aspx
        static void Main1(string[] args)
        {
            int dif = String.Compare("ab1c", "abc");
            Console.Write(dif);
            //ref:http://stackoverflow.com/questions/15600142/reflection-emit-assemblybuilder-setentrypoint-does-not-set-entry-point
            //ref2:http://codex.wiki/question/1505549-4488
            // Create a dynamic assembly with name 'MyAssembly' and build version '1.0.0.2001'.
            AssemblyName myAssemblyName = new AssemblyName();
            // Set the codebase to the physical directory were the assembly resides.
            myAssemblyName.CodeBase = Directory.GetCurrentDirectory();
            // Set the culture information of the assembly to 'English-American'.
            myAssemblyName.CultureInfo = new CultureInfo("en-US");
            // Set the hash algoritm to 'SHA1'.
            myAssemblyName.HashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1;
            myAssemblyName.Name = "MyAssembly";
            myAssemblyName.Version = new Version("1.0.0.2015");
            MakeAssembly(myAssemblyName, "MyAssembly.exe");

            // Get all the assemblies currently loaded in the application domain.
            Assembly[] myAssemblies = Thread.GetDomain().GetAssemblies();

            // Get the dynamic assembly named 'MyAssembly'. 
            Assembly myAssembly = null;
            for (int i = 0; i < myAssemblies.Length; i++)
            {
                AssemblyName assemblyName = myAssemblies[i].GetName();
                Console.WriteLine("myAssemblies[{0}]:{1}",i,assemblyName.ToString());
                if (String.Compare(assemblyName.Name, "MyAssembly") == 0)
                {
                    myAssembly = myAssemblies[i];
                }
            }

            // Display the full assembly information to the console.
            if (myAssembly != null)
            {
                Console.WriteLine("\nDisplaying the full assembly name\n");
                Console.WriteLine(myAssembly.GetName().FullName);
            }
            Process.Start("MyAssembly.exe");
            
            Console.ReadKey();
        }

        static void MakeAssembly(AssemblyName myAssemblyName, string fileName)
        {
            // Get the assembly builder from the application domain associated with the current thread.
            //1.建立組件(從當前的Domain去建立一個)
            AssemblyBuilder myAssemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.RunAndSave);

            // Create a dynamic module in the assembly.
            //2.使用組件建立模組並設定模組名稱
            ModuleBuilder myModuleBuilder = myAssemblyBuilder.DefineDynamicModule("MyModule",fileName);

            // Create a type in the module.
            //3.使用模組建立型別並定義型別名稱
            TypeBuilder myTypeBuilder = myModuleBuilder.DefineType("MyType");

            // Create a method called 'Main'.
            //4.使用型別定義方法(弄一個Main方法當進入點)
            MethodBuilder myMethodBuilder = myTypeBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, typeof(void), null);
            
            // Get the Intermediate Language generator for the method.
            //5.使用方法建立一個IL產生器(MSIL)
            ILGenerator myILGenerator = myMethodBuilder.GetILGenerator();

            // Use the utility method to generate the IL instructions that print a string to the console.
            //6.Main方法建立一個Console並寫入字串
            myILGenerator.EmitWriteLine("Hello World!!!");
            //弄一個Console.ReadKey暫停看一下東西(會有exception ... 猜測是進入的參數沒輸入值產生錯誤中斷)
            //myILGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadKey", new Type[] { typeof(bool) }));
            myILGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadLine", Type.EmptyTypes));
            
            // Generate the 'ret' IL instruction.
            //7.產生IL指令
            myILGenerator.Emit(OpCodes.Ret);
            
            // End the creation of the type.
            //8.將上面執行過的設定建立成一個型別
            myTypeBuilder.CreateType();

            // Set the method with name 'Main' as the entry point in the assembly.
            //9.使用組件設定進入點的方法設定
            myAssemblyBuilder.SetEntryPoint(myMethodBuilder,PEFileKinds.ConsoleApplication);
            myAssemblyBuilder.Save(fileName);
        }
    }
}
