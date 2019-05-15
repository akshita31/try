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
            #region HelloWorld
            Console.WriteLine("Hello World!");
            #endregion
        }

        public static void DateTime()
        {
            #region DateTime
            Console.WriteLine(System.DateTime.Now);
            #endregion
        }

        public static void Guid()
        {
            #region Guid
            Console.WriteLine(System.Guid.NewGuid());
            #endregion
        }
        
        public static void EmptyRegion()
        {
            #region EmptyRegion
            #endregion
        }
    }
}