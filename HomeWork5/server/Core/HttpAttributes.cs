using System;


namespace server.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class HttpController : Attribute { }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPost :Attribute  { }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPut :Attribute { }


}
