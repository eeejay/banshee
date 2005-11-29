using System;
using System.Reflection;

namespace Banshee.Base
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class BansheeTodo : Attribute 
    {
        private string comment;
        
        public BansheeTodo() : this("Unimplemented")
        {
        }

        public BansheeTodo(string comment) : base()
        {
            this.comment = comment;
        }

        public string Comment {
            get { 
                return comment; 
            }
        }
        
        public static void PrintReport()
        {
            PrintReport(Assembly.GetExecutingAssembly());
        }
        
        public static void PrintReport(Assembly asm)
        {
            Console.WriteLine("TODO Report for {0}", asm.FullName);
            
            foreach(Type type in asm.GetTypes()) {
                Attribute [] type_attrs = Attribute.GetCustomAttributes(type, typeof(BansheeTodo));

                foreach(BansheeTodo todo in type_attrs) {
                    Console.WriteLine("  {0}: {0}", type, todo.Comment);
                }
                
                foreach(MemberInfo member in type.GetMembers()) {
                    Attribute [] member_attrs = (Attribute [])member.GetCustomAttributes(typeof(BansheeTodo), false);
                    
                    foreach(BansheeTodo todo in member_attrs) {
                        Console.WriteLine("  {0}:{1} ({2}): {3}", type, member.Name, member.MemberType, todo.Comment);
                    }
                }
            }
        }
    }
}
