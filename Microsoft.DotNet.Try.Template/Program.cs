using System;

namespace Snippets
{
    public class Program
    {
        static void Main(
            string region = null,
            string session = null,
            string package = null,
            string project = null,
            string[] args = null)
        {
            switch (region)
            {
                case "HelloWorld":
                    HelloWorld();
                    break;
                case "DateTime":
                    DateTime();
                    break;
                case "Guid":
                    Guid();
                    break;
                case "EmptyRegion":
                    EmptyRegion();
                    break;
            }
        }

        public static void HelloWorld()
        {
            #region run
            Console.WriteLine("Hello World!");
            #endregion
        }

        public static void DateTime()
        {
            #region run1
            Console.WriteLine(System.DateTime.Now);
            #endregion
        }

        public static void Guid()
        {
            #region run2
            Console.WriteLine(System.Guid.NewGuid());
            #endregion
        }
        
        public static void EmptyRegion()
        {
            #region run3
            #endregion
        }
    }
}